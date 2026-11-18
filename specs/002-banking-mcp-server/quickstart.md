# Quickstart: Banking MCP Server

Validation guide. Tool details: [contracts/mcp-tools.md](contracts/mcp-tools.md); data rules: [data-model.md](data-model.md).

## Prerequisites
- Java 21, Maven, Docker (PostgreSQL), VS Code with the **REST Client** extension.
- Backend started as in the 001 quickstart (`DB_URL`, `DB_USER`, `DB_PASSWORD` set; seed data loaded). Flyway applies `V2` on startup.
- **Security**: the MCP endpoint has no authentication. Keep the default `127.0.0.1` binding.

## Run
```powershell
cd src\backend
mvn spring-boot:run
```
MCP endpoint: `http://127.0.0.1:8080/mcp` (Streamable HTTP, stateless).

## Validate with `src/backend/test.http`
Open the file in VS Code and click **Send Request** in order. Headers on every MCP call: `Content-Type: application/json` and `Accept: application/json, text/event-stream`. No session id is needed (stateless).

| # | Request | Expected |
|---|---|---|
| 1 | `initialize` (protocolVersion, clientInfo) | 200; result has `serverInfo` and `capabilities.tools` |
| 2 | `notifications/initialized` | 202/200 with no body |
| 3 | `tools/list` | 16 tools from the contract, each with description and input schema |
| 4 | `search_customers` (`query` = seeded name) | seeded customer appears; id captured for later requests |
| 5 | `list_customer_accounts`, `get_account_balance` | accounts with status `OPEN`, numeric balance |
| 6 | `create_customer` -> `open_account` x2 | new customer; two accounts with `0.00` |
| 7 | `deposit` 1000.00 -> `withdraw` 200.00 -> `transfer_funds` 300.00 | balances 500.00 / 300.00 |
| 8 | `get_transaction_history` | newest first: transfer out, withdrawal, deposit |
| 9 | Error cases: overdraw, negative amount, same-account transfer, unknown id | `isError: true` with `INSUFFICIENT_FUNDS`, `VALIDATION_ERROR`, `SAME_ACCOUNT`, `NOT_FOUND` |
| 10 | `delete_customer` while accounts exist | `CUSTOMER_HAS_ACCOUNTS` |
| 11 | Withdraw all funds, `close_account` on both accounts | status `CLOSED`; deposit on a closed account -> `ACCOUNT_CLOSED`; history still readable; `delete_customer` still refused (`CUSTOMER_HAS_ACCOUNTS`) so history is preserved |
| 11b | `create_customer` (no accounts) -> `delete_customer` | delete succeeds; `get_customer` -> `NOT_FOUND` |
| 12 | REST section of `test.http` (`GET /api/accounts/{id}/transactions`) | shows the same entries created via MCP (FR-013) |

Responses may arrive as plain JSON or SSE-framed (`data: {...}`); the payload is the same JSON-RPC message.

## Automated
```powershell
cd src\backend
mvn verify
```
Expect existing 001 tests plus `McpToolsIT` and `AccountLifecycleIT` to pass (Testcontainers needs Docker).

## Connect an assistant (optional)
VS Code `.vscode/mcp.json`: `{"servers": {"banking": {"type": "http", "url": "http://127.0.0.1:8080/mcp"}}}`
