using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Banking.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialBanking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "transaction_entry_sequence");

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    english_name = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    address_line = table.Column<string>(type: "text", nullable: true),
                    subdistrict = table.Column<string>(type: "text", nullable: true),
                    district = table.Column<string>(type: "text", nullable: true),
                    province = table.Column<string>(type: "text", nullable: true),
                    postcode = table.Column<string>(type: "text", nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    occupation = table.Column<string>(type: "text", nullable: true),
                    national_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "seed_records",
                columns: table => new
                {
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    canonical_version = table.Column<int>(type: "integer", nullable: false),
                    source_fingerprint = table.Column<string>(type: "text", nullable: false),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_records", x => new { x.entity_type, x.record_id });
                    table.CheckConstraint("ck_seed_entity_type", "entity_type IN ('customers', 'accounts', 'transfers', 'transaction_entries')");
                });

            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.id);
                    table.CheckConstraint("ck_accounts_balance", "balance >= 0 AND balance <= 9999999999999999.99");
                    table.CheckConstraint("ck_accounts_currency", "currency = 'THB'");
                    table.ForeignKey(
                        name: "FK_accounts_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfers", x => x.id);
                    table.CheckConstraint("ck_transfers_accounts", "source_account_id <> destination_account_id");
                    table.CheckConstraint("ck_transfers_amount", "amount > 0 AND amount <= 9999999999999999.99");
                    table.CheckConstraint("ck_transfers_currency", "currency = 'THB'");
                    table.ForeignKey(
                        name: "FK_transfers_accounts_destination_account_id",
                        column: x => x.destination_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfers_accounts_source_account_id",
                        column: x => x.source_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transaction_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resulting_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    counterparty_account_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_entries", x => x.id);
                    table.CheckConstraint("ck_entries_amount", "amount > 0 AND amount <= 9999999999999999.99");
                    table.CheckConstraint("ck_entries_balance", "resulting_balance >= 0 AND resulting_balance <= 9999999999999999.99");
                    table.CheckConstraint("ck_entries_currency", "currency = 'THB'");
                    table.CheckConstraint("ck_entries_sequence", "sequence > 0");
                    table.CheckConstraint("ck_entries_type_refs", "(type IN ('DEPOSIT', 'WITHDRAWAL') AND transfer_id IS NULL AND counterparty_account_id IS NULL) OR (type IN ('TRANSFER_IN', 'TRANSFER_OUT') AND transfer_id IS NOT NULL AND counterparty_account_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_transaction_entries_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transaction_entries_accounts_counterparty_account_id",
                        column: x => x.counterparty_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transaction_entries_transfers_transfer_id",
                        column: x => x.transfer_id,
                        principalTable: "transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_customer_id",
                table: "accounts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_entries_account_id_sequence",
                table: "transaction_entries",
                columns: new[] { "account_id", "sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_transaction_entries_counterparty_account_id",
                table: "transaction_entries",
                column: "counterparty_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_entries_sequence",
                table: "transaction_entries",
                column: "sequence",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_entries_transfer_id_type",
                table: "transaction_entries",
                columns: new[] { "transfer_id", "type" },
                unique: true,
                filter: "transfer_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_destination_account_id",
                table: "transfers",
                column: "destination_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_source_account_id",
                table: "transfers",
                column: "source_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seed_records");

            migrationBuilder.DropTable(
                name: "transaction_entries");

            migrationBuilder.DropTable(
                name: "transfers");

            migrationBuilder.DropTable(
                name: "accounts");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropSequence(
                name: "transaction_entry_sequence");
        }
    }
}
