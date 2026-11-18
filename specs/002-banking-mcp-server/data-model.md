# Data Model: Banking MCP Server

Builds on [001 data model](../001-local-banking-api/data-model.md). Only the changes are listed.

## account (changed)

| Field | Type | Rules |
|---|---|---|
| status | text, NOT NULL, default `OPEN` | CHECK in (`OPEN`, `CLOSED`); existing rows become `OPEN` |
| label | text, nullable | Free-text descriptive name, max 100 chars; updatable |

## customer (no schema change)

Updatable fields: name (required, non-blank), english_name, email (format-validated), phone, address_line, subdistrict, district, province, postcode, date_of_birth (past), occupation, national_id. `id` and `created_at` immutable.

## State transitions

```text
Account:  OPEN --close (balance == 0)--> CLOSED   (terminal; no reopen)
Customer: exists --delete (no accounts, open or closed)--> removed
```

## Rules added

| Rule | Error code |
|---|---|
| Deposit/withdraw/transfer touching a CLOSED account is rejected | `ACCOUNT_CLOSED` |
| Close with balance != 0 is rejected | `NON_ZERO_BALANCE` |
| Close an already closed account is rejected | `ACCOUNT_CLOSED` |
| Delete a customer who has any account | `CUSTOMER_HAS_ACCOUNTS` |
| Update with blank name / invalid email | `VALIDATION_ERROR` |
| Unknown id | `NOT_FOUND` |

History (`transaction_entry`, `transfer`) is never modified or deleted; closed-account history stays readable. Existing rules (positive amounts, 2 dp, sufficient funds, same-account, currency) are unchanged.
