---
description: "Executable implementation tasks for the fictional local banking API and React UI"
---

# Tasks: Fictional Local Banking API and UI

**Input**: Design documents in `specs\001-local-banking-api\`.
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts\rest-api.md`, `contracts\ui.md`, and `quickstart.md`.
**Created**: 2026-11-17
**Branch**: `workshop/dotnet`

**Tests**: Included because the specification explicitly requires acceptance checks and the approved plan/quickstart requires automated service, PostgreSQL, REST, and UI verification. Add focused tests before their corresponding behavior, confirm the expected failure, implement, then run them. Do not treat scaffold failures as evidence of a behavioral regression test.

**Organization**: Setup, shared foundations, then one phase per user story in specification order. All P2 stories retain the same priority; their sequencing below reflects actual prerequisites and shared-file constraints.

## Format: `[ID] [P?] [Story] Description`

- Every executable task starts with `- [ ] Tnnn`; story tasks also have `[US1]` through `[US4]`.
- `[P]` means separate-file work that may run concurrently **after its listed prerequisites are complete**, not permission to bypass those prerequisites.
- All filesystem paths are repository-relative Windows paths. REST routes retain URL syntax.
- Inline quoted constraints reproduce the approved data model; implement the remaining field definitions from that document without inventing validation.
- This file schedules implementation only. No application code, package installation, database change, or deployment is performed by task generation.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the approved .NET/React layout and reproducible private development infrastructure without moving or modifying the seed files.

- [ ] T001 Create `BankingWorkshop.slnx`, `global.json`, `.config\dotnet-tools.json`, `src\backend\Banking.Api\Banking.Api.csproj`, and `src\backend\Banking.Api.Tests\Banking.Api.Tests.csproj`; target `net10.0`, scaffold controllers rather than minimal banking endpoints, reference compatible pinned EF Core/Design/tools 10.x, Npgsql provider 10.x, Microsoft.AspNetCore.OpenApi 10.x, standalone SwaggerUI, CsvHelper, xUnit, and WebApplicationFactory; generate backend `packages.lock.json` files and remove scaffold sample endpoints without adding authentication.
- [ ] T002 [P] Scaffold React JSX/Vite in `src\frontend\package.json`, `src\frontend\package-lock.json`, `src\frontend\index.html`, `src\frontend\src\main.jsx`, `src\frontend\eslint.config.js`, and `src\frontend\src\test\setup.js`; pin compatible dependencies and configure `dev`, `build`, `preview`, `lint`, `test`, and `test:e2e` scripts with Vitest, React Testing Library, and Playwright, preserving the existing README (depends on T001).
- [ ] T003 [P] Create `compose.yaml` and `.env.example` with PostgreSQL 17 pinned to a tested image digest, required environment-supplied password, `db` user/database `banking`, loopback `127.0.0.1:5432:5432`, health check, and named volume at `/var/lib/postgresql/data`; add opt-in `test-db` profile `test` with user/database `banking_test`, loopback port 5433, and temporary storage; include no actual credentials or volume-deletion/reset instructions (depends on T001).
- [ ] T004 [P] Configure `.editorconfig` and `.gitignore` for .NET/JS formatting and generated output, excluding local secrets/environment values while keeping `.env.example`, seed CSVs, migrations, and dependency lockfiles tracked; integrate existing ignore rules rather than replacing them (depends on T001).
- [ ] T005 Configure `src\backend\Banking.Api\appsettings.json`, `src\backend\Banking.Api\Properties\launchSettings.json`, `src\frontend\vite.config.js`, and `src\frontend\playwright.config.js` for backend loopback 8080, frontend loopback 5173 with strict ports, same-origin `/api` proxy, loopback preview, serial mutation E2E tests, and configured user-secrets support; do not enable wildcard hosts, wildcard CORS, or public forwarding (depends on T002).
- [ ] T006 Restore tools/dependencies and confirm the scaffold builds through `BankingWorkshop.slnx` and `src\frontend\package.json`; verify locked .NET restore, npm clean install, and the declared lint/test commands resolve, updating only scaffold configuration/lockfiles as necessary (depends on T001-T005).

**Checkpoint**: Reproducible project skeleton and private PostgreSQL definitions exist; no banking behavior is assumed yet.

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared entities, relational constraints, contracts, host configuration, transaction coordination, and isolated test infrastructure. Complete this phase before implementing story behavior.

- [ ] T007 [P] Create `src\backend\Banking.Api\Data\Entities\Customer.cs` with all customer source fields: `id` is "Required, unique, preserved verbatim as an identity."; `name` is "Required; preserve fictional Thai text."; `created_at` is "Required; preserve original instant at microsecond precision."; `english_name`, `email`, and `phone` must "Preserve supplied profile values."; address fields/postcode must "Preserve text, including postcode formatting."; `date_of_birth` must "Preserve supplied date, without timezone conversion."; occupation/national ID are "Fictional text only; preserve nonnumeric demo identifiers."; preserve optional empty profile text without imposing real-world identifier rules (depends on T006).
- [ ] T008 [P] Create `src\backend\Banking.Api\Data\Entities\Account.cs`: `id` must "Preserve imported UUID."; `customer_id` is "Required, indexed; deletion restricted."; currency is "Required, exactly `THB` for this baseline and new operations."; balance is "Required, finite, 0 through 9999999999999999.99."; creation time must "Preserve original instant."; map money to `decimal`, not floating point (depends on T006).
- [ ] T009 [P] Create `src\backend\Banking.Api\Data\Entities\Transfer.cs`: `id` has "Imported identity preserved; new identity generated once per completed operation."; source is "Required, indexed."; destination is "Required, indexed; distinct from source."; amount is "Positive, finite, within supported range."; currency is "THB; same as both linked accounts."; completion time has "Imported instant preserved; new value set on completion."; retain no pending/rejected transfer state (depends on T006).
- [ ] T010 [P] Create `src\backend\Banking.Api\Data\Entities\TransactionEntry.cs`: `id` has "Imported identity preserved; new identity generated for each new entry."; sequence has "Positive imported order preserved. New values from `transaction_entry_sequence`."; account reference is "Required."; type is "`DEPOSIT`, `WITHDRAWAL`, `TRANSFER_OUT`, or `TRANSFER_IN`."; amount is "Positive and finite; direction comes from type, not a negative amount."; currency is "Same currency as account and linked transfer, if any."; completion time must "Preserve historical instant."; resulting balance is "Nonnegative and finite; historical value, not an instruction to update current balance."; both transfer/counterparty references are "Required for transfer types; empty for deposit/withdrawal." (depends on T006).
- [ ] T011 [P] Create `src\backend\Banking.Api\Data\Entities\SeedRecord.cs`: entity type is "One of the four entity/table names."; record ID is "UUID from the source record."; canonical version is "Initially 1; defines the exact fingerprint normalization."; fingerprint is "SHA-256 hex of all canonical original source fields."; import time is "Time the original source row was first accepted."; use "Composite primary key `(entity_type, record_id)`." and keep metadata internal to import (depends on T006).
- [ ] T012 Implement `src\backend\Banking.Api\Data\BankingDbContext.cs` with all five entity mappings, UUID keys, `numeric(18,2)` decimal money, UTC `timestamptz` microsecond precision, `DateOnly` dates, text-preserving profile columns, restricted foreign-key deletion, required/optional constraints from T007-T011, unique positive bigint sequence, `(account_id, sequence)` index, filtered unique `(transfer_id, type)` index, finite bounded balance/amount checks, and transfer-type reference checks; cross-table two-sided transfer consistency remains service/import validation (depends on T007-T011).
- [ ] T013 Generate and inspect the initial migration and snapshot in `src\backend\Banking.Api\Data\Migrations\`; include `transaction_entry_sequence`, all constraints/indexes, and no migration-owned seed balances or `HasData`; ensure generated SQL uses PostgreSQL types and can apply in an empty isolated schema (depends on T012).
- [ ] T014 [P] Implement exact monetary parsing/formatting and errors in `src\backend\Banking.Api\Contracts\MoneyValue.cs`, `src\backend\Banking.Api\Contracts\BankingProblem.cs`, and `src\backend\Banking.Api\Services\BankingExceptionHandler.cs`; enforce the REST decimal-string regex/range, reject numeric JSON tokens/excess precision before persistence, format two decimals invariantly, and define sanitized ProblemDetails codes/statuses including field/model-binding failures (depends on T006).
- [ ] T015 [P] Implement shared locking/sequence primitives in `src\backend\Banking.Api\Data\BankingTransactionCoordinator.cs`: one transaction-scoped shared import-coordination advisory lock for each money writer, exclusive counterpart for import, parameterized account `FOR UPDATE` locks in PostgreSQL UUID order, and sequence allocation after account locks; expose a non-HTTP test seam for precommit failure injection and never automatically replay an ambiguous commit (depends on T012, T014).
- [ ] T016 [P] Create `src\backend\Banking.Api.Tests\Integration\PostgresFixture.cs` and `src\backend\Banking.Api.Tests\Integration\BankingApiFactory.cs`; require `BANKING_TEST_CONNECTION` targeting `banking_test`, fail on a missing/development connection, apply migrations in fixture-owned unique schemas, configure schema-specific tables/migration history/sequences and API DbContext, serialize shared-schema writers as needed, and limit cleanup to the fixture's own resolved schema (depends on T003, T013).
- [ ] T017 [P] Define read DTOs and shared paging in `src\backend\Banking.Api\Contracts\ReadDtos.cs` and `src\backend\Banking.Api\Contracts\PageResult.cs`; map every contracted customer/account/balance/transfer/entry field, monetary values and bigint sequences as strings, explicit null optional references, UTC timestamps, ISO birth dates, and pagination `page >= 1`, default size 50, maximum 100 (depends on T014).
- [ ] T018 Wire `src\backend\Banking.Api\Program.cs` for controllers, Npgsql DbContext, transaction coordinator, ProblemDetails/model-binding error handling, sanitized DB readiness at `/health`, and Development-only built-in OpenAPI plus separate SwaggerUI pointing at `/openapi/v1.json`; configure monetary schemas accurately and no `AddSwaggerGen`, `UseSwagger`, automatic migrations/import/reset, authentication, public host, or broad CORS (depends on T005, T012, T014, T015, T017).

**Checkpoint**: Database mappings/migrations, exact wire types, private host, Development-only documentation, and real-PostgreSQL test infrastructure are ready.

## Phase 3: User Story 1 - Explore the Fictional Banking Baseline (Priority: P1) - MVP

**Goal**: Import the complete fictional baseline safely and browse customers, accounts, balances, transfers, and full histories through REST and the blue/white UI.

**Independent Test**: Import into an empty isolated schema, compare all fields and relationships with the four CSVs, and browse all read routes/pages and the UI. Counts must be 102 customers, 150 accounts, 101 transfers, and 1,002 entries; balances must equal imported balances without replay.

### Tests for User Story 1

- [ ] T019 [P] [US1] Add read-contract tests in `src\backend\Banking.Api.Tests\Integration\ReadApiTests.cs` for every GET route in `contracts\rest-api.md`, complete pagination/order/empty children, money/sequence strings, malformed UUID 400 versus unknown UUID 404, unavailable readiness 503, read-only customer/account behavior, and OpenAPI/SwaggerUI present only in Development (depends on Phase 2).
- [ ] T020 [P] [US1] Add baseline tests in `src\backend\Banking.Api.Tests\Import\BaselineImportTests.cs` comparing all four CSVs against stored fields, UUIDs, relationships, timestamps, profile text, balances and sequences; assert exact fixture counts, original histories unchanged, no history replay, and a second identical import inserts nothing (depends on Phase 2).
- [ ] T021 [P] [US1] Add browsing tests in `src\frontend\src\test\Browsing.test.jsx` for paginated customer/account selection, exact THB/string-sequence display, complete history access, original Thai text, empty/loading/read failures, stale-response suppression, and the absence of login or maintenance controls (depends on Phase 2).

### Implementation for User Story 1

- [ ] T022 [US1] Implement `src\backend\Banking.Api\Import\SeedCsvReader.cs` and `src\backend\Banking.Api\Import\SeedCanonicalizer.cs` using CsvHelper UTF-8 parsing for all four required files; validate required headers/values, UUID uniqueness, positive unique sequences, money precision/range, types, populated references and matching transfer sides; canonicalize fixed-order fields with invariant decimals, lowercase UUIDs, microsecond UTC instants, ISO dates and explicit empty/null values, preserving original text without trimming (depends on T020).
- [ ] T023 [US1] Implement `src\backend\Banking.Api\Import\SeedImporter.cs` with full validation before writes, exclusive transaction advisory lock, dependency-ordered inserts and original-source fingerprints in one commit; skip matching accepted rows without changing live balances, reject modified/missing accepted source rows or untracked UUID collisions atomically, allow only a complete valid extension, and advance history sequence without lowering it or replaying operations (depends on T022, T015).
- [ ] T024 [US1] Add Development-only `--import-seed --seed-path <absolute-path>` dispatch and importer dependency registration in `src\backend\Banking.Api\Program.cs`; invoke the importer and exit before opening HTTP, return 0 for success/no-op and nonzero for failure, and report inserted/skipped counts or sanitized file/row/UUID diagnostics without credentials/profile dumps (depends on T023, T018).
- [ ] T025 [US1] Create read methods in `src\backend\Banking.Api\Services\BankingService.cs` using untracked queries and DTO projection for customers, customer accounts, all accounts, account/balance, transaction entry/history, transfer detail/list, exact ordering and pagination; distinguish missing parent from an existing parent with no children and register the scoped banking service in `src\backend\Banking.Api\Program.cs` (depends on T019, T017, T024).
- [ ] T026 [P] [US1] Implement GET actions in `src\backend\Banking.Api\Controllers\CustomersController.cs`, `src\backend\Banking.Api\Controllers\AccountsController.cs`, `src\backend\Banking.Api\Controllers\TransactionsController.cs`, and `src\backend\Banking.Api\Controllers\TransfersController.cs`, delegating reads to the banking service and preserving contracted routes/DTOs/400 versus 404 semantics without UUID route constraints that mask malformed input (depends on T025).
- [ ] T027 [P] [US1] Implement read fetch/pagination and sanitized error handling in `src\frontend\src\api\bankingClient.js`, plus exact string/BigInt money and sequence formatting in `src\frontend\src\api\money.js`; use only `/api` routes, support aborting/ignoring stale reads, preserve nulls/text, and never convert money to binary floating point (depends on T021, T017).
- [ ] T028 [US1] Build `src\frontend\src\App.jsx`, `src\frontend\src\components\CustomerBrowser.jsx`, `AccountBrowser.jsx`, `AccountDetails.jsx`, `HistoryTable.jsx`, and `src\frontend\src\styles.css`; implement contract browsing states, reachable history pagination, full UUIDs, English labels, explicit THB, responsive Azure-inspired blue/white surfaces, fictional-data notice, keyboard labels/focus and status announcements without logos/login/maintenance (depends on T027).
- [ ] T029 [US1] Run and resolve the focused US1 tests in `src\backend\Banking.Api.Tests\Integration\ReadApiTests.cs`, `src\backend\Banking.Api.Tests\Import\BaselineImportTests.cs`, and `src\frontend\src\test\Browsing.test.jsx`; demonstrate initial/repeat import CLI and a live read-only REST/UI browse using `specs\001-local-banking-api\quickstart.md`, retaining the supplied seed files unchanged (depends on T019-T028).

**Checkpoint**: US1 is a usable local MVP. Import is already atomic, conflict-aware, and non-destructive; US4 does not license a temporary unsafe importer.

## Phase 4: User Story 2 - Deposit and Withdraw Fictional Funds (Priority: P2)

**Goal**: Add exact single-account money operations with atomic history updates and clear UI outcomes.

**Independent Test**: Snapshot an account and complete history, deposit/withdraw valid amounts and check exact deltas plus one entry each; rejected/excessive withdrawals and invalid amounts leave all state unchanged.

### Tests for User Story 2

- [ ] T030 [P] [US2] Add service/integration tests in `src\backend\Banking.Api.Tests\Services\DepositWithdrawalTests.cs` for exact deposit/withdrawal deltas, exactly one correctly typed entry, exact-balance spend, insufficient funds, decimal/range/NaN rejection, credit overflow, concurrent competing withdrawals, and failure-before-commit rollback using actual PostgreSQL (depends on T029).
- [ ] T031 [P] [US2] Add POST contract tests in `src\backend\Banking.Api.Tests\Integration\MoneyApiTests.cs` for request strings/currency, 201 MoneyOperation and entry Location, invalid/numeric-body precision cases, missing/malformed accounts, and stable ProblemDetails codes; compare full pre/post state on rejection (depends on T029).
- [ ] T032 [P] [US2] Add component tests in `src\frontend\src\test\MoneyForms.test.jsx` for labeled decimal input, zero/negative/three-decimal validation, one request while pending, exact display, success/rejection refresh, completed-operation retention after refresh failure, and ambiguous network outcome without automatic retry (depends on T029).

### Implementation for User Story 2

- [ ] T033 [US2] Add deposit/withdrawal request and MoneyOperation result DTOs in `src\backend\Banking.Api\Contracts\MoneyOperationDtos.cs` using the shared exact amount parser and required THB currency, with contracted transaction/balance result fields and no exposed tracked entities (depends on T030, T031, T014).
- [ ] T034 [US2] Extend `src\backend\Banking.Api\Services\BankingService.cs` with deposit/withdrawal operations: take shared advisory lock and account row lock inside one transaction, validate after locking, enforce supported balance limits, allocate sequence, change balance and append one completed entry, commit once, and leave no partial writes on failure without automatic request replay (depends on T033, T015).
- [ ] T035 [US2] Add POST deposit/withdrawal actions to `src\backend\Banking.Api\Controllers\AccountsController.cs`, mapping banking-service outcomes to exact 201/400/404/409 responses and entry Location headers; update generated OpenAPI request/result metadata (depends on T034).
- [ ] T036 [US2] Extend `src\frontend\src\api\bankingClient.js` and `src\frontend\src\api\money.js` with deposit/withdrawal requests, exact string amount validation and stable problem outcomes; do not retry mutations or optimistically calculate persisted balances (depends on T032, T033).
- [ ] T037 [US2] Implement `src\frontend\src\components\MoneyForms.jsx` and integrate it in `src\frontend\src\App.jsx` with selected-account targeting, globally coordinated pending mutation state, outcome announcements, authoritative balance/history refresh and distinct mutation-versus-refresh failures (depends on T036).
- [ ] T038 [US2] Add single-account browser scenarios in `src\frontend\e2e\money.spec.js` against the real REST API and run US2 service/contract/component/E2E checks, comparing complete original history and exact persisted results for success and rejection without resetting development data (depends on T030-T037).

**Checkpoint**: Deposits and withdrawals work without transfers and preserve the baseline browsing flow.

## Phase 5: User Story 3 - Transfer Between Existing Accounts (Priority: P2)

**Goal**: Add atomic two-account transfers, both-account UI refresh, and assertion-based REST verification.

**Independent Test**: Use distinct funded THB UUID accounts, capture both balances and complete histories, transfer 100.00 successfully, capture fresh snapshots, then reject source-current-balance plus 0.01 and confirm both sides and transfer records are unchanged.

### Tests for User Story 3

- [ ] T039 [P] [US3] Add real-PostgreSQL transfer tests in `src\backend\Banking.Api.Tests\Services\TransferTests.cs` for conserved combined balance, exactly one transfer/two linked reciprocal entries, unchanged prior fields, exact-balance spend, insufficient funds, same/unknown account, mismatched currency, destination overflow, concurrent competing debits and opposite transfers, and precommit rollback of both balances and histories (depends on T029).
- [ ] T040 [P] [US3] Add transfer contract tests in `src\backend\Banking.Api.Tests\Integration\TransferApiTests.cs` for 201 TransferOperation/Location and all rejection codes; use source `00209a31-c483-41bb-a9fd-35530f8f680b` and destination `fc99d3e9-f19d-439e-87e4-c7a54b585c7d` with baseline fixture expectations and independent fresh before/after snapshots for success and rejection, fetching all history/transfer pages (depends on T029).
- [ ] T041 [P] [US3] Add UI transfer tests in `src\frontend\src\test\TransferForm.test.jsx` for distinct source/destination selection across customers, pending submission prevention, exact amounts, shared transfer ID, both-account cache invalidation/refresh, rejection non-mutation display, and unknown network outcome without automatic retry (depends on T029).

### Implementation for User Story 3

- [ ] T042 [US3] Define transfer request and TransferOperation result DTOs in `src\backend\Banking.Api\Contracts\TransferDtos.cs` with both UUIDs, required THB and amount string, completed Transfer, two Balance DTOs, and ordered outgoing/incoming entries (depends on T039, T040).
- [ ] T043 [US3] Extend `src\backend\Banking.Api\Services\BankingService.cs` with atomic transfer using shared advisory lock and PostgreSQL-ordered account locks, current-funds/currency/range validation after locking, one shared transfer UUID/completion instant and two new sequence values, reciprocal history links/resulting balances, and one commit with no replay on ambiguous completion (depends on T042, T015; serialize with T034 because both edit the same service).
- [ ] T044 [US3] Add POST transfer action to `src\backend\Banking.Api\Controllers\TransfersController.cs`, delegating all money rules to the service and returning contracted statuses, TransferOperation body, and `/api/transfers/{id}` Location while retaining GET behavior and OpenAPI metadata (depends on T043).
- [ ] T045 [US3] Extend `src\frontend\src\api\bankingClient.js` with transfer submission and complete affected-account refresh support, consuming money/sequence strings and stable problem codes without retrying mutations (depends on T041, T042; serialize with T036 because both edit the same client).
- [ ] T046 [US3] Implement `src\frontend\src\components\TransferForm.jsx` and `src\frontend\src\components\TransferResult.jsx`, integrating `src\frontend\src\App.jsx` for explicit distinct source/destination selection, all-account destination paging, globally pending mutation controls, shared ID/result display and access to both refreshed complete histories (depends on T045; serialize with T037 because both edit App).
- [ ] T047 [US3] Create `scripts\verify-banking.ps1` with contracted BaseUrl/SourceAccountId/DestinationAccountId/Amount parameters, UUID/distinct/same-currency/funds prechecks, all-page exact snapshots, one successful transfer and one excessive transfer using fresh snapshots, assertions for both balances/all prior history fields/new linked entries/transfer records, and nonzero exit on failure; use actual current balances, not assumed baseline values, and do not reset data (depends on T044).
- [ ] T048 [US3] Add `src\frontend\e2e\transfers.spec.js` with real REST snapshots around successful and rejected UI submissions; run US3 tests and `scripts\verify-banking.ps1`, checking baseline 52,608.86/47,043.92 and 4/6 entries only in fresh fixtures, while live checks use actual state and avoid parallel writers (depends on T039-T047).

**Checkpoint**: Transfers preserve combined funds, append both sides together, and prove exact non-mutation on rejection; US3 does not require the deposit/withdrawal feature.

## Phase 6: User Story 4 - Repeat Initialization Without Losing Workshop Activity (Priority: P2)

**Goal**: Prove and complete repeat import behavior under later activity, changed input, concurrency, CLI failures, and restarts.

**Independent Test**: Import twice, perform new money operations, reimport unchanged files, and compare every stored record. Invalid/conflicting imports change nothing; normal restart retains committed state.

### Tests for User Story 4

- [ ] T049 [P] [US4] Add `src\backend\Banking.Api.Tests\Import\RepeatImportTests.cs` for identical repeats before and after deposits, withdrawals and transfers, comparing every current account balance, complete entry/transfer records, original metadata and counts; assert zero duplicates, resets, replay or false source conflicts from legitimate balance changes (test authoring depends on T029; full execution depends on T038, T048).
- [ ] T050 [P] [US4] Add `src\backend\Banking.Api.Tests\Import\ImportConflictTests.cs` covering changed accepted fields, missing accepted records, untracked UUID collisions, missing files/columns, invalid decimals/currency/UUIDs/types, duplicate UUIDs/sequences, unresolved optional populated references, mismatched transfer sides and valid baseline extensions; use fixture copies and assert entire entity/metadata state unchanged on rejection (depends on T029).
- [ ] T051 [P] [US4] Add `src\backend\Banking.Api.Tests\Import\SeedCanonicalizerTests.cs` for canonical version 1, equivalent quoting/UTC instant representations, exact decimal/date/UUID normalization, Thai text preservation, empty/null distinctions, and changed original account-balance fingerprint despite legitimate differing live balance; verify every original source field contributes to the fingerprint (depends on T029).

### Implementation and Verification for User Story 4

- [ ] T052 [US4] Complete any gaps exposed by T049-T051 in `src\backend\Banking.Api\Import\SeedImporter.cs`, `SeedCsvReader.cs`, and `SeedCanonicalizer.cs`: compare the full accepted source set, reject unknown collisions or missing accepted rows, retain original fingerprints and live changes, accept only complete valid extensions, and roll back all invalid/conflicting imports; preserve the safe no-op behavior already delivered in US1 (depends on T049-T051).
- [ ] T053 [US4] Add and satisfy `src\backend\Banking.Api.Tests\Import\ImportConcurrencyTests.cs` for overlapping imports and import-versus-money writers, verifying shared/exclusive advisory coordination, no duplicate imports, unique subsequent sequences above all stored values, no lowered sequence, allowed rollback gaps and unchanged live activity; repair only demonstrated coordination gaps in `src\backend\Banking.Api\Data\BankingTransactionCoordinator.cs` or `Import\SeedImporter.cs` (depends on T052, T038, T048).
- [ ] T054 [US4] Add and satisfy subprocess/dispatch checks in `src\backend\Banking.Api.Tests\Import\ImportCommandTests.cs` for Development-only import, absolute seed path handling, zero/no-op success counts, nonzero conflict/input failure, sanitized record diagnostics, and no HTTP listener; correct `src\backend\Banking.Api\Program.cs` only as needed (depends on T052, T024).
- [ ] T055 [US4] Add restart-persistence integration checks in `src\backend\Banking.Api.Tests\Integration\PersistenceTests.cs` by disposing/recreating API hosts against the same isolated schema after committed operations; assert unchanged balances/history and repeat-import no-op, with no automatic migration/import/reset on normal startup (depends on T052, T038, T048).
- [ ] T056 [US4] Run `src\backend\Banking.Api.Tests\Import\` and `src\backend\Banking.Api.Tests\Integration\PersistenceTests.cs` plus the before/after activity sequence in `specs\001-local-banking-api\quickstart.md`; resolve remaining US4 failures without modifying tracked seed files or clearing the development database (depends on T053-T055).

**Checkpoint**: Repeated initialization preserves subsequent activity and committed data survives host restarts; conflicting input never partially applies.

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Complete cross-story documentation and whole-project verification without adding scope.

- [ ] T057 Update `src\README.md`, `src\backend\README.md`, `src\frontend\README.md`, and `specs\001-local-banking-api\quickstart.md` to match the implemented solution, pinned prerequisites, exact build/test/import/start commands, blue/white UI, database credential configuration, private forwarding, actual source layout, repeatability and non-destructive shutdown; remove obsolete application-placeholder statements only where implementation now exists (depends on T029, T038, T048, T056).
- [ ] T058 Run locked restore/build/test/format for `BankingWorkshop.slnx` and clean install/build/lint/component tests declared in `src\frontend\package.json` using the isolated test database; correct only implementation-related failures and keep exact SDK/package/image lock information consistent (depends on T057).
- [ ] T059 Run the full serial Playwright suite configured by `src\frontend\playwright.config.js` and the assertion runner `scripts\verify-banking.ps1` against private live services; verify complete both-account snapshots for transfer success/rejection, all history pages, pending controls, stale reads, refresh failures and exact THB presentation without resetting development data (depends on T058).
- [ ] T060 Verify host/documentation/readiness behavior against `src\backend\Banking.Api\Program.cs`, `Properties\launchSettings.json`, `src\frontend\vite.config.js`, `compose.yaml`, and `contracts\rest-api.md`: backend 8080 and all other listeners remain loopback, forwarded ports are Private, SwaggerUI/document return 404 outside Development, errors reveal no secrets, and no login/public-deployment/MCP/customer-maintenance features were introduced (depends on T058).
- [ ] T061 Execute the real API/PostgreSQL stop/restart and repeated-import sequence in `specs\001-local-banking-api\quickstart.md` after completed money operations, compare complete before/after snapshots, wait for readiness, and preserve the development named volume and all committed data (depends on T059, T056).
- [ ] T062 Reconcile implemented behavior and evidence against every FR/SC in `specs\001-local-banking-api\spec.md`, `plan.md`, `data-model.md`, `contracts\rest-api.md`, and `contracts\ui.md`; confirm `src\data\seed\customers.csv`, `accounts.csv`, `transfers.csv`, and `transaction_entries.csv` have no changes, no actual credentials are tracked, no required check is silently skipped, and stop only task-owned verification processes (depends on T060, T061).

## Dependencies & Execution Order

### Phase Dependencies

```text
Setup T001-T006
  -> Foundations T007-T018
     -> US1 T019-T029 (MVP)
        -> US2 T030-T038 --------\
        -> US3 T039-T048 --------+-> US4 completion T049-T056
        -> US4 isolated test ---/         |
           authoring T049-T051            v
                                   Polish T057-T062
```

### User Story Dependencies

| Story | Required prerequisites | Independence and execution notes |
|---|---|---|
| US1 | Setup and all foundations | Independently demonstrable seed import/read-only MVP. |
| US2 | US1 shared reads/import/UI | No transfer dependency; use an existing seeded account for testing. |
| US3 | US1 shared reads/import/UI | No deposit/withdrawal behavior dependency; use funded seed accounts. Serialize shared-file edits with US2. |
| US4 | US1 for isolated import checks; US2 and US3 for full later-activity acceptance | Baseline safeguards already exist in US1. Full acceptance adds all new operation types and restart/concurrency checks. |

Within each story, test tasks precede their associated implementation. Run the independent checkpoint after the required service, controller, client and UI changes are integrated. An implementation task may legitimately span several files when those edits form one coherent vertical change.

### Concrete Parallel Opportunities

- After T001: T002, T003 and T004 have disjoint files.
- After T006: T007-T011 and T014 have disjoint files; T017 can follow T014 independently of entity work.
- After their explicit prerequisites: T015 transaction infrastructure and T016 database fixture can proceed independently.
- Within each story: test tasks operate on separate files and can be authored in parallel. Do not run mutation E2E/REST verification concurrently against the same development accounts.
- US1: T026 backend controllers and T027 frontend client can proceed together after their separate prerequisites.
- US2/US3: backend test authoring and frontend component-test authoring can overlap. Do not concurrently edit `BankingService.cs`, `AccountsController.cs`, `TransfersController.cs`, `bankingClient.js`, or `App.jsx` from separate tasks.
- T060 configuration/privacy verification can run independently of T059 browser/REST checks once T058 passes, provided it does not change or restart their live services. Use isolated hosts for non-Development route checks.

## Parallel Examples by User Story

### User Story 1

After foundations, author T019 `ReadApiTests.cs`, T020 `BaselineImportTests.cs`, and T021 `Browsing.test.jsx` in parallel. Later run T026 controller work alongside T027 client work. T028 waits for the client and T029 waits for the whole slice.

### User Story 2

After US1, author T030 `DepositWithdrawalTests.cs`, T031 `MoneyApiTests.cs`, and T032 `MoneyForms.test.jsx` in parallel. After contracts are ready, frontend T036/T037 can proceed alongside backend T034/T035 because their file sets are disjoint.

### User Story 3

After US1, author T039 `TransferTests.cs`, T040 `TransferApiTests.cs`, and T041 `TransferForm.test.jsx` in parallel. Backend T043/T044 and frontend T045/T046 can proceed separately after DTOs are defined, while respecting the US2 shared-file barriers. T047 can be developed once backend transfer behavior exists.

### User Story 4

Author T049 `RepeatImportTests.cs`, T050 `ImportConflictTests.cs`, and T051 `SeedCanonicalizerTests.cs` in parallel. After T052, T054 command checks and T055 host-restart checks can proceed independently; serialize any fixes touching the same importer/Program/coordinator files and run each check against its own fixture schema.

## Requirement Coverage

| Requirements / outcomes | Principal task coverage |
|---|---|
| FR-001, FR-002; read-only scope and no login | T018-T021, T025-T029, T060 |
| FR-003, FR-004; SC-001; exact baseline/no replay | T007-T013, T020, T022-T024, T029 |
| FR-005, FR-006; SC-002; atomic validation/reimport | T011, T015, T022-T024, T049-T054, T056 |
| FR-007, FR-008; single-account SC-003 | T030-T038 |
| FR-009; two-account SC-003/SC-004 | T039-T048, T059 |
| FR-010, FR-011; exact money/concurrency/rollback | T012-T015, T030-T035, T039-T044, T053 |
| FR-012; stable complete history | T010, T012, T017, T019-T020, T025-T029, T039-T040, T047 |
| FR-013; SC-005; client-neutral functional UI | T021, T027-T028, T032, T036-T038, T041, T045-T046, T048, T059 |
| FR-014; clear errors and rejection non-mutation | T014, T019, T030-T041, T044, T047-T048 |
| FR-015; SC-006; private/fictional/no extra scope | T001-T005, T018, T028, T057, T060, T062 |
| FR-016; SC-007; restart persistence | T003, T055-T056, T061 |

## Implementation Strategy

### MVP First

Complete T001-T029. Demonstrate private import plus customer/account/balance/history browsing through REST and the themed UI. Validate that repeat initialization is already a safe no-op. Do not add money forms backed by mock data or an unsafe reset-based importer.

### Incremental Delivery

1. Establish the scaffold and real-database foundations.
2. Deliver US1 as the read-only, faithfully imported local MVP.
3. Add US2, validating single-account changes and rejection independently.
4. Add US3, validating both sides of transfers independently of US2.
5. Complete US4 activity/concurrency/conflict/restart evidence without duplicating or weakening the US1 importer.
6. Run cross-story checks and reconcile documentation with the actual application.

### Notes

- Total: **62 tasks**: setup 6, foundations 12, US1 11, US2 9, US3 10, US4 8, polish 6.
- The constitution is an unfilled template; do not invent ratified requirements or edit it as part of implementation.
- No extra architecture layers, authentication, public deployment, MCP, real financial data, or customer branding.
- Do not reset or delete development data to satisfy a test. Test fixture cleanup targets only a fixture-owned isolated schema.
- Task completion requires implemented behavior and the relevant checkpoint evidence, not merely creation of the named files.
