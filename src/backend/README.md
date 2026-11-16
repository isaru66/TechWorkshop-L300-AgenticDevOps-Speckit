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
