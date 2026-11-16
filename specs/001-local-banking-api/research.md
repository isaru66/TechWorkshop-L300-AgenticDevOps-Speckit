# Research: Local Banking API

No `NEEDS CLARIFICATION` items remain. The decisions below cover each technology choice.

## R1. Spring Boot version

- **Decision**: Use Spring Boot 4.1.x, currently 4.1.2, on Java 21.
- **Rationale**: Maven Central lists 4.1.0, 4.1.1 and 4.1.2 as released. Your fallback ("latest released 4.x") therefore does not change the choice. 4.2.0 is only milestone/RC.
- **Alternatives**: 4.2 RC (not released); 3.5.x (not requested).
- **Note**: `java` is not on PATH here. The installed JDK is Microsoft OpenJDK 25.0.4.1 (`C:\Users\isarar\.jdks\ms-25.0.4.1`), plus OpenJDK 24. No JDK 21 is installed. Plan: build with JDK 25 using `maven.compiler.release=21`, so the code targets Java 21 as requested, and run the tests on JDK 25. Maven 3.9.9 is present. Install a JDK 21 if you need to run on 21 itself.

## R2. Money and atomicity

- **Decision**: `numeric(19,2)` columns, `BigDecimal` in code, checks `amount > 0` and `balance >= 0`. Amounts with more than 2 decimals are rejected, never rounded.
- **Transfer atomicity**: one `@Transactional` service method. It locks both account rows with `SELECT ... FOR UPDATE` in ascending UUID order (prevents deadlocks), checks rules, updates both balances, and inserts one transfer plus two entries. Any failure rolls everything back.
- **Rationale**: Row locks give correct concurrent behaviour without a global lock; the DB `CHECK` is a last line of defence.
- **Alternatives**: optimistic locking with retries (more complex, retry noise); serializable isolation (heavier).

## R3. Idempotent seed import

- **Decision**: An `ApplicationRunner` runs on every start in one transaction. Files are parsed and validated first (unique IDs, resolvable references, THB, amounts). Rows are then inserted in dependency order (customers, accounts, transfers, entries) with `INSERT ... ON CONFLICT (id) DO NOTHING`.
- **Why this satisfies the spec**: existing UUIDs are skipped, so there are no duplicates and no overwrites of later changes. Balances are loaded verbatim and history is loaded as data, never executed, so nothing is replayed.
- **Partial-import recovery**: because the import is one transaction, a failure leaves nothing behind and the next start retries.
- **Entry sequence**: seed entries carry sequence 1..1002. A database sequence is advanced past the maximum after import (`setval` to `max(sequence)`), so new entries get higher numbers.
- **Interrupted data / drift**: a seed row with the same UUID but different values is skipped and counted, and logged without personal data.
- **Alternatives**: manual import endpoint (rejected: you chose automatic at startup); upsert (overwrites changes, rejected).

## R4. Migrations and schema

- **Decision**: Flyway SQL migrations create tables, foreign keys, checks and indexes. Hibernate `ddl-auto` is set to `validate`.
- **Rationale**: Explicit and reviewable, and `numeric` precision is controlled.

## R5. History pagination

- **Decision**: `page` (0-based) and `size` (default 20, max 100), ordered by `completed_at DESC, sequence DESC`. The response includes the total count.
- **Rationale**: Simple for the UI; a stable tiebreaker guarantees each entry appears once. Out-of-range sizes return 400.
- **Alternatives**: keyset cursors (better for heavy writes, unnecessary here).

## R6. API documentation

- **Decision**: springdoc-openapi (Spring Boot 4 compatible release) exposes `/v3/api-docs` and Swagger UI at `/swagger-ui.html`, enabled in the default/local profile only. Verify the exact springdoc version against Boot 4.1 at implementation time.
- **Contract**: `contracts/openapi.yaml` is the design-time contract; the generated document must match it.

## R7. Frontend

- **Decision**: React + TypeScript on Vite, dev server on port 5173, `/api` proxied to `http://localhost:8080`. TanStack Query for data, React Router for pages, plain CSS variables for the theme.
- **Theme (your choice)**: Azure-style blue and white. Primary blue `#0078D4`, hover `#106EBE`, dark text `#201F1E`, surface white, light panel `#F3F2F1`, and an accessible focus ring. The theme uses generic Azure-style tokens, with no logos or customer branding, so it stays reusable.
- **No login**: no auth screens, tokens or guards.
- **CORS**: not needed through the proxy. If the UI is called directly, allow only `http://localhost:5173`.

## R8. Network privacy

- **Decision**: Bind the backend to loopback (`server.address=127.0.0.1`), publish the database container on `127.0.0.1` only, and bind the Vite dev server to localhost. Don't use public port forwarding. If Dev Container or Codespaces forwarding is in play, keep it private.
- **Database settings**: supplied through environment variables (`DB_URL`, `DB_USER`, `DB_PASSWORD`) and never committed. `.env` is already git-ignored. Values are deliberately not recorded in these docs, as you asked.

## R9. Testing

- **Decision**: Testcontainers PostgreSQL for integration tests, so tests don't touch your local database. They cover import idempotency, balance preservation, transfer success and rejection, and a concurrency test. The service has plain unit tests. The frontend gets component tests with the API mocked.
- **Prerequisite**: Docker must be running.

## R10. Verification approach

A scripted check selects two distinct same-currency accounts, with the source holding enough funds, captures balances and histories for both, performs a successful transfer and compares, then attempts a transfer larger than the source balance and compares again. See [quickstart.md](quickstart.md).
