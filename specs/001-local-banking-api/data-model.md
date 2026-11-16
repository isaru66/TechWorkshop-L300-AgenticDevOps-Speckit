# Data Model: Local Banking API

All ids are UUIDs. Timestamps are UTC with time zone. Money is `numeric(19,2)` (`BigDecimal`).

## customer

| Field | Type | Rules |
|---|---|---|
| id | uuid PK | Preserved from seed; generated for new customers |
| name | text | Required |
| created_at | timestamptz | Set on create; preserved from seed |
| english_name, email, phone | text | Required: name; others optional but validated (email format) |
| address_line, subdistrict, district, province, postcode | text | Optional |
| date_of_birth | date | Optional; must be in the past |
| occupation | text | Optional |
| national_id | text | Optional; fictional values only |

The full record is returned by the API (clarification Q1).

## account

| Field | Type | Rules |
|---|---|---|
| id | uuid PK | Preserved from seed |
| customer_id | uuid FK -> customer | Required; must exist |
| currency | char(3) | `THB` only; CHECK |
| balance | numeric(19,2) | CHECK `>= 0`; seed value loaded verbatim; new accounts start at 0.00 |
| created_at | timestamptz | |

## transfer

| Field | Type | Rules |
|---|---|---|
| id | uuid PK | |
| source_account_id, destination_account_id | uuid FK -> account | CHECK source <> destination |
| amount | numeric(19,2) | CHECK `> 0` |
| currency | char(3) | Must equal both accounts' currency |
| completed_at | timestamptz | |

## transaction_entry

| Field | Type | Rules |
|---|---|---|
| id | uuid PK | |
| sequence | bigint, unique | Global ordering key; DB sequence advanced past the seed maximum (1002) |
| account_id | uuid FK -> account | |
| type | enum | `DEPOSIT`, `WITHDRAWAL`, `TRANSFER_IN`, `TRANSFER_OUT` |
| amount | numeric(19,2) | CHECK `> 0` |
| currency | char(3) | |
| completed_at | timestamptz | |
| resulting_balance | numeric(19,2) | Account balance after this entry |
| transfer_id | uuid FK -> transfer, nullable | Required for transfer types; null otherwise |
| counterparty_account_id | uuid FK -> account, nullable | Required for transfer types; null otherwise |

Indexes: `account(customer_id)`, `transaction_entry(account_id, completed_at DESC, sequence DESC)`, `transaction_entry(transfer_id)`.

## Relationships

- customer 1 -> * account
- account 1 -> * transaction_entry
- transfer 1 -> 2 transaction_entry (`TRANSFER_OUT` on the source, `TRANSFER_IN` on the destination)

## State and invariants

- For a new operation: `new balance = old balance +/- amount`, and the entry's `resulting_balance` equals the new balance.
- A transfer changes both balances and inserts one transfer and two entries in one transaction, or changes nothing.
- A rejected operation writes nothing.
- Imported history is stored as-is. Imported `resulting_balance` values are not applied to balances, and rows are never overwritten on re-import.
- The latest entry for an account (by sequence) agrees with its balance for any account that has history.
