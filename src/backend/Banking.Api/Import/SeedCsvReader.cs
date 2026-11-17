using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Banking.Api.Contracts;
using Banking.Api.Data.Entities;
using CsvHelper;
using CsvHelper.Configuration;

namespace Banking.Api.Import;

public sealed record SeedRow(string EntityType, Guid Id, object Entity, string Fingerprint, int RowNumber);
public sealed record SeedBatch(IReadOnlyDictionary<string, IReadOnlyList<SeedRow>> Rows);
public sealed class SeedInputException(string message) : Exception(message);

public static partial class SeedCsvReader
{
    private static readonly IReadOnlyDictionary<string, string[]> Headers = new Dictionary<string, string[]>
    {
        ["customers"] = ["id", "name", "created_at", "english_name", "email", "phone", "address_line",
            "subdistrict", "district", "province", "postcode", "date_of_birth", "occupation", "national_id"],
        ["accounts"] = ["id", "customer_id", "currency", "balance", "created_at"],
        ["transfers"] = ["id", "source_account_id", "destination_account_id", "amount", "currency", "completed_at"],
        ["transaction_entries"] = ["id", "sequence", "account_id", "type", "amount", "currency", "completed_at",
            "resulting_balance", "transfer_id", "counterparty_account_id"]
    };

    [GeneratedRegex(@"^[0-9]{1,16}(\.[0-9]{1,2})?$", RegexOptions.CultureInvariant)]
    private static partial Regex DecimalPattern();

    public static SeedBatch Read(string path)
    {
        if (!Path.IsPathFullyQualified(path))
            throw new SeedInputException("Seed directory must be an absolute path.");

        return ReadSources(type =>
        {
            var file = Path.Combine(path, $"{type}.csv");
            if (!File.Exists(file))
                throw new SeedInputException($"{type}.csv: required file is missing.");
            return new StreamReader(file, new UTF8Encoding(false, true));
        });
    }

    public static SeedBatch ReadSources(Func<string, TextReader> openSource)
    {
        var all = new Dictionary<string, IReadOnlyList<SeedRow>>();
        foreach (var (type, headers) in Headers)
        {
            var rows = new List<SeedRow>();
            var seen = new HashSet<Guid>();
            using var stream = openSource(type);
            using var csv = new CsvReader(stream, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                DetectColumnCountChanges = true,
                BadDataFound = _ => throw new SeedInputException($"{type}.csv: invalid CSV format."),
                MissingFieldFound = _ => throw new SeedInputException($"{type}.csv: missing field.")
            });

            bool ReadRecord()
            {
                try { return csv.Read(); }
                catch (Exception exception) when (exception is CsvHelperException or DecoderFallbackException)
                {
                    throw new SeedInputException($"{type}.csv: invalid CSV format at row {csv.Context.Parser?.Row ?? 0}.");
                }
            }

            if (!ReadRecord() || !csv.ReadHeader() || csv.HeaderRecord is null ||
                csv.HeaderRecord.Distinct(StringComparer.Ordinal).Count() != csv.HeaderRecord.Length ||
                csv.HeaderRecord.Length != headers.Length ||
                !headers.All(header => csv.HeaderRecord.Contains(header, StringComparer.Ordinal)))
                throw new SeedInputException($"{type}.csv: missing or duplicate required headers.");

            while (ReadRecord())
            {
                var rowNumber = csv.Context.Parser?.Row ?? 0;
                try
                {
                    var fields = headers.ToDictionary(header => header, header => csv.GetField(header) ?? "", StringComparer.Ordinal);
                    var entity = Parse(type, fields);
                    var id = RequiredGuid(fields["id"]);
                    if (!seen.Add(id))
                        throw new FormatException("Duplicate UUID.");
                    rows.Add(new SeedRow(type, id, entity, SeedCanonicalizer.Fingerprint(headers, fields), rowNumber));
                }
                catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentException)
                {
                    throw new SeedInputException($"{type}.csv: invalid record at row {rowNumber}.");
                }
            }
            all[type] = rows;
        }

        ValidateReferences(all);
        return new SeedBatch(all);
    }

    private static object Parse(string type, IReadOnlyDictionary<string, string> fields)
    {
        var id = RequiredGuid(fields["id"]);
        switch (type)
        {
            case "customers":
                if (fields["name"].Length == 0)
                    throw new FormatException();
                return new Customer
                {
                    Id = id,
                    Name = fields["name"],
                    CreatedAt = Timestamp(fields["created_at"]),
                    EnglishName = fields["english_name"],
                    Email = fields["email"],
                    Phone = fields["phone"],
                    AddressLine = fields["address_line"],
                    Subdistrict = fields["subdistrict"],
                    District = fields["district"],
                    Province = fields["province"],
                    Postcode = fields["postcode"],
                    DateOfBirth = fields["date_of_birth"].Length == 0 ? null
                        : DateOnly.ParseExact(fields["date_of_birth"], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Occupation = fields["occupation"],
                    NationalId = fields["national_id"]
                };
            case "accounts":
                return new Account
                {
                    Id = id,
                    CustomerId = RequiredGuid(fields["customer_id"]),
                    Currency = Currency(fields["currency"]),
                    Balance = Money(fields["balance"], false),
                    CreatedAt = Timestamp(fields["created_at"])
                };
            case "transfers":
                var source = RequiredGuid(fields["source_account_id"]);
                var destination = RequiredGuid(fields["destination_account_id"]);
                if (source == destination)
                    throw new FormatException();
                return new Transfer
                {
                    Id = id,
                    SourceAccountId = source,
                    DestinationAccountId = destination,
                    Amount = Money(fields["amount"], true),
                    Currency = Currency(fields["currency"]),
                    CompletedAt = Timestamp(fields["completed_at"])
                };
            case "transaction_entries":
                if (!long.TryParse(fields["sequence"], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence) || sequence <= 0)
                    throw new FormatException();
                var entryType = fields["type"];
                var transferId = OptionalGuid(fields["transfer_id"]);
                var counterpartyId = OptionalGuid(fields["counterparty_account_id"]);
                var transferEntry = entryType is "TRANSFER_IN" or "TRANSFER_OUT";
                if ((!transferEntry && entryType is not ("DEPOSIT" or "WITHDRAWAL")) ||
                    transferEntry != transferId.HasValue || transferEntry != counterpartyId.HasValue)
                    throw new FormatException();
                return new TransactionEntry
                {
                    Id = id,
                    Sequence = sequence,
                    AccountId = RequiredGuid(fields["account_id"]),
                    Type = entryType,
                    Amount = Money(fields["amount"], true),
                    Currency = Currency(fields["currency"]),
                    CompletedAt = Timestamp(fields["completed_at"]),
                    ResultingBalance = Money(fields["resulting_balance"], false),
                    TransferId = transferId,
                    CounterpartyAccountId = counterpartyId
                };
            default:
                throw new ArgumentException("Unknown source type.");
        }
    }

    private static Guid RequiredGuid(string value) =>
        Guid.TryParse(value, out var id) && id != Guid.Empty ? id : throw new FormatException();

    private static Guid? OptionalGuid(string value) => value.Length == 0 ? null : RequiredGuid(value);

    private static string Currency(string value) => value == "THB" ? value : throw new FormatException();

    private static decimal Money(string value, bool positive)
    {
        if (!DecimalPattern().IsMatch(value) ||
            !decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount) ||
            amount > MoneyValue.Maximum || (positive && amount <= 0))
            throw new FormatException();
        return amount;
    }

    private static DateTimeOffset Timestamp(string value)
    {
        if (!Regex.IsMatch(value, @"(?:Z|[+-]\d{2}(?::?\d{2})?)$", RegexOptions.CultureInvariant) ||
            !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ||
            result.Ticks % 10 != 0)
            throw new FormatException();
        return result.ToUniversalTime();
    }

    private static void ValidateReferences(IReadOnlyDictionary<string, IReadOnlyList<SeedRow>> rows)
    {
        var customers = rows["customers"].ToDictionary(row => row.Id, row => (Customer)row.Entity);
        var accounts = rows["accounts"].ToDictionary(row => row.Id, row => (Account)row.Entity);
        var transfers = rows["transfers"].ToDictionary(row => row.Id, row => (Transfer)row.Entity);
        var entries = rows["transaction_entries"].Select(row => (TransactionEntry)row.Entity).ToList();

        if (accounts.Values.Any(account => !customers.ContainsKey(account.CustomerId)) ||
            transfers.Values.Any(transfer => !accounts.ContainsKey(transfer.SourceAccountId) || !accounts.ContainsKey(transfer.DestinationAccountId)) ||
            entries.Select(entry => entry.Sequence).Distinct().Count() != entries.Count ||
            entries.Any(entry => !accounts.ContainsKey(entry.AccountId) ||
                (entry.CounterpartyAccountId.HasValue && !accounts.ContainsKey(entry.CounterpartyAccountId.Value)) ||
                (entry.TransferId.HasValue && !transfers.ContainsKey(entry.TransferId.Value))))
            throw new SeedInputException("Seed files contain duplicate sequences or unresolved references.");

        foreach (var transfer in transfers.Values)
        {
            var linked = entries.Where(entry => entry.TransferId == transfer.Id).ToList();
            if (linked.Count != 2 || linked.Count(entry => entry.Type == "TRANSFER_OUT") != 1 ||
                linked.Count(entry => entry.Type == "TRANSFER_IN") != 1)
                throw new SeedInputException($"transfers.csv: transfer {transfer.Id:D} must have two matching entries.");
            foreach (var entry in linked)
            {
                var outgoing = entry.Type == "TRANSFER_OUT";
                if (entry.AccountId != (outgoing ? transfer.SourceAccountId : transfer.DestinationAccountId) ||
                    entry.CounterpartyAccountId != (outgoing ? transfer.DestinationAccountId : transfer.SourceAccountId) ||
                    entry.Amount != transfer.Amount || entry.Currency != transfer.Currency || entry.CompletedAt != transfer.CompletedAt)
                    throw new SeedInputException($"transfers.csv: transfer {transfer.Id:D} has inconsistent entries.");
            }
        }
    }
}
