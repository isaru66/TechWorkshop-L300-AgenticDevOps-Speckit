using System.Text.RegularExpressions;
using Banking.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Api.Data;

public sealed class BankingDbContext(DbContextOptions<BankingDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<TransactionEntry> TransactionEntries => Set<TransactionEntry>();
    public DbSet<SeedRecord> SeedRecords => Set<SeedRecord>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasSequence<long>("transaction_entry_sequence");

        model.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Name).IsRequired();
        });

        model.Entity<Account>(entity =>
        {
            entity.ToTable("accounts", table =>
            {
                table.HasCheckConstraint("ck_accounts_currency", "currency = 'THB'");
                table.HasCheckConstraint("ck_accounts_balance", "balance >= 0 AND balance <= 9999999999999999.99");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Balance).HasPrecision(18, 2);
            entity.Property(x => x.Currency).IsRequired();
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<Transfer>(entity =>
        {
            entity.ToTable("transfers", table =>
            {
                table.HasCheckConstraint("ck_transfers_accounts", "source_account_id <> destination_account_id");
                table.HasCheckConstraint("ck_transfers_amount", "amount > 0 AND amount <= 9999999999999999.99");
                table.HasCheckConstraint("ck_transfers_currency", "currency = 'THB'");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.SourceAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.DestinationAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<TransactionEntry>(entity =>
        {
            entity.ToTable("transaction_entries", table =>
            {
                table.HasCheckConstraint("ck_entries_sequence", "sequence > 0");
                table.HasCheckConstraint("ck_entries_amount", "amount > 0 AND amount <= 9999999999999999.99");
                table.HasCheckConstraint("ck_entries_balance", "resulting_balance >= 0 AND resulting_balance <= 9999999999999999.99");
                table.HasCheckConstraint("ck_entries_currency", "currency = 'THB'");
                table.HasCheckConstraint("ck_entries_type_refs",
                    "(type IN ('DEPOSIT', 'WITHDRAWAL') AND transfer_id IS NULL AND counterparty_account_id IS NULL) OR " +
                    "(type IN ('TRANSFER_IN', 'TRANSFER_OUT') AND transfer_id IS NOT NULL AND counterparty_account_id IS NOT NULL)");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Sequence).ValueGeneratedNever();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.ResultingBalance).HasPrecision(18, 2);
            entity.Property(x => x.Type).IsRequired();
            entity.Property(x => x.Currency).IsRequired();
            entity.HasIndex(x => x.Sequence).IsUnique();
            entity.HasIndex(x => new { x.AccountId, x.Sequence });
            entity.HasIndex(x => new { x.TransferId, x.Type }).IsUnique().HasFilter("transfer_id IS NOT NULL");
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.CounterpartyAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Transfer>().WithMany().HasForeignKey(x => x.TransferId).OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<SeedRecord>(entity =>
        {
            entity.ToTable("seed_records", table => table.HasCheckConstraint(
                "ck_seed_entity_type", "entity_type IN ('customers', 'accounts', 'transfers', 'transaction_entries')"));
            entity.HasKey(x => new { x.EntityType, x.RecordId });
            entity.Property(x => x.EntityType).IsRequired();
            entity.Property(x => x.SourceFingerprint).IsRequired();
        });

        foreach (var entity in model.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
            }
        }
    }
}
