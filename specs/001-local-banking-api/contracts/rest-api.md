# REST Contract: Fictional Local Banking

**Base URL**: `http://127.0.0.1:8080/api`
**Scope**: Trusted private workshop, fictional data, no authentication.
**Format**: JSON, camelCase fields; UUID identifiers; UTC ISO 8601 timestamps preserving imported microsecond precision.

## Shared representations

- Money is a JSON **string**, never a JSON number. Requests accept `^[0-9]{1,16}(\.[0-9]{1,2})?$`, additionally constrained to positive values through `9999999999999999.99`. Reject whitespace, signs, separators, exponents, more than two fractional digits, zero, and numeric JSON tokens. Responses always have exactly two fractional digits.
- Currency is `THB`. The service checks that request currency matches the selected account(s); no conversion.
- Lists return `{ "items": [...], "page": 1, "pageSize": 50, "totalCount": 150 }`. Query `page` starts at 1; `pageSize` defaults to 50 and is 1 through 100. Invalid pagination is 400.
- Customers/accounts sort by UUID; transfers sort by `completedAt`, then UUID; account history sorts by ascending `sequence`. All pages remain retrievable. Verification snapshots are taken with no competing writers.
- Unknown UUID resources are 404; malformed UUIDs are 400, including route values. Avoid UUID route constraints that silently turn malformed input into 404.

## Endpoints

| Method | Path | Purpose | Success |
|---|---|---|---|
| GET | `/customers?page=1&pageSize=50` | Browse existing customers. | 200 Customer page |
| GET | `/customers/{customerId}` | Read fictional profile. | 200 Customer |
| GET | `/customers/{customerId}/accounts?page=1&pageSize=50` | Browse that customer's accounts. | 200 Account page |
| GET | `/accounts?page=1&pageSize=50` | Browse all accounts for transfer selection. | 200 Account page |
| GET | `/accounts/{accountId}` | Read account and current balance. | 200 Account |
| GET | `/accounts/{accountId}/balance` | Read current balance explicitly. | 200 Balance |
| GET | `/accounts/{accountId}/transactions?page=1&pageSize=50` | Read complete paginated account history. | 200 TransactionEntry page |
| GET | `/transactions/{entryId}` | Read a completed entry. | 200 TransactionEntry |
| POST | `/accounts/{accountId}/deposits` | Deposit fictional funds. | 201 MoneyOperation |
| POST | `/accounts/{accountId}/withdrawals` | Withdraw available fictional funds. | 201 MoneyOperation |
| GET | `/transfers?page=1&pageSize=50` | Browse completed transfers. | 200 Transfer page |
| GET | `/transfers/{transferId}` | Read a completed transfer. | 200 Transfer |
| POST | `/transfers` | Transfer atomically between distinct accounts. | 201 TransferOperation |

Existing parents with no child records return an empty page, not 404. No customer/account writes, deletion/closure, history modification, seed-import HTTP endpoint, login, or public deployment endpoints.

## DTO fields

**Customer**: `id`, `name`, `createdAt`, `englishName`, `email`, `phone`, `addressLine`, `subdistrict`, `district`, `province`, `postcode`, `dateOfBirth`, `occupation`, `nationalId`. Preserve original fictional values; birth date is `YYYY-MM-DD`.

**Account**: `id`, `customerId`, `currency`, `balance`, `createdAt`.

**Balance**: `accountId`, `currency`, `balance`.

**TransactionEntry**: `id`, `sequence`, `accountId`, `type`, `amount`, `currency`, `completedAt`, `resultingBalance`, `transferId`, `counterpartyAccountId`. Optional references are explicit nulls. Types are `DEPOSIT`, `WITHDRAWAL`, `TRANSFER_OUT`, `TRANSFER_IN`. Sequence is a decimal digit string in JSON to preserve the full PostgreSQL bigint range in JavaScript; sort it numerically, not lexically.

**Transfer**: `id`, `sourceAccountId`, `destinationAccountId`, `amount`, `currency`, `completedAt`.

**MoneyOperation**: `{ "accountId": "...", "currency": "THB", "balance": "101.00", "transaction": { ...TransactionEntry } }`. Location points to `/api/transactions/{id}`.

**TransferOperation**: `{ "transfer": { ...Transfer }, "sourceBalance": { ...Balance }, "destinationBalance": { ...Balance }, "entries": [ ...outgoingEntry, ...incomingEntry ] }`. Location points to `/api/transfers/{id}`. The balances/entries reflect that committed operation, while subsequent reads can reflect newer operations.

## Money request examples

Deposit/withdrawal body:

```json
{
  "amount": "100.00",
  "currency": "THB"
}
```

Transfer body:

```json
{
  "sourceAccountId": "00209a31-c483-41bb-a9fd-35530f8f680b",
  "destinationAccountId": "fc99d3e9-f19d-439e-87e4-c7a54b585c7d",
  "amount": "100.00",
  "currency": "THB"
}
```

A successful transfer creates one transfer plus exactly two entries, with the same transfer ID, currency, amount, and completion instant, and reciprocal counterparty references. Both original histories are retained unchanged.

## Errors and non-mutation

Errors use `application/problem+json` with standard ProblemDetails fields and extensions `code` and `traceId`. Validation problems may also include field-level `errors`. Do not include connection strings, SQL, stack traces, or profile data in errors.

| Status | Stable code | Meaning |
|---|---|---|
| 400 | `invalid_input` | Malformed UUID/body, required field missing, invalid pagination. |
| 400 | `invalid_amount` | Invalid decimal string, unsupported precision, nonpositive/out-of-range amount. |
| 400 | `same_account` | Source and destination are identical. |
| 400 | `currency_mismatch` | Unsupported currency or account/request mismatch. |
| 404 | `not_found` | Well-formed UUID not found. |
| 409 | `insufficient_funds` | Source balance cannot cover withdrawal/transfer. |
| 409 | `balance_limit_exceeded` | Credit would exceed supported balance range. |
| 503 | `operation_unavailable` | Database unavailable before commit. |
| 503 | `completion_unknown` | Commit was attempted but its completion could not be confirmed. Refresh and reconcile before submitting again. |
| 500 | `unexpected_error` | Unexpected failure, sanitized diagnostic response. |

```json
{
  "type": "about:blank",
  "title": "Insufficient funds",
  "status": 409,
  "detail": "The source account has insufficient funds for this operation.",
  "code": "insufficient_funds",
  "traceId": "..."
}
```

Deterministic validation/business rejections leave both balances, complete histories, and completed-transfer records unchanged. If a network failure or `completion_unknown` prevents the client from knowing whether a commit succeeded, it must refresh and reconcile rather than assume rejection or automatically resubmit. No idempotency key or automatic mutation retry is promised in this version.

## Development documentation and readiness

- `GET /openapi/v1.json`: generated by Microsoft.AspNetCore.OpenApi 10, Development only.
- `GET /swagger`: standalone SwaggerUI displaying the generated document, Development only.
- Both documentation routes return 404 outside Development.
- `GET /health`: lightweight readiness probe, 200 when the database is reachable, otherwise 503; no credentials or internal diagnostic details.
- Backend binds to `127.0.0.1:8080`. Frontend uses a same-origin `/api` proxy; no wildcard CORS allowance.

## Import command contract

`dotnet run --project src\backend\Banking.Api\Banking.Api.csproj --no-launch-profile -- --import-seed --seed-path <absolute-seed-directory>`

Requires Development environment, migrated schema, and configured connection string. It exits without opening HTTP ports, returns exit code 0 on success/no-op and nonzero on invalid input/conflict, and reports inserted/skipped counts per entity. Error output identifies file/row/UUID without dumping profile fields or credentials. All four files are required; import validates the complete set and commits records plus seed metadata together.
