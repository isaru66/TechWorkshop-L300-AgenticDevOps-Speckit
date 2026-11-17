using Banking.Api.Data;
using Banking.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Api.Import;

public sealed record ImportCounts(int Inserted, int Skipped);
public sealed record ImportResult(IReadOnlyDictionary<string, ImportCounts> Counts);

public sealed class SeedImporter(BankingDbContext db, BankingTransactionCoordinator coordinator)
{
    public Task<ImportResult> ImportAsync(string seedPath, CancellationToken cancellationToken = default) =>
        ImportBatchAsync(SeedCsvReader.Read(seedPath), cancellationToken);

    internal Task<ImportResult> ImportAsync(Func<string, TextReader> openSource, CancellationToken cancellationToken = default) =>
        ImportBatchAsync(SeedCsvReader.ReadSources(openSource), cancellationToken);

    internal async Task<ImportResult> ImportBatchAsync(SeedBatch batch, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await coordinator.AcquireImportLockAsync(cancellationToken);

        var accepted = await db.SeedRecords.AsNoTracking().ToDictionaryAsync(
            record => (record.EntityType, record.RecordId), cancellationToken);
        var entityIds = new Dictionary<string, HashSet<Guid>>
        {
            ["customers"] = (await db.Customers.AsNoTracking().Select(entity => entity.Id).ToListAsync(cancellationToken)).ToHashSet(),
            ["accounts"] = (await db.Accounts.AsNoTracking().Select(entity => entity.Id).ToListAsync(cancellationToken)).ToHashSet(),
            ["transfers"] = (await db.Transfers.AsNoTracking().Select(entity => entity.Id).ToListAsync(cancellationToken)).ToHashSet(),
            ["transaction_entries"] = (await db.TransactionEntries.AsNoTracking().Select(entity => entity.Id).ToListAsync(cancellationToken)).ToHashSet()
        };
        var incoming = batch.Rows.Values.SelectMany(rows => rows).ToDictionary(row => (row.EntityType, row.Id));
        foreach (var existing in accepted)
        {
            if (!incoming.TryGetValue(existing.Key, out var source))
                throw new SeedInputException($"{existing.Key.EntityType}.csv: accepted seed UUID {existing.Key.RecordId:D} is missing.");
            if (!entityIds[existing.Key.EntityType].Contains(existing.Key.RecordId) ||
                existing.Value.CanonicalVersion != SeedCanonicalizer.Version ||
                existing.Value.SourceFingerprint != source.Fingerprint)
                throw new SeedInputException($"{source.EntityType}.csv: conflicting seed UUID {source.Id:D} at row {source.RowNumber}.");
        }

        foreach (var row in incoming.Values)
        {
            if (!accepted.ContainsKey((row.EntityType, row.Id)) && entityIds[row.EntityType].Contains(row.Id))
                throw new SeedInputException($"{row.EntityType}.csv: untracked UUID collision {row.Id:D} at row {row.RowNumber}.");
        }

        var existingSequences = (await db.TransactionEntries.AsNoTracking().Select(entry => entry.Sequence)
            .ToListAsync(cancellationToken)).ToHashSet();
        foreach (var row in batch.Rows["transaction_entries"])
        {
            var entry = (TransactionEntry)row.Entity;
            if (!accepted.ContainsKey((row.EntityType, row.Id)) && existingSequences.Contains(entry.Sequence))
                throw new SeedInputException($"transaction_entries.csv: sequence {entry.Sequence} conflicts with stored history for UUID {row.Id:D} at row {row.RowNumber}.");
        }

        var counts = new Dictionary<string, ImportCounts>();
        foreach (var (type, rows) in batch.Rows)
        {
            var inserted = 0;
            foreach (var row in rows)
            {
                if (accepted.ContainsKey((type, row.Id)))
                    continue;

                switch (row.Entity)
                {
                    case Customer customer: db.Customers.Add(customer); break;
                    case Account account: db.Accounts.Add(account); break;
                    case Transfer transfer: db.Transfers.Add(transfer); break;
                    case TransactionEntry entry: db.TransactionEntries.Add(entry); break;
                    default: throw new InvalidOperationException("Unsupported seed entity.");
                }
                db.SeedRecords.Add(new SeedRecord
                {
                    EntityType = type,
                    RecordId = row.Id,
                    CanonicalVersion = SeedCanonicalizer.Version,
                    SourceFingerprint = row.Fingerprint,
                    ImportedAt = DateTimeOffset.UtcNow
                });
                inserted++;
            }

            if (inserted > 0)
                await db.SaveChangesAsync(cancellationToken);
            counts[type] = new ImportCounts(inserted, rows.Count - inserted);
        }

        if (counts["transaction_entries"].Inserted > 0)
        {
            await db.Database.ExecuteSqlRawAsync(
                "SELECT setval('transaction_entry_sequence', GREATEST((SELECT COALESCE(MAX(sequence), 0) FROM transaction_entries), (SELECT last_value FROM transaction_entry_sequence)), true)",
                cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new ImportResult(counts);
    }
}
