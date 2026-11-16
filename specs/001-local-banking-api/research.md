# Research: Fictional Local Banking

**Date**: 2026-11-16
**Scope**: Phase 0 design research; no application implementation.

## 1. Requested backend stack

**Decision**: Use `net10.0`, controller-based ASP.NET Core 10, EF Core 10, and Npgsql.EntityFrameworkCore.PostgreSQL 10. Pin compatible stable patches during implementation rather than mixing framework majors.

**Rationale**: Npgsql's official release documentation describes provider 10 support for EF 10. One API project and one service layer satisfy the workshop scope.

**Alternatives considered**: Minimal APIs and Java are not the requested controller-based .NET path. Extra domain/repository projects add no required behavior.

**Source**: `https://www.npgsql.org/efcore/release-notes/10.0.html`

## 2. OpenAPI generation versus display

**Decision**: Microsoft.AspNetCore.OpenApi 10 generates the document using `AddOpenApi`/`MapOpenApi`. A separately referenced compatible Swashbuckle.AspNetCore.SwaggerUI package renders `/openapi/v1.json`; both document and UI routes are Development-only. SwaggerUI 10.0.0 is a verified compatible baseline, not a claim about the latest patch.

**Rationale**: Microsoft's documented integration separates generation from visualization. SwaggerUI's own release line is independent of the framework.

**Alternatives considered**: `AddSwaggerGen`/`UseSwagger` would add a second document generator. An always-enabled documentation surface contradicts the requested environment restriction.

**Sources**:
- `https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0`
- `https://www.nuget.org/packages/Swashbuckle.AspNetCore.SwaggerUI/10.0.0`

## 3. Exact money across all boundaries

**Decision**: C# `decimal`, explicitly mapped `numeric(18,2)`, with positive-operation and nonnegative-balance checks, finite upper bounds, and precision validation before persistence. Monetary JSON fields are invariant decimal strings; the React client retains strings and uses integer minor units when needed.

**Rationale**: Npgsql maps numeric to decimal. PostgreSQL rounds excess scale, so rejecting it in the service is necessary. Strings avoid losing precision when JavaScript parses response JSON. The largest supported value is `9999999999999999.99`; reject a deposit or destination credit that would exceed it.

**Alternatives considered**: Floating point and PostgreSQL money are unnecessary. Number-valued JSON plus ordinary JavaScript arithmetic would undermine the exact-money contract. Silently rounding inputs violates the specification.

**Sources**:
- `https://www.npgsql.org/doc/types/basic.html`
- `https://www.postgresql.org/docs/17/datatype-numeric.html`

## 4. Atomic operations and concurrent spending

**Decision**: Every balance writer uses an explicit transaction, a shared advisory transaction lock for import coordination, and account-row `FOR UPDATE` locks acquired in PostgreSQL UUID order. Validate after locking; save balances, transfer, and entries together. Import uses the corresponding exclusive advisory transaction lock.

**Rationale**: Row locks serialize competing mutations of the same account. Consistent acquisition order avoids application-created opposite-direction lock inversions; advisory locks coordinate the otherwise independent importer. This is a design inference from PostgreSQL's locking mechanisms, not a built-in banking facility.

**Alternatives considered**: Process-local locks do not protect multiple processes. Unprotected read-modify-write permits lost updates. Serializable isolation or optimistic retries are viable but add retry complexity for a small workshop.

**Source**: `https://www.postgresql.org/docs/17/explicit-locking.html`

## 5. History order and retry boundaries

**Decision**: Preserve imported UUIDs and sequences; generate new UUIDs and allocate new sequences above existing values. Accept sequence gaps, and do not claim sequence values represent global commit order across unrelated accounts. Do not automatically retry money requests or ambiguous commits.

**Rationale**: PostgreSQL sequence advancement is not rolled back. Per-account row locking provides ordered new account history without requiring gaplessness.

**Alternatives considered**: `MAX(sequence)+1` without coordination races. Request idempotency is useful beyond this workshop, but the approved specification explicitly excludes it; automatic replay without idempotency would be incorrect.

**Sources**:
- `https://www.postgresql.org/docs/17/functions-sequence.html`
- `https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency`

## 6. Repeatable seed import

**Decision**: A Development-only import command validates all four CSVs, then inserts missing rows in dependency order and stores versioned SHA-256 fingerprints of canonical original source values in the same transaction. Compare repeats to original fingerprints, not live balances. Existing matching fingerprints are skipped; changed fingerprints or untracked UUID collisions reject the whole import.

**Rationale**: This design preserves legitimate balance changes while detecting modified baseline input. Preserve UUIDs, timestamps, text, history, and resulting balances, and never call deposit/withdraw/transfer services for imported history.

**Alternatives considered**: EF `HasData` manages migration-owned data and is inappropriate for changing balances. Upsert-overwrite and replaying history would corrupt workshop activity. Plain conflict-ignore cannot distinguish modified source data.

**Source**: `https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding`

## 7. Minimal React interface

**Decision**: React with Vite and JSX, native fetch, component state, and CSS. Use a white background, Azure-inspired blue accents, English labels, explicit THB values, and no customer or Microsoft logos. Vite proxies `/api` to the backend and binds explicitly to `127.0.0.1:5173` with strict port selection.

**Rationale**: React documents Vite as a build-tool option for an app built from scratch; this small client does not require a full-stack React framework. Vite supports explicit host/port and proxy configuration. Use Node.js 22.12+ as the workshop prerequisite.

**Alternatives considered**: SSR, a router, a design-system dependency, and global state tooling are not required for one dashboard. Wildcard listeners and unrestricted hosts conflict with private-only operation.

**Sources**:
- `https://react.dev/learn/build-a-react-app-from-scratch`
- `https://vite.dev/guide/`
- `https://vite.dev/config/server-options`

## 8. Local PostgreSQL lifecycle

**Decision**: Compose runs PostgreSQL 17 on loopback with a health check and persistent named volume mounted at `/var/lib/postgresql/data`. Host processes run the API/UI. A separate opt-in test service has a distinct database/port and temporary storage. Use local environment variables for database credentials.

**Rationale**: Docker documents loopback publishing and named-volume persistence. Pinning PostgreSQL 17 avoids accidental major-image upgrades; its volume target differs from newer image layouts.

**Alternatives considered**: A preinstalled server was offered but the user chose Compose. Compose initialization SQL alone runs only on a fresh data directory and does not provide the required repeated import behavior.

**Sources**:
- `https://hub.docker.com/_/postgres`
- `https://docs.docker.com/guides/postgresql/`
- `https://docs.docker.com/compose/how-tos/startup-order/`

## Resolution

All planning clarifications are resolved. Future exact patch locking is an implementation dependency-resolution step, not an unresolved feature decision. The constitution is an unfilled template; feature constraints and the user's confirmed choices supply the applicable design gates.
