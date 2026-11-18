# MCP Tool Contract

Endpoint: `POST http://127.0.0.1:8080/mcp` (Streamable HTTP, stateless, no auth).
Required headers: `Content-Type: application/json`, `Accept: application/json, text/event-stream`. No `Mcp-Session-Id`.
Amounts: decimal strings (`"100.00"`), currency `THB`. IDs: GUID strings. Lists use `page` (default 1) / `pageSize` (default 50).

| Tool | Kind | Inputs | Result |
|------|------|--------|--------|
| `list_customers` | read | `page?`, `pageSize?` | page of CustomerDto |
| `get_customer` | read | `customerId` | CustomerDto |
| `create_customer` | write | `name`, optional profile fields | CustomerDto |
| `update_customer` | write | `customerId`, any profile fields | CustomerDto |
| `delete_customer` | write | `customerId` | `{deleted:true}`; `has_dependents` if accounts exist |
| `list_accounts` | read | `customerId?`, `page?`, `pageSize?` | page of AccountDto |
| `get_account` | read | `accountId` | AccountDto |
| `create_account` | write | `customerId` | AccountDto (balance `0.00`) |
| `delete_account` | write | `accountId` | `{deleted:true}`; `has_dependents` if balance ≠ 0 or history exists |
| `deposit` | write | `accountId`, `amount` | MoneyOperationDto |
| `withdraw` | write | `accountId`, `amount` | MoneyOperationDto |
| `transfer_funds` | write | `sourceAccountId`, `destinationAccountId`, `amount` | TransferOperationDto |
| `get_transaction_history` | read | `accountId`, `page?`, `pageSize?` | page of TransactionEntryDto, newest first |

Notes:
- Accounts have no mutable attributes, so there is no `update_account`; balances change only via deposit/withdraw/transfer (documented in spec assumptions).
- Currency is always `THB` and supplied implicitly by tools.
- DTO shapes match `Banking.Api/Contracts/ReadDtos.cs`, `TransferDtos.cs`, `MoneyOperationDtos.cs`.

## Errors
Tool failures return `result.isError = true` with text `"<code>: <detail>"`. Codes: `invalid_input`, `invalid_amount`, `not_found`, `same_account`, `currency_mismatch`, `insufficient_funds`, `balance_limit_exceeded`, `has_dependents`, `unexpected_error`. Protocol errors (malformed JSON-RPC, unknown tool) use JSON-RPC `error`.

## REST compatibility
`/api/*` and `/health` routes, payloads, and status codes are unchanged; verified by existing tests plus a regression test.
