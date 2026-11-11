# Fictional seed data

The repository tracks these four fictional CSV files. They are a seed
baseline, not a populated database or an implemented import process.

| File | Contents and dependencies |
|---|---|
| `customers.csv` | Customers with UUID `id` values. |
| `accounts.csv` | Accounts linked to customers by `customer_id`, with THB currency and existing balances. |
| `transfers.csv` | Historical transfers in THB, linked to `source_account_id` and `destination_account_id`. |
| `transaction_entries.csv` | Historical entries linked to `account_id`, with resulting balances and optional `transfer_id` and `counterparty_account_id` references. |

Primary keys and relationship IDs are UUIDs; the recorded currency is THB.
Plan to import customers first, then accounts, transfers, and transaction
entries. Preserve the IDs, amounts, currency, balances, timestamps, and
relationships. Validate that IDs are unique and populated references resolve,
and make initialization repeatable without duplicating already imported
records.

The account balances are part of the imported baseline. Import transfers and
transaction entries as existing history; do **not** replay those historical
deposits, withdrawals, or transfers over the imported balances. An entry's
`resulting_balance` is historical information, not an additional operation.

Use only obviously fictional data. Do not include real personal or financial
information, production data, passwords, tokens, or other secrets. Review any
files before committing or sharing them. Import into a private, trusted
development environment; the intended application has no application
authentication.
