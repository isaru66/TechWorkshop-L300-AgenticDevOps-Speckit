# Tasks: Banking MCP Server

**Input**: Design documents from `/specs/002-banking-mcp-server/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/mcp-tools.md, contracts/rest-additions.yaml, quickstart.md

**Tests**: Included (plan.md specifies `McpToolsIT` and `AccountLifecycleIT`; existing `*IT` convention with Testcontainers PostgreSQL via `PostgresIntegrationTest`).

**Organization**: Grouped by user story. All paths are relative to the repository root; backend root is `src/backend/` (Java package root `src/backend/src/main/java/com/workshop/banking/`, abbreviated `{main}`; tests `src/backend/src/test/java/com/workshop/banking/`, abbreviated `{test}`).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 = look up (P1), US2 = manage customers/accounts (P2), US3 = move money (P3)

---

## Phase 1: Setup

- [X] T001 Add `spring-ai-bom` 2.0.2 to `<dependencyManagement>` and the `org.springframework.ai:spring-ai-starter-mcp-server-webmvc` dependency to `src/backend/pom.xml`; run `mvn -f src/backend/pom.xml -q compile` and, if 2.0.2 is incompatible with Spring Boot 4.1.2, use the nearest compatible 2.0.x and record the final version in `specs/002-banking-mcp-server/research.md` (R1)
- [X] T002 [P] Configure MCP in `src/backend/src/main/resources/application.yml`: `spring.ai.mcp.server.protocol: STATELESS`, Streamable HTTP endpoint `/mcp`, server name `local-banking-mcp`, version, and instructions text; keep `server.address: 127.0.0.1` and port 8080

---

## Phase 2: Foundational (blocks all user stories)

- [X] T003 Create Flyway migration `src/backend/src/main/resources/db/migration/V2__account_status_and_label.sql`: add `account.status text NOT NULL DEFAULT 'OPEN' CHECK (status IN ('OPEN','CLOSED'))` and `account.label text NULL` (max 100 chars enforced via `CHECK (char_length(label) <= 100)`)
- [X] T004 Add `status` (OPEN|CLOSED, default OPEN) and `label` fields with getters/setters to `{main}domain/Account.java`; add an `AccountStatus` enum in `{main}domain/AccountStatus.java`
- [X] T005 Expose `status` and `label` additively in the account response DTO in `{main}api/dto/ApiDtos.java` (existing fields and shapes unchanged)
- [X] T006 [P] Add error factories to `{main}service/BankingException.java`: `accountClosed()` -> `ACCOUNT_CLOSED`, `nonZeroBalance()` -> `NON_ZERO_BALANCE`, `customerHasAccounts()` -> `CUSTOMER_HAS_ACCOUNTS`; map the new codes to HTTP 409 in `{main}api/GlobalExceptionHandler.java`
- [X] T007 Create `{main}mcp/McpErrorMapper.java` converting `BankingException` to a tool error result (`isError=true`, text `"<CODE>: <message>"`) and unexpected exceptions to a generic error without stack traces
- [X] T008 Create the tool class skeleton `{main}mcp/BankingMcpTools.java` (`@Component`, constructor-injects only `BankingService`, no repositories) with a shared helper that wraps each call with `McpErrorMapper` and serializes results to JSON text using existing DTO shapes
- [X] T009 [P] Create `{test}mcp/McpToolsIT.java` scaffold extending `{test}support/PostgresIntegrationTest.java` with helpers to POST JSON-RPC to `/mcp` with headers `Content-Type: application/json` and `Accept: application/json, text/event-stream` and parse JSON or SSE `data:` framing; add a test that `initialize` returns `serverInfo` and `capabilities.tools`
- [X] T010 Extend `{test}service/ControllerLayeringTest.java` so classes in `{main}mcp` may not depend on `repository` or `domain` repositories (tools delegate to `BankingService` only)
- [X] T011 [P] Create `src/backend/test.http` header section: `@baseUrl = http://127.0.0.1:8080`, reusable headers comment, and the `initialize` request (`protocolVersion`, `capabilities`, `clientInfo`) plus `notifications/initialized` and `tools/list` requests with `# Expected:` comments (16 tools); no session header (stateless)

**Checkpoint**: App starts, `V2` applies, `/mcp` answers `initialize` and `tools/list` (empty/partial list), existing tests still pass (`mvn -f src/backend/pom.xml verify`).

---

## Phase 3: User Story 1 - Look up customers, accounts, and history (Priority: P1) MVP

**Goal**: Assistants can list/search/view customers, list accounts, view account and balance, and page through history.

**Independent Test**: Via `test.http` or `McpToolsIT`, call the read tools against seeded data and compare with REST responses; unknown ids return `NOT_FOUND` errors.

### Tests for US1

- [X] T012 [P] [US1] In `{test}mcp/McpToolsIT.java` add tests: `tools/list` contains read tools with descriptions; `list_customers`, `search_customers` (match on name/English name/email/phone, case-insensitive), `get_customer`, `list_customer_accounts`, `get_account`, `get_account_balance`, `get_transaction_history` (newest first, `size` max 100) return data equal to the corresponding REST responses; unknown id -> `isError` with `NOT_FOUND`; `size=101` -> `VALIDATION_ERROR`

### Implementation for US1

- [X] T013 [US1] Add `searchCustomers(String query, int page, int size)` to `{main}service/BankingService.java` (case-insensitive match on name, englishName, email, phone; reuse `pageRequest` bounds, page size max 100; blank query -> `VALIDATION_ERROR`) and a matching query method in `{main}repository/CustomerRepository.java`
- [X] T014 [US1] Implement tools `list_customers`, `search_customers`, `get_customer`, `list_customer_accounts`, `get_account`, `get_account_balance`, `get_transaction_history` in `{main}mcp/BankingMcpTools.java` with `@McpTool` descriptions and `@McpToolParam` descriptions per `contracts/mcp-tools.md` (ids as UUID strings, `page` default 0, `size` default 20)
- [X] T015 [US1] Add US1 requests to `src/backend/test.http`: `search_customers` named request (`# @name searchCustomers`) capturing a seeded customer id via `{{searchCustomers.response.body...}}` (or a documented variable), then `list_customer_accounts`, `get_account_balance`, `get_transaction_history`, plus a `get_customer` unknown-id error case, each with `# Expected:` comment and sample (fictional, client-neutral) data

**Checkpoint**: US1 independently demonstrable (MVP).

---

## Phase 4: User Story 2 - Manage customers and accounts (Priority: P2)

**Goal**: Create/update/delete customers; open/update/close accounts; closed accounts reject money movement; REST parity for the new operations.

**Independent Test**: Create customer -> open account -> update both -> close account -> delete a customer with no accounts; refusal cases return the documented error codes; REST shows the same state.

### Tests for US2

- [X] T016 [P] [US2] Create `{test}service/AccountLifecycleIT.java` (extends `PostgresIntegrationTest`): update customer changes only descriptive fields; blank name -> `VALIDATION_ERROR`; close with non-zero balance -> `NON_ZERO_BALANCE`; close zero-balance -> status `CLOSED`, history still readable; close twice -> `ACCOUNT_CLOSED`; delete customer with open or closed account -> `CUSTOMER_HAS_ACCOUNTS`; delete customer with no accounts succeeds then `NOT_FOUND`; deposit/withdraw/transfer touching a closed account -> `ACCOUNT_CLOSED` with balances unchanged
- [X] T017 [P] [US2] In `{test}mcp/McpToolsIT.java` add tests for `create_customer`, `update_customer`, `delete_customer`, `open_account`, `update_account`, `close_account` success and error paths (error text begins with the code)
- [X] T018 [P] [US2] In `{test}api/BrowseAndCreateApiIT.java` (or a new `{test}api/LifecycleApiIT.java`) add REST tests for `PUT/DELETE /api/customers/{id}`, `PATCH /api/accounts/{id}`, `POST /api/accounts/{id}/close` (200/204/404/409) and that existing endpoints still behave as before

### Implementation for US2

- [X] T019 [US2] In `{main}service/BankingService.java` add `updateCustomer(UUID, CustomerInput)` (name required non-blank; email format validated; `id`/`createdAt` immutable; same validation as create), `deleteCustomer(UUID)` (refuse with `customerHasAccounts()` if any account exists, including CLOSED), `updateAccount(UUID, String label)` (label max 100 chars, `VALIDATION_ERROR` otherwise), `closeAccount(UUID)` (lock row; refuse non-zero balance with `nonZeroBalance()`; already closed -> `accountClosed()`; set status CLOSED); extend `openAccount(UUID, String label)` keeping the existing one-arg behavior; add repository support (`existsByCustomerId` or equivalent) in `{main}repository/AccountRepository.java`
- [X] T020 [US2] In `{main}service/BankingService.java` reject `deposit`, `withdraw`, and `transfer` when any involved account is CLOSED (`accountClosed()`), checked after locking and before changing balances; `getHistory` and reads remain allowed for closed accounts
- [X] T021 [P] [US2] Add REST endpoints per `contracts/rest-additions.yaml` in `{main}api/CustomerController.java` (`PUT /api/customers/{customerId}`, `DELETE /api/customers/{customerId}` -> 204) and `{main}api/AccountController.java` (`PATCH /api/accounts/{accountId}`, `POST /api/accounts/{accountId}/close`); controllers only delegate; update `{main}api/OpenApiConfig.java` only if needed
- [X] T022 [US2] Implement tools `create_customer`, `update_customer`, `delete_customer`, `open_account`, `update_account`, `close_account` in `{main}mcp/BankingMcpTools.java` per `contracts/mcp-tools.md`
- [X] T023 [US2] Add US2 requests to `src/backend/test.http`: create customer (capture id), open two accounts (capture ids), update customer, update account label, close account with non-zero balance error, delete customer with accounts error, delete accounts-free customer success; each with `# Expected:` comment; add REST section requests for the new endpoints

**Checkpoint**: US1 + US2 work independently; REST and MCP agree.

---

## Phase 5: User Story 3 - Move money (Priority: P3)

**Goal**: Deposit, withdraw, and transfer through MCP with existing rules.

**Independent Test**: Deposit 1000.00, withdraw 200.00, transfer 300.00 between two accounts; balances 500.00/300.00 and history shows entries; rule violations are refused without changing balances.

### Tests for US3

- [X] T024 [P] [US3] In `{test}mcp/McpToolsIT.java` add tests for `deposit`, `withdraw`, `transfer_funds` success (balances and history via `get_transaction_history`, also visible via REST) and errors: `INSUFFICIENT_FUNDS`, zero/negative/3-decimal amount -> `VALIDATION_ERROR`, same account -> `SAME_ACCOUNT`, unknown id -> `NOT_FOUND`, closed account -> `ACCOUNT_CLOSED`; assert no balance change after each refusal

### Implementation for US3

- [X] T025 [US3] Implement tools `deposit`, `withdraw`, `transfer_funds` in `{main}mcp/BankingMcpTools.java` calling `BankingService.deposit/withdraw/transfer` (amount as decimal string, passed through unchanged so existing `parseAmount` validation applies)
- [X] T026 [US3] Add US3 requests to `src/backend/test.http`: deposit 1000.00, withdraw 200.00, transfer_funds 300.00, `get_transaction_history` (expect newest first: transfer out, withdrawal, deposit), error cases (overdraw, negative amount, same-account transfer, closed account), and the closing sequence (withdraw all, `close_account`) with `# Expected:` comments; add a REST `GET /api/accounts/{{accountId}}/transactions` request to show MCP changes are visible through REST

**Checkpoint**: All three stories complete; all 16 tools implemented.

---

## Phase 6: Polish & Cross-Cutting

- [X] T027 [P] Verify `tools/list` returns exactly the 16 tools in `contracts/mcp-tools.md`, each with description and parameter descriptions; add the assertion to `{test}mcp/McpToolsIT.java`
- [X] T028 [P] Add MCP usage docs (endpoint, no-auth warning, stateless transport, tool list, `.vscode/mcp.json` example, `test.http` usage) to `src/backend/README.md` if it exists, otherwise to the existing backend/workshop doc location; keep client-neutral
- [X] T029 Update `specs/002-banking-mcp-server/quickstart.md` if tool names, headers, or expected outputs changed during implementation
- [X] T030 Run `mvn -f src/backend/pom.xml verify` (Docker required) and confirm all existing 001 tests plus new tests pass
- [X] T031 Start the app and execute every request in `src/backend/test.http` in order; confirm each matches its `# Expected:` comment and fix the file or code where it does not

---

## Dependencies & Execution Order

- Phase 1 -> Phase 2 -> user stories -> Phase 6.
- US1 depends only on Phase 2. US2 depends on Phase 2 (independent of US1 except sharing `BankingMcpTools.java`). US3 depends on Phase 2 and on T020 (closed-account guard) for its `ACCOUNT_CLOSED` test; its tools are otherwise independent.
- Tasks editing the same file (`BankingMcpTools.java`, `BankingService.java`, `test.http`, `McpToolsIT.java`) are sequential, not [P].
- Within a story: tests -> service -> controllers/tools -> `test.http`.

## Parallel Opportunities

- Setup: T001 || T002 after T001's dependency resolves (different files; T002 is independent).
- Foundational: T006, T009, T011 in parallel; T003 -> T004 -> T005 sequential.
- US2: T016, T017, T018 (tests) in parallel; T021 (REST) in parallel with T022 (tools) once T019/T020 are done.
- Polish: T027, T028 in parallel.

## Implementation Strategy

- **MVP**: Phases 1-3 (read-only MCP over existing data, plus `test.http` reads).
- Then add US2 (lifecycle + REST parity), then US3 (money movement).
- Validate after each phase with `mvn verify` and the matching `test.http` section.


## Phase 7: Convergence

- [X] T032 Escape `%`, `_` and `\` in the search text before building the LIKE pattern in `searchCustomers` in `src/backend/src/main/java/com/workshop/banking/service/BankingService.java` (and add an `escape` clause to the query in `src/backend/src/main/java/com/workshop/banking/repository/CustomerRepository.java`), and add a test in `src/backend/src/test/java/com/workshop/banking/mcp/McpToolsIT.java` proving that a query of `%` or `_` does not match every customer, per FR-003 (partial)
- [X] T033 [P] Document in `specs/002-banking-mcp-server/contracts/rest-additions.yaml` the optional `{ "label": string }` request body now accepted by `POST /api/customers/{customerId}/accounts`, and the additive `status`/`label` fields on account responses, per plan: REST additions (partial)
- [X] T034 [P] Document in `specs/002-banking-mcp-server/contracts/mcp-tools.md` that object results also include `structuredContent` (null fields omitted; list results such as `list_customer_accounts` return text content only) and that the text content is the full JSON, per plan: MCP tool contract (unrequested)
- [X] T035 Add edge-case tests in `src/backend/src/test/java/com/workshop/banking/mcp/McpToolsIT.java` for the spec edge cases "simultaneous operations never produce an incorrect balance" (concurrent `deposit`/`withdraw`/`close_account` on one account ends with a consistent balance and status) and "banking data cannot be reached" (an unexpected service failure yields `isError: true` with text starting `INTERNAL_ERROR:` and no stack trace), per spec Edge Cases (partial)