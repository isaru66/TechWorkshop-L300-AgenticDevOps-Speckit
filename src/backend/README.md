# Local Banking API (backend)

Fictional, local-only banking REST API: Spring Boot 4.1, Java 21 bytecode
(`maven.compiler.release=21`), Maven 3.9, PostgreSQL. No authentication; it
listens on `127.0.0.1:8080` only. Use fictional data only and keep endpoints
private; see the [root README](../../README.md).

## Configuration

Set these environment variables for a local PostgreSQL database you control:
`DB_URL` (JDBC URL), `DB_USER`, `DB_PASSWORD`.

Seed CSVs are read from `../data/seed` (override with `banking.seed.path`), so
run from this directory.

## Build and test

```powershell
mvn -B clean verify
```

Tests use Testcontainers (Docker required) and never touch your local database.

## Run

```powershell
mvn spring-boot:run
```

On startup the seed is imported insert-only: existing UUIDs are skipped, later
changes are never overwritten and history is never replayed into balances.

- Swagger UI: http://localhost:8080/swagger-ui.html
- OpenAPI: http://localhost:8080/v3/api-docs
- Health: http://localhost:8080/actuator/health

REST verification steps: `specs/001-local-banking-api/quickstart.md`.

## MCP server (AI assistants)

The app also exposes a Model Context Protocol server at `http://127.0.0.1:8080/mcp`
(Streamable HTTP, **stateless**: no session id). It has **no authentication**; keep the
default `127.0.0.1` binding and use fictional data only.

Tools: `list_customers`, `search_customers`, `get_customer`, `create_customer`,
`update_customer`, `delete_customer`, `list_customer_accounts`, `get_account`,
`get_account_balance`, `open_account`, `update_account`, `close_account`, `deposit`,
`withdraw`, `transfer_funds`, `get_transaction_history`. They call the same service as the
REST API, so rules and data are shared. A customer can be deleted only when it has no
accounts; an account can be closed only at a zero balance and then rejects money movement
(its history stays readable).

Try it with the REST Client samples in [`test.http`](test.http) (initialize, tool listing,
every tool, error cases, expected results). Connect an assistant, for example with
`.vscode/mcp.json`: `{"servers": {"banking": {"type": "http", "url": "http://127.0.0.1:8080/mcp"}}}`.

Details: `specs/002-banking-mcp-server/`.
