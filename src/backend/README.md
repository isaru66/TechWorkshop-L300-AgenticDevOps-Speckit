# Banking API

`Banking.Api` targets .NET 10 and uses controllers, EF Core 10 with Npgsql,
and PostgreSQL. The scoped banking service performs exact-decimal deposits,
withdrawals, and transfers atomically with their history. The seed import is a
Development-only CLI command; it preserves original balances and safely skips
previously accepted source records.

From the repository root, follow the
[quickstart](../../specs/001-local-banking-api/quickstart.md) for private
database credentials, migration, import, build, tests, and startup. In
Development, the API listens on `127.0.0.1:8080`; `/openapi/v1.json` and
`/swagger` expose its local API documentation.

## MCP server

The API also hosts a Model Context Protocol server at `http://127.0.0.1:8080/mcp`
(Streamable HTTP, stateless, no `Mcp-Session-Id`). It exposes 13 tools backed by
the same banking service and PostgreSQL data: `list_customers`, `get_customer`,
`create_customer`, `update_customer`, `delete_customer`, `list_accounts`,
`get_account`, `create_account`, `delete_account`, `deposit`, `withdraw`,
`transfer_funds`, and `get_transaction_history` (newest first). Tool failures
return `isError: true` with text containing `<code>: <detail>`.

**Demo only: there is no authentication.** Keep it bound to loopback.

Try it with [`Banking.Api/test.http`](Banking.Api/test.http) (VS Code REST Client),
or add to `.vscode/mcp.json`:

```json
{ "servers": { "banking": { "type": "http", "url": "http://127.0.0.1:8080/mcp" } } }
```

See [specs/002-mcp-banking-server](../../specs/002-mcp-banking-server/quickstart.md).

`Banking.Api.Tests` requires an isolated `banking_test` PostgreSQL database on
loopback port 5433. It creates uniquely named schemas and removes only those
test-owned schemas. It never resets the development database.
