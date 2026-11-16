# Tasks: Local Banking API

**Input**: Design documents from `/specs/001-local-banking-api/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml, quickstart.md

**Tests**: Included, because the spec requires verifiable acceptance checks (import idempotency, before/after transfer comparisons, concurrency safety).

**Organization**: Grouped by user story (US numbers match spec.md). Base Java package: `com.workshop.banking`, under `src/backend/src/main/java/com/workshop/banking/` (abbreviated `{pkg}/` below). Test root: `src/backend/src/test/java/com/workshop/banking/` (abbreviated `{tpkg}/`).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story the task belongs to
- Rules that apply everywhere: money is `BigDecimal` / `numeric(19,2)`, never float; fictional data only; no authentication; database settings come only from environment variables `DB_URL`, `DB_USER`, `DB_PASSWORD` and are never written into committed files; services and ports stay private (loopback).
- Run builds with `$env:JAVA_HOME = "C:\Users\isarar\.jdks\ms-25.0.4.1"` set (see research.md R1).

---

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Create `src/backend/pom.xml`: Spring Boot parent 4.1.2, `maven.compiler.release=21`, dependencies for web, data-jpa, validation, actuator, flyway-core and flyway-database-postgresql, postgresql driver, springdoc-openapi (the release compatible with Spring Boot 4; verify), and test dependencies spring-boot-starter-test and Testcontainers (postgresql, junit-jupiter)
- [X] T002 [P] Create `src/backend/src/main/java/com/workshop/banking/BankingApplication.java` with the `@SpringBootApplication` main class
- [X] T003 [P] Create `src/backend/src/main/resources/application.yml`: `server.port=8080`, `server.address=127.0.0.1`, datasource from `${DB_URL}`, `${DB_USER}`, `${DB_PASSWORD}`, `spring.jpa.hibernate.ddl-auto=validate`, `spring.jpa.open-in-view=false`, springdoc paths `/v3/api-docs` and `/swagger-ui.html`, `banking.seed.path=../data/seed`
- [X] T004 [P] Scaffold `src/frontend` with Vite + React + TypeScript (`package.json`, `tsconfig.json`, `index.html`, `src/main.tsx`); add dependencies react-router-dom, @tanstack/react-query; dev dependencies vitest, @testing-library/react, jsdom
- [X] T005 [P] Create `src/frontend/vite.config.ts`: dev server host `localhost`, port 5173, proxy `/api` to `http://localhost:8080`, vitest config using jsdom

---

## Phase 2: Foundational (Blocking Prerequisites)

**CRITICAL**: No user story work starts until this phase is complete.

- [X] T006 Create Flyway migration `src/backend/src/main/resources/db/migration/V1__baseline_schema.sql` per data-model.md: tables `customer`, `account`, `transfer`, `transaction_entry`; all ids `uuid` PK; money columns `numeric(19,2)`; `account.currency` `char(3)` CHECK = 'THB'; `account.balance` CHECK >= 0; `transfer.amount` and `transaction_entry.amount` CHECK > 0; `transfer` CHECK `source_account_id <> destination_account_id`; `transaction_entry.type` restricted to DEPOSIT, WITHDRAWAL, TRANSFER_IN, TRANSFER_OUT; `transaction_entry.sequence` bigint UNIQUE; nullable `transfer_id` and `counterparty_account_id` with FKs; indexes `account(customer_id)`, `transaction_entry(account_id, completed_at DESC, sequence DESC)`, `transaction_entry(transfer_id)`; a database sequence `transaction_entry_seq`
- [X] T007 [P] Create JPA entity `{pkg}/domain/Customer.java` (fields per data-model.md: name required; english_name, email, phone, address_line, subdistrict, district, province, postcode, date_of_birth, occupation, national_id, created_at)
- [X] T008 [P] Create JPA entity `{pkg}/domain/Account.java` (customer_id, currency, balance `BigDecimal`, created_at)
- [X] T009 [P] Create JPA entity `{pkg}/domain/Transfer.java` (source_account_id, destination_account_id, amount, currency, completed_at)
- [X] T010 [P] Create JPA entity `{pkg}/domain/TransactionEntry.java` and enum `{pkg}/domain/EntryType.java` (DEPOSIT, WITHDRAWAL, TRANSFER_IN, TRANSFER_OUT); fields: sequence, account_id, type, amount, currency, completed_at, resulting_balance, transfer_id, counterparty_account_id
- [X] T011 Create Spring Data repositories in `{pkg}/repository/`: `CustomerRepository`, `AccountRepository` (include a `findByIdForUpdate` using `PESSIMISTIC_WRITE`, and `findByCustomerId`), `TransferRepository`, `TransactionEntryRepository` (paged query by account ordered by `completed_at DESC, sequence DESC`)
- [X] T012 [P] Create the error model and handler: `{pkg}/api/ApiError.java`, domain exceptions in `{pkg}/service/` (`NotFoundException`, `InsufficientFundsException`, `SameAccountException`, `CurrencyMismatchException`, `InvalidAmountException`), and `{pkg}/api/GlobalExceptionHandler.java` mapping to 400 `VALIDATION_ERROR`, 404 `NOT_FOUND`, 422 `INSUFFICIENT_FUNDS` / `SAME_ACCOUNT` / `CURRENCY_MISMATCH`, shaped as in contracts/openapi.yaml
- [X] T013 [P] Create shared DTOs matching contracts/openapi.yaml in `{pkg}/api/dto/`: `PageResponse<T>`, `AccountResponse`, `BalanceResponse`, `TransactionEntryResponse`; money serialized as a decimal string with 2 decimals
- [X] T014 [P] Create Testcontainers base class `{tpkg}/support/PostgresIntegrationTest.java` (PostgreSQL container, dynamic datasource properties, Flyway enabled) so tests never use the local database
- [X] T015 [P] Create the frontend theme `src/frontend/src/theme/theme.css` (Azure-style blue and white; `--color-primary: #0078D4`, hover `#106EBE`, text `#201F1E`, surface `#FFFFFF`, panel `#F3F2F1`, visible focus ring; no logos or customer branding) and app shell `src/frontend/src/App.tsx` with React Router routes and a nav bar; no login screen
- [X] T016 [P] Create the typed REST client `src/frontend/src/api/client.ts` (fetch wrapper for `/api`, parses `ApiError`) and `src/frontend/src/api/types.ts` mirroring contracts/openapi.yaml schemas (money as string)

**Checkpoint**: Schema, entities, errors, and the frontend shell exist.

---

## Phase 3: User Story 1 - Load the fictional baseline repeatably (Priority: P1) 🎯 MVP

**Goal**: Startup import of the four CSVs, insert-only, atomic, with no replay.

**Independent Test**: Start on an empty database and confirm counts 102/150/101/1002 and seed balances; restart and confirm nothing is added or changed.

### Tests for User Story 1

- [X] T017 [P] [US1] Create `{tpkg}/seed/SeedImporterIT.java`: first import yields counts 102/150/101/1002 and every account balance equals `accounts.csv`; a second import inserts 0 rows; a later deposit and its changed balance survive a re-import; imported balances are not recomputed from entries
- [X] T018 [P] [US1] Create `{tpkg}/seed/SeedValidationTest.java` using small malformed CSV fixtures under `src/backend/src/test/resources/seed-invalid/`: duplicate id, unresolved reference, non-THB currency, and bad amount are each rejected and leave the database empty

### Implementation for User Story 1

- [X] T019 [US1] Create `{pkg}/seed/SeedFiles.java` and `{pkg}/seed/SeedValidator.java`: parse the four CSVs from `banking.seed.path` (handle the `+00` timestamp format and empty `transfer_id` / `counterparty_account_id`), validate unique ids, that all populated references resolve, currency is THB, and amounts are positive 2-decimal `BigDecimal`
- [X] T020 [US1] Create `{pkg}/seed/SeedImporter.java` as an `ApplicationRunner`, in one `@Transactional` run: insert in order customers, accounts, transfers, entries using `INSERT ... ON CONFLICT (id) DO NOTHING` via `JdbcTemplate` batches; load balances and `resulting_balance` verbatim and never apply entries to balances; log inserted/skipped counts per file (no personal data in logs); fail without partial data on validation errors
- [X] T021 [US1] In `SeedImporter` (`{pkg}/seed/SeedImporter.java`), after the import advance `transaction_entry_seq` past `max(sequence)` using `setval`, and ensure new entries use `nextval`
- [X] T022 [US1] Add the seed-path property binding class `{pkg}/seed/SeedProperties.java` and make the importer skip with a clear error if a file is missing

**Checkpoint**: Backend starts, imports once, and is repeatable.

---

## Phase 4: User Story 2 - Browse customers, accounts and balances (Priority: P1)

**Goal**: Read APIs and UI pages for customers, accounts, and balances.

**Independent Test**: After import, fetch a customer (full record), list their accounts, and fetch a balance; compare with the seed files.

### Tests for User Story 2

- [X] T023 [P] [US2] Create `{tpkg}/api/BrowseApiIT.java`: `GET /api/customers/{id}` returns the full record including `nationalId`; `GET /api/customers/{id}/accounts` returns all linked accounts; `GET /api/accounts/{id}/balance` matches seed; unknown UUID returns 404 `NOT_FOUND`; paged lists honour `page` and `size` (size above 100 or below 1 is rejected with 400)
- [X] T024 [P] [US2] Create `src/frontend/src/test/CustomersPage.test.tsx` (mocked API): list renders, a detail view shows the full record and the account list with balances

### Implementation for User Story 2

- [X] T025 [P] [US2] Create `{pkg}/api/dto/CustomerResponse.java` (full record) and `{pkg}/api/dto/CustomerInput.java` per contracts/openapi.yaml
- [X] T026 [US2] Create `{pkg}/service/BankingService.java` with read operations: list/get customers, list customers' accounts, list/get accounts, get balance; throw `NotFoundException` for unknown ids; validate paging bounds
- [X] T027 [US2] Create `{pkg}/api/CustomerController.java` (`GET /api/customers`, `GET /api/customers/{customerId}`, `GET /api/customers/{customerId}/accounts`) and `{pkg}/api/AccountController.java` (`GET /api/accounts`, `GET /api/accounts/{accountId}`, `GET /api/accounts/{accountId}/balance`), delegating only to `BankingService`
- [X] T028 [US2] Configure springdoc in `{pkg}/config/OpenApiConfig.java` (title, tags Customers/Accounts/Transactions) so `/v3/api-docs` and `/swagger-ui.html` work locally
- [X] T029 [P] [US2] Create `src/frontend/src/pages/CustomersPage.tsx` and `src/frontend/src/pages/CustomerDetailPage.tsx` (full record, accounts with balances and links), using TanStack Query and the theme
- [X] T030 [P] [US2] Create `src/frontend/src/pages/AccountsPage.tsx` and `src/frontend/src/pages/AccountDetailPage.tsx` (balance panel; history area is filled in by US6)

**Checkpoint**: Seeded data can be browsed through REST, Swagger UI, and the UI.

---

## Phase 5: User Story 5 - Transfer between accounts (Priority: P1)

**Goal**: Atomic transfers with rejection rules, exercised by before/after comparisons.

**Independent Test**: Follow quickstart.md section 5: successful transfer and an insufficient-funds transfer, comparing both balances and both histories before and after.

### Tests for User Story 5

- [X] T031 [P] [US5] Create `{tpkg}/service/TransferServiceIT.java`: choose two distinct same-currency seeded accounts where the source has sufficient funds; snapshot balances and histories; a successful transfer changes source -amount, destination +amount, total unchanged, exactly one new `TRANSFER_OUT` and one `TRANSFER_IN` entry sharing a `transferId` and with each other as counterparty; then snapshot again and an insufficient-funds transfer returns 422 `INSUFFICIENT_FUNDS` with balances and histories identical
- [X] T032 [P] [US5] Create `{tpkg}/service/TransferRulesIT.java`: same-account returns 422 `SAME_ACCOUNT`; mismatched currency returns 422 `CURRENCY_MISMATCH` (insert a temporary non-THB case through a unit-level test of the service if the schema forbids it); unknown UUID returns 404; zero, negative, and 3-decimal amounts return 400; transferring the exact balance leaves 0.00; a failure injected after the first balance update rolls everything back
- [X] T033 [P] [US5] Create `{tpkg}/service/TransferConcurrencyIT.java`: concurrent opposing transfers between two accounts neither deadlock nor lose updates, and no balance goes negative
- [X] T034 [P] [US5] Create `src/frontend/src/test/TransferPage.test.tsx` (mocked API): success shows a confirmation; a 422 `INSUFFICIENT_FUNDS` shows a clear error and no balance changes

### Implementation for User Story 5

- [X] T035 [P] [US5] Create `{pkg}/api/dto/TransferRequest.java` (`sourceAccountId`, `destinationAccountId`, `amount` with at most 2 decimals, greater than zero) and `{pkg}/api/dto/TransferResponse.java`
- [X] T036 [US5] Add `transfer(...)` to `{pkg}/service/BankingService.java`: `@Transactional`; reject source == destination; lock both accounts with `findByIdForUpdate` in ascending UUID order; reject different currency; reject amount greater than source balance; update both balances; insert one `Transfer` plus a `TRANSFER_OUT` entry and a `TRANSFER_IN` entry with `resulting_balance`, `transfer_id`, `counterparty_account_id`, and `sequence` from `transaction_entry_seq`
- [X] T037 [US5] Create `{pkg}/api/TransferController.java` (`POST /api/transfers`, returns 201), delegating only to `BankingService`
- [X] T038 [US5] Create `src/frontend/src/pages/TransferPage.tsx`: account pickers (by UUID or from lists), amount input, submit; show the result, new balances, and API errors (insufficient funds, same account, unknown account)

**Checkpoint**: Transfers are atomic and verified by the before/after comparison.

---

## Phase 6: User Story 3 - Create customers and accounts (Priority: P2)

**Goal**: Create customers and open THB accounts.

**Independent Test**: Create a customer, open an account, read both back; the account has 0.00 THB and an empty history.

### Tests for User Story 3

- [X] T039 [P] [US3] Create `{tpkg}/api/CreateApiIT.java`: `POST /api/customers` returns 201 with a generated UUID; invalid or missing `name` and malformed `email` return 400 with field errors; `POST /api/customers/{id}/accounts` returns 201, THB, `0.00`; a non-existent customer returns 404 and creates nothing
- [X] T040 [P] [US3] Create `src/frontend/src/test/CreateForms.test.tsx` (mocked API): the create-customer form validates `name`, and the open-account action refreshes the account list

### Implementation for User Story 3

- [X] T041 [US3] Add `createCustomer(...)` and `openAccount(customerId)` to `{pkg}/service/BankingService.java` (validate `name` required, `email` format, `date_of_birth` in the past; new account is THB with balance 0.00); add Bean Validation annotations to `{pkg}/api/dto/CustomerInput.java`
- [X] T042 [US3] Add `POST /api/customers` and `POST /api/customers/{customerId}/accounts` to `{pkg}/api/CustomerController.java`
- [X] T043 [P] [US3] Create `src/frontend/src/pages/NewCustomerPage.tsx` (all customer fields, client-side validation) and an "Open account" button on `src/frontend/src/pages/CustomerDetailPage.tsx`

**Checkpoint**: New customers and accounts can be created from REST and UI.

---

## Phase 7: User Story 4 - Deposit and withdraw (Priority: P2)

**Goal**: Single-account money movements that record history.

**Independent Test**: Deposit then withdraw on one account; balance moves by the exact amounts and two entries appear.

### Tests for User Story 4

- [X] T044 [P] [US4] Create `{tpkg}/service/DepositWithdrawIT.java`: deposit raises the balance and adds a `DEPOSIT` entry with the correct `resulting_balance`; withdrawal lowers it and adds a `WITHDRAWAL` entry; insufficient funds returns 422 with balance and history unchanged; zero, negative, and 3-decimal amounts return 400; a withdrawal of the exact balance leaves 0.00; concurrent withdrawals never produce a negative balance
- [X] T045 [P] [US4] Create `src/frontend/src/test/AccountActions.test.tsx` (mocked API): the deposit and withdraw forms submit, update the balance, and surface errors

### Implementation for User Story 4

- [X] T046 [P] [US4] Create `{pkg}/api/dto/AmountRequest.java` (`amount`: greater than zero, at most 2 decimals, `@Digits(integer=17, fraction=2)`)
- [X] T047 [US4] Add `deposit(accountId, amount)` and `withdraw(accountId, amount)` to `{pkg}/service/BankingService.java`: `@Transactional`, lock the account row, apply, insert the entry with `resulting_balance` and a `sequence` from `transaction_entry_seq`; withdrawals raise `InsufficientFundsException`
- [X] T048 [US4] Add `POST /api/accounts/{accountId}/deposits` and `POST /api/accounts/{accountId}/withdrawals` (201) to `{pkg}/api/AccountController.java`
- [X] T049 [US4] Add deposit and withdraw forms to `src/frontend/src/pages/AccountDetailPage.tsx`, refreshing the balance and history afterwards

**Checkpoint**: Deposits and withdrawals work from REST and UI.

---

## Phase 8: User Story 6 - View transaction history (Priority: P2)

**Goal**: Paged, newest-first history per account.

**Independent Test**: Request successive pages for a seeded account and confirm order, completeness, and no overlap.

### Tests for User Story 6

- [X] T050 [P] [US6] Create `{tpkg}/api/HistoryApiIT.java`: newest first by `completed_at` then `sequence`; with `size=5`, successive pages contain every entry exactly once; an account with no history returns an empty page with 200; `size=0` or `size=101` returns 400; entries expose type, amount, currency, `completedAt`, `resultingBalance`, `transferId`, `counterpartyAccountId`
- [X] T051 [P] [US6] Create `src/frontend/src/test/HistoryTable.test.tsx` (mocked API): rows render newest first, paging controls work, and the empty state shows

### Implementation for User Story 6

- [X] T052 [US6] Add `getHistory(accountId, page, size)` to `{pkg}/service/BankingService.java` (404 for an unknown account; bounds 1-100 for `size`) and `GET /api/accounts/{accountId}/transactions` to `{pkg}/api/AccountController.java`
- [X] T053 [US6] Create `src/frontend/src/components/HistoryTable.tsx` with paging and use it in `src/frontend/src/pages/AccountDetailPage.tsx`

**Checkpoint**: All six stories work.

---

## Phase 9: Polish & Cross-Cutting Concerns

- [X] T054 [P] Create `{tpkg}/contract/OpenApiContractIT.java`: fetch `/v3/api-docs` and assert that every path and operation in `specs/001-local-banking-api/contracts/openapi.yaml` exists
- [X] T055 [P] Create `{tpkg}/service/NoCrossUseTest.java` architecture test: controllers must not contain banking rules and must not use repositories directly (they only call `BankingService`)
- [X] T056 [P] Verify privacy settings: add `{tpkg}/config/PrivacyConfigTest.java` asserting `server.address` is `127.0.0.1` and that no security or auth dependency is on the classpath; confirm the frontend dev server binds to localhost in `src/frontend/vite.config.ts`
- [X] T057 [P] Update `src/backend/README.md` and `src/frontend/README.md` with the build, test, and run commands from quickstart.md (no database values, credentials, or port numbers for the database)
- [X] T058 Run `mvn -B clean verify` in `src/backend` and `npm run build && npm test` in `src/frontend`; fix any failures
- [X] T059 Run the full quickstart.md validation: start the backend, confirm the import and a restart (no duplicates, no overwrite), run both transfer comparisons through REST, then complete the UI checks in quickstart.md section 7
- [X] T060 Review that no real or personal data, secrets, or database settings were added to committed files and that `.env` remains git-ignored

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: none
- **Foundational (Phase 2)**: after Setup; blocks all stories
- **US1 (Phase 3)**: after Foundational; blocks verification of the other stories, since they need imported data
- **US2, US5 (P1)**: after Foundational and US1 for seeded data; US5 reuses the read service from US2 (T026) for account lookups
- **US3, US4, US6 (P2)**: after Foundational; US4 and US5 share the `transaction_entry_seq` set up in T021
- **Polish**: after all desired stories

### Within Each Story

- Tests first (they should fail), then DTOs, service, controller, then UI
- `BankingService.java` and controller files are shared across stories, so tasks that edit them run sequentially, in phase order

### Parallel Opportunities

- Setup: T002 to T005
- Foundational: T007 to T010, T012 to T016
- Within a story, all `[P]` test tasks and the frontend page tasks run alongside backend work, since they touch different files
- After US1, US2's frontend (T029, T030) and US5's tests (T031 to T034) can proceed in parallel

### Parallel Example: User Story 5

```text
T031 TransferServiceIT, T032 TransferRulesIT, T033 TransferConcurrencyIT, T034 TransferPage.test.tsx
then T035 dto  ->  T036 service  ->  T037 controller  ->  T038 TransferPage.tsx
```

## Implementation Strategy

### MVP First

1. Setup, then Foundational
2. US1 (import) and validate repeatability
3. US2 (browse) and US5 (transfer) to prove the core banking behaviour end to end
4. Stop and run quickstart sections 4 and 5

### Incremental Delivery

Add US3 (create), US4 (deposit and withdraw), and US6 (history) in turn, validating each story's independent test before moving on, then finish with Polish.

## Notes

- Never write database connection values into files, docs, or logs
- Commit after each task or logical group
- Keep the UI and docs client-neutral

## Phase 10: Convergence

- [X] T061 Add `src/backend/src/test/java/com/workshop/banking/service/TransferRulesIT.java` covering the rules T032 still lacks: a `CURRENCY_MISMATCH` rejection (the schema CHECK forbids non-THB accounts, so use a service-level test with mocked repositories) and an atomic-rollback test that injects a failure after the first balance update and asserts both balances and both histories are unchanged per FR-010, FR-012 (partial)
- [X] T062 Add a balance-consistency assertion to `src/backend/src/test/java/com/workshop/banking/service/DepositWithdrawHistoryIT.java`: after the concurrent withdrawal and opposing-transfer tests, every touched account's balance equals its latest entry's `resulting_balance` per SC-005 (partial)
- [X] T063 Add an API test in `src/backend/src/test/java/com/workshop/banking/api/BrowseAndCreateApiIT.java` that `GET /api/accounts/{id}/transactions` for a newly opened account returns 200 with an empty `items` page and `totalElements` 0 per FR-013 (partial)
