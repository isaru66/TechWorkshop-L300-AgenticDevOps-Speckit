# Contract: MCP Tools

**Endpoint**: `POST http://127.0.0.1:8080/mcp` — Streamable HTTP, stateless, JSON-RPC 2.0, no authentication, no session header.

**Required request headers**: `Content-Type: application/json`, `Accept: application/json, text/event-stream`.

**Methods**: `initialize`, `notifications/initialized`, `tools/list`, `tools/call`.

**Conventions**: ids are UUID strings; `amount` is a decimal string (e.g. `"250.00"`, max 2 dp, > 0); currency is THB. Paged tools take `page` (default 0) and `size` (default 20, max 100). Failures return a tool result with `isError: true` and text `CODE: message`.

| Tool | Type | Inputs | Result |
|---|---|---|---|
| `list_customers` | read | page, size | page of customers |
| `search_customers` | read | query, page, size | page of matching customers |
| `get_customer` | read | customerId | customer |
| `create_customer` | write | name (req), englishName, email, phone, addressLine, subdistrict, district, province, postcode, dateOfBirth, occupation, nationalId | created customer |
| `update_customer` | write | customerId, same fields as create | updated customer |
| `delete_customer` | write (destructive) | customerId | confirmation; `CUSTOMER_HAS_ACCOUNTS` if any account exists |
| `list_customer_accounts` | read | customerId | accounts with balance and status |
| `get_account` | read | accountId | account |
| `get_account_balance` | read | accountId | balance, currency |
| `open_account` | write | customerId, label (opt) | new account (balance 0.00, OPEN) |
| `update_account` | write | accountId, label | updated account |
| `close_account` | write (destructive) | accountId | account with status CLOSED; `NON_ZERO_BALANCE` if balance != 0 |
| `deposit` | write | accountId, amount | transaction entry |
| `withdraw` | write | accountId, amount | transaction entry; `INSUFFICIENT_FUNDS` |
| `transfer_funds` | write | sourceAccountId, destinationAccountId, amount | transfer details |
| `get_transaction_history` | read | accountId, page, size | page of entries, newest first |

Error codes: `NOT_FOUND`, `VALIDATION_ERROR`, `INSUFFICIENT_FUNDS`, `SAME_ACCOUNT`, `CURRENCY_MISMATCH`, `ACCOUNT_CLOSED`, `NON_ZERO_BALANCE`, `CUSTOMER_HAS_ACCOUNTS`.

Each tool carries a description and per-parameter descriptions so `tools/list` is self-documenting (FR-012). The text content of every result is the full JSON (REST DTO shapes). Object results additionally carry `structuredContent` (same data, null fields omitted) so clients such as REST Client can read ids without parsing text; list results (e.g. `list_customer_accounts`) return text content only.
