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

`Banking.Api.Tests` requires an isolated `banking_test` PostgreSQL database on
loopback port 5433. It creates uniquely named schemas and removes only those
test-owned schemas. It never resets the development database.
