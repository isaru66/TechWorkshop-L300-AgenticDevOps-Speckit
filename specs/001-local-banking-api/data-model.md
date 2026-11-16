# Data Model: Fictional Local Banking

**Storage**: PostgreSQL 17 through EF Core 10/Npgsql 10.
**Mapping**: UUID to C# `Guid`; `numeric(18,2)` to `decimal`; `timestamptz` to UTC `DateTimeOffset`; `date` to `DateOnly`; historical sequence to `long`.

## Customer

Table `customers`; primary key `id uuid`.

| Source field | Storage | Rules |
|---|---|---|
| id | uuid | Required, unique, preserved verbatim as an identity. |
| name | text | Required; preserve fictional Thai text. |
| created_at | timestamptz | Required; preserve original instant at microsecond precision. |
| english_name, email, phone | text | Preserve supplied profile values. |
| address_line, subdistrict, district, province, postcode | text | Preserve text, including postcode formatting. |
| date_of_birth | date | Preserve supplied date, without timezone conversion. |
| occupation, national_id | text | Fictional text only; preserve nonnumeric demo identifiers. |

Non-key profile fields other than name may be empty when allowed by the source schema; canonicalization must preserve the distinction between empty text and absent required fields. Do not normalize names or impose real-world identity validation on fictional identifiers.

Relationship: one customer to many accounts. No customer mutation interface.

## Account

Table `accounts`.

| Field | Storage | Rules |
|---|---|---|
| id | uuid PK | Preserve imported UUID. |
| customer_id | uuid FK customers | Required, indexed; deletion restricted. |
| currency | text | Required, exactly `THB` for this baseline and new operations. |
| balance | numeric(18,2) | Required, finite, 0 through 9999999999999999.99. |
| created_at | timestamptz | Preserve original instant. |

Only the banking service changes live balances. Import supplies the initial balance once; a later import does not reconcile it back to the original value.

## Transfer

Table `transfers`.

| Field | Storage | Rules |
|---|---|---|
| id | uuid PK | Imported identity preserved; new identity generated once per completed operation. |
| source_account_id | uuid FK accounts | Required, indexed. |
| destination_account_id | uuid FK accounts | Required, indexed; distinct from source. |
| amount | numeric(18,2) | Positive, finite, within supported range. |
| currency | text | THB; same as both linked accounts. |
| completed_at | timestamptz | Imported instant preserved; new value set on completion. |

Exactly two corresponding entries for every completed transfer: one `TRANSFER_OUT` for source and one `TRANSFER_IN` for destination. Transfer and entries share amount/currency/completion time. No pending or rejected transfer rows.

## Transaction Entry

Table `transaction_entries`.

| Field | Storage | Rules |
|---|---|---|
| id | uuid PK | Imported identity preserved; new identity generated for each new entry. |
| sequence | bigint, unique | Positive imported order preserved. New values from `transaction_entry_sequence`. |
| account_id | uuid FK accounts | Required. |
| type | text | `DEPOSIT`, `WITHDRAWAL`, `TRANSFER_OUT`, or `TRANSFER_IN`. |
| amount | numeric(18,2) | Positive and finite; direction comes from type, not a negative amount. |
| currency | text | Same currency as account and linked transfer, if any. |
| completed_at | timestamptz | Preserve historical instant. |
| resulting_balance | numeric(18,2) | Nonnegative and finite; historical value, not an instruction to update current balance. |
| transfer_id | uuid nullable FK transfers | Required for transfer types; empty for deposit/withdrawal. |
| counterparty_account_id | uuid nullable FK accounts | Required for transfer types; empty for deposit/withdrawal. |

Index `(account_id, sequence)` for complete ordered histories. Unique filtered `(transfer_id, type)` prevents duplicate entry directions for a transfer. Foreign-key deletes are restricted. Service/import validation enforces cross-record agreement and exactly two sides, in the same transaction; an ordinary row-level check constraint cannot enforce cross-table completeness.

Imported history is immutable. New history is append-only, with gaps allowed in its sequence. Historical resulting balances are not recalculated, and the importer must not infer the current account balance by replaying entries.

## Accepted Seed Record

Table `seed_records`; internal import metadata, not exposed by REST.

| Field | Storage | Rules |
|---|---|---|
| entity_type | text | One of the four entity/table names. |
| record_id | uuid | UUID from the source record. |
| canonical_version | integer | Initially 1; defines the exact fingerprint normalization. |
| source_fingerprint | text | SHA-256 hex of all canonical original source fields. |
| imported_at | timestamptz | Time the original source row was first accepted. |

Composite primary key `(entity_type, record_id)`. Canonical input is a fixed-order UTF-8 field array per CSV header, with invariant exact decimals, UUIDs in standard lowercase form, timestamps as UTC instants at microsecond precision, dates as ISO dates, and explicit empty/null representation. Preserve original text exactly; do not trim names or addresses. Canonicalization removes only representational differences such as CSV quoting or equivalent UTC timestamp spellings.

An existing entity UUID without corresponding seed metadata is a collision, not permission to overwrite or retroactively adopt the row. Missing previously accepted seed rows in a later input are treated as a conflicting/incomplete baseline, not as deletion requests. Newly supplied UUIDs may be imported only when the complete extended baseline validates and no existing accepted source row has changed or disappeared.

## Numeric and input invariants

- Database checks bound balances/resulting balances to `[0, 9999999999999999.99]` and amounts to `(0, 9999999999999999.99]`; explicit finite upper bounds also reject PostgreSQL numeric `NaN`.
- Service/import validation rejects precision beyond two fractional digits before storing; do not rely on scale coercion, which could round.
- All API money is represented as strings; backend DTOs parse into C# decimal invariantly. UI arithmetic uses integer satang, not JavaScript Number.
- Imported UUIDs and all populated references must resolve; duplicates, invalid types, unsupported currency, invalid decimals, and mismatched transfer sides reject the complete import.
- Initial baseline totals are 102 customers, 150 accounts, 101 transfers, and 1,002 entries. Counts are acceptance fixtures, not hardcoded importer limits.

## State transitions and transactions

| Action | Accepted transition | Rejected transition |
|---|---|---|
| Initial import | Validated baseline -> entities plus metadata in one commit; set next entry sequence above all imported values. | No entity/metadata changes. |
| Repeat import | Same fingerprints -> no live changes; permitted new source rows inserted atomically. | Any source conflict/collision/invalid reference -> no changes. |
| Deposit | B -> B + A; one deposit entry. | No balance/history change. |
| Withdrawal | B -> B - A where B >= A; one withdrawal entry. | No balance/history change. |
| Transfer | Source B -> B - A; destination C -> C + A; one transfer and two linked entries. | Neither account nor history changes; no transfer row. |
| Normal restart | All committed records remain. | No implicit reset/import/replay. |

For each mutation: begin a transaction; take the shared import-coordination advisory lock; acquire affected account row locks in database UUID order; validate current funds/currency/range; allocate history sequences; write all changes; commit. Import takes the exclusive advisory lock before inspecting seed metadata or advancing the sequence and holds it through commit. A failed operation may consume sequence numbers but leaves no completed records.

No automatic request/transaction replay after an uncertain commit; request idempotency is outside the approved scope. Controlled rollback faults and concurrent mutations are verified against actual PostgreSQL.
