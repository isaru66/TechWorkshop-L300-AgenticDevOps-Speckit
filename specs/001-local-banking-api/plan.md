# Implementation Plan: Fictional Local Banking API and UI

**Branch**: `workshop/dotnet` | **Date**: 2026-11-16 | **Spec**: [spec.md](spec.md)

**Input**: `specs\001-local-banking-api\spec.md` and the user's technical planning instructions.

**Feature resolution**: Setup returned logical feature identifier `001-local-banking-api`; the actual Git branch remains `workshop/dotnet`. Feature directory and branch are independent.

## Summary

Create a React client and an ASP.NET Core 10 controller-based REST API backed by local PostgreSQL. Read existing customers/accounts and support balances, deposits, withdrawals, transfers, and complete history. Import the four existing fictional CSVs without changing their identities, historical fields, or baseline balances. Preserve subsequent activity on repeated import.

Confirmed choices: Azure-inspired blue accents on a white background, client-neutral presentation without logos, English labels with THB formatting, and Docker Compose PostgreSQL with loopback-only ports and persistent storage. No login, public deployment, MCP integration, or customer/account maintenance.

## Technical Context

**Language/Version**: C# 14 / .NET SDK 10, backend and test projects targeting `net10.0`; React JavaScript with JSX, Node.js 22.12+.

**Primary Dependencies**: ASP.NET Core 10 MVC controllers; EF Core and Design/tools 10.x; Npgsql.EntityFrameworkCore.PostgreSQL 10.x; Microsoft.AspNetCore.OpenApi 10.x; standalone Swashbuckle.AspNetCore.SwaggerUI; React and Vite; CsvHelper for standards-compliant UTF-8 CSV parsing.

**Version policy**: Resolve compatible stable releases during implementation, pin exact package versions and the .NET SDK, commit npm and NuGet lockfiles and a local `dotnet-ef` tool manifest. Keep Microsoft EF components on a matching 10.x patch line and the Npgsql provider on major 10. SwaggerUI has its own version lifecycle and must not be forced to major 10 merely because the application uses .NET 10.

**Storage**: PostgreSQL 17 (`postgres:17`, pin tested image digest during implementation), named volume mounted at `/var/lib/postgresql/data`; C# `decimal` and PostgreSQL `numeric(18,2)` for money; UUID keys and UTC timestamps.

**Testing**: xUnit service and real-PostgreSQL integration tests with ASP.NET Core WebApplicationFactory; Vitest and React Testing Library for UI behavior; Playwright Chromium for REST/UI end-to-end checks; ESLint and `dotnet format`.

**Target Platform**: Private local Windows development with Docker Desktop Linux containers, PowerShell commands, and a modern browser.

**Project Type**: Local web application: one backend executable and one React client, without additional domain/repository projects.

**Performance Goals**: Correctness and repeatability for the supplied baseline take priority. No new latency SLA; reads are paginated (default 50, maximum 100). Concurrency tests exercise at least two competing debits and opposite-direction transfers.

**Constraints**: Backend `127.0.0.1:8080`; frontend `127.0.0.1:5173`; development PostgreSQL `127.0.0.1:5432`; optional isolated test PostgreSQL `127.0.0.1:5433`. Forwarded ports remain private. All mutations are atomic; all API monetary values are decimal strings, parsed into `decimal` server-side. No real data, authentication, public hosting, or MCP work.

**Scale/Scope**: Initial seed: 102 customers, 150 accounts, 101 transfers, 1,002 entries. Read-only customer/account views, one minimal dashboard, three money-operation forms, repeatable import CLI, and local verification.

## Constitution Check

*GATE: evaluated before Phase 0 and again after Phase 1.*

The constitution file exists but contains unfilled template placeholders, not ratified principles. No invented constitution requirements or constitution edits are introduced.

| Gate | Pre-research | Post-design evidence |
|---|---|---|
| Honor confirmed functional scope | PASS | Customer/account reads only; API and UI contracts exclude maintenance and login. |
| Preserve seed data and later activity | PASS | Separate original-seed fingerprints, all-or-nothing importer, no replay; data model and import checks define invariants. |
| Exact and atomic money operations | PASS | Decimal/numeric mapping, deterministic account locks, single transaction for balances and history; real-database tests required. |
| Private fictional workshop only | PASS | Loopback bindings, private forwarding, local-only documentation and no authentication/public deployment/MCP additions. |
| Minimal architecture | PASS | Controllers call a banking service using one DbContext; no generic repository, CQRS, message broker, or extra service. |
| Clarifications resolved | PASS | User selected theme, language, and Compose setup before planning. |

No gate violations require a complexity exception.

## Project Structure

### Documentation (this feature)

```text
specs\001-local-banking-api\
  spec.md
  checklists\requirements.md
  plan.md
  research.md
  data-model.md
  quickstart.md
  contracts\
    rest-api.md
    ui.md
```

`tasks.md` is produced by `/speckit-tasks`, not by this planning command.

### Source Code (repository root; to be generated during implementation)

```text
BankingWorkshop.slnx
global.json
compose.yaml
.env.example
.config\dotnet-tools.json
src\
  backend\
    Banking.Api\
      Banking.Api.csproj
      Program.cs
      Controllers\
      Contracts\
      Services\BankingService.cs
      Data\BankingDbContext.cs
      Data\Entities\
      Data\Migrations\
      Import\SeedImporter.cs
      Properties\launchSettings.json
      appsettings.json
    Banking.Api.Tests\
      Banking.Api.Tests.csproj
      Services\
      Integration\
      Import\
  frontend\
    package.json
    package-lock.json
    vite.config.js
    playwright.config.js
    src\
      App.jsx
      api\bankingClient.js
      components\
      styles.css
      test\
    e2e\
  data\seed\
    customers.csv
    accounts.csv
    transfers.csv
    transaction_entries.csv
scripts\
  verify-banking.ps1
```

**Structure Decision**: Populate the existing backend/frontend placeholder directories without moving or modifying seed files. Keep the existing README files, updating their startup references during implementation. Root solution includes the API and test project. Compose runs PostgreSQL only; backend/frontend run on the host.

## Phase 0: Research

Decisions, rationale, alternatives, and primary sources are recorded in [research.md](research.md). Research covers the requested framework/provider compatibility, built-in OpenAPI plus separate SwaggerUI, exact money representation, transaction locking, seed identity tracking, React tooling, and private Compose persistence.

## Phase 1: Design

### API and banking service

- Register controllers and a scoped `BankingService` using one scoped `BankingDbContext`. Controllers map DTOs and service outcomes; the service owns validation and money-operation transactions. Do not expose tracked EF entities directly.
- Use `AddOpenApi()` and Development-only `MapOpenApi()` to generate `/openapi/v1.json`. In the same Development condition, configure `UseSwaggerUI` with that document URL at `/swagger`. Do not add `AddSwaggerGen`, `UseSwagger`, or the Swashbuckle metapackage.
- Document monetary strings with a decimal pattern and examples. Parse amounts invariantly into `decimal`; reject excess fractional precision before PostgreSQL can round it. Format responses as two-decimal invariant strings.
- Use ProblemDetails with stable error codes. An uncertain network outcome must not trigger automatic resubmission; refresh state and show the ambiguity.

### Transaction and import boundaries

- Mutating operations take a shared transaction-scoped PostgreSQL advisory lock reserved for baseline coordination; import takes the exclusive version of that same lock. This prevents import/sequence coordination from racing normal mutations.
- Within each banking transaction, lock affected account rows in PostgreSQL UUID order using parameterized `SELECT ... ORDER BY id FOR UPDATE`; validate funds/currency/range after obtaining locks; update balances and append entries/transfer; save and commit once. Deposits follow the same lock discipline.
- Use a PostgreSQL sequence for unique new entry order and allocate values only after account locks. Import advances it beyond all stored sequences without lowering it. Gaps after rollback are allowed and are not history entries.
- Parse and validate the full baseline before changes; insert customers, accounts, transfers, entries and accepted-source fingerprints in one transaction. Reimport compares canonical incoming source values to stored original fingerprints, never to mutated current account balances. Reject conflicts and unknown collisions, preserve later activity, and never invoke banking operations to import history.
- Run migrations explicitly. Development-only `--import-seed --seed-path <absolute-path>` initializes/imports and exits without opening HTTP ports; normal startup does not import, migrate, reset, or recreate the database.

### Frontend and local environment

- Use React JSX, Vite, native fetch, and local component state; no routing/state-management/UI framework is necessary for this single dashboard.
- Use the Azure-inspired blue/white design in [contracts\ui.md](contracts/ui.md). Keep money as strings; validate with text/decimal rules and use integer minor units (`BigInt`) when arithmetic or formatting is needed, never binary floating-point money calculations.
- Proxy browser `/api` requests through Vite to the loopback backend. No broad CORS policy. Use strict ports and explicit loopback hosts for development and preview; any forwarded ports must be private.
- Compose `db` uses a health check and persistent named volume; an optional `test-db` profile uses a different database, port 5433, and temporary storage. Database credentials are local environment configuration, not application user authentication and not committed secrets.

### Validation and traceability

| Coverage | Required evidence |
|---|---|
| FR-001, FR-002, FR-013, FR-015 | Controller read contracts, blue/white UI, no maintenance/login, private bindings and Development-only docs. |
| FR-003 through FR-006 | Exact baseline field/relationship comparisons, repeat import before/after new operations, changed-source conflict and malformed-input rollback. |
| FR-007 through FR-012, FR-014 | Successful/rejected operations, exact-balance spend, monetary range/precision, both-account transfer snapshots, rollback fault injection, concurrent competing debits and opposite transfers. |
| FR-016 | Normal restart preserves all balances/history; repeating import remains a no-op. |

Real-PostgreSQL integration tests use only an explicitly configured isolated `banking_test` database, with a guard rejecting the development database. They create unique test schemas, apply migrations, and clean up only their own schema. No EF in-memory provider as a substitute for relational/locking verification.

[quickstart.md](quickstart.md) defines build, lint, test, startup, REST snapshots, import repetition, restart persistence, UI verification, and non-destructive shutdown instructions for this exact layout.

## Complexity Tracking

No constitutional violations or extra architecture layers. Original-source fingerprints and database locks are necessary to satisfy non-destructive reimport and concurrent atomic money operations.
