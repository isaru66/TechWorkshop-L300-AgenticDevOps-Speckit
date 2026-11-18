# Tasks: MCP Banking Server

**Input**: Design documents from `/specs/002-mcp-banking-server/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/mcp-tools.md, contracts/test.http, quickstart.md

**Tests**: Included (plan specifies xUnit tests in the existing `Banking.Api.Tests` project).

**Organization**: Grouped by user story. Paths are relative to the repo root; API project = `src/backend/Banking.Api`, tests = `src/backend/Banking.Api.Tests`.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

- [ ] T001 Add `<PackageReference Include="ModelContextProtocol.AspNetCore" Version="2.4.0" />` to `src/backend/Banking.Api/Banking.Api.csproj` and run `dotnet restore src/backend/Banking.Api` to regenerate `src/backend/Banking.Api/packages.lock.json` (and the tests project lock file if it changes)
- [ ] T002 [P] Create folders `src/backend/Banking.Api/Mcp/` and `src/backend/Banking.Api.Tests/Mcp/`

---

## Phase 2: Foundational (blocks all stories)

- [ ] T003 Add `BankingProblem.HasDependents(string detail)` returning `new(409, "has_dependents", "Has dependents", detail)` in `src/backend/Banking.Api/Contracts/BankingProblem.cs`
- [ ] T004 Create `src/backend/Banking.Api/Mcp/McpErrors.cs`: helper that runs a tool delegate, catches `BankingProblem` and rethrows `McpException` with message `"<code>: <detail>"` (so the SDK returns `isError: true`), and maps any other exception to `"unexpected_error: An unexpected error occurred."` without internals (research R6)
- [ ] T005 Register MCP in `src/backend/Banking.Api/Program.cs`: `builder.Services.AddMcpServer().WithHttpTransport(o => o.Stateless = true).WithTools<CustomerTools>().WithTools<AccountTools>().WithTools<TransferTools>().WithTools<HistoryTools>()` and `app.MapMcp("/mcp")` before `app.Run()`; leave all existing registrations, `MapControllers`, and `/health` untouched (depends on T006–T008 types existing; create empty `[McpServerToolType]` classes first if sequencing requires)
- [ ] T006 [P] Create empty `[McpServerToolType]` class `CustomerTools` (constructor takes `BankingService`) in `src/backend/Banking.Api/Mcp/CustomerTools.cs`
- [ ] T007 [P] Create empty `[McpServerToolType]` classes `AccountTools` in `src/backend/Banking.Api/Mcp/AccountTools.cs` and `TransferTools` in `src/backend/Banking.Api/Mcp/TransferTools.cs`
- [ ] T008 [P] Create empty `[McpServerToolType]` class `HistoryTools` in `src/backend/Banking.Api/Mcp/HistoryTools.cs`
- [ ] T009 Create MCP test helper `src/backend/Banking.Api.Tests/Mcp/McpTestClient.cs` that uses `BankingApiFactory`/`PostgresFixture` to POST JSON-RPC to `/mcp` with headers `Content-Type: application/json` and `Accept: application/json, text/event-stream`, parses either JSON or SSE `data:` payloads, and exposes `CallToolAsync(name, args)` returning `result` JSON

**Checkpoint**: `dotnet build src/backend` succeeds; `/mcp` responds to `initialize` and `tools/list`.

---

## Phase 3: User Story 1 - Manage customers and accounts (P1) 🎯 MVP

**Goal**: Assistants can create/list/get/update/delete customers and create/list/get/delete accounts.

**Independent Test**: Via `/mcp`, create a customer, open an account, get/list them, update the customer, delete the account then the customer; unknown IDs return `not_found`.

### Tests for User Story 1

- [ ] T010 [P] [US1] Test `initialize` returns 200 and `tools/list` returns all 13 tool names from `contracts/mcp-tools.md` in `src/backend/Banking.Api.Tests/Mcp/McpToolListTests.cs`
- [ ] T011 [P] [US1] Customer/account tool tests in `src/backend/Banking.Api.Tests/Mcp/CustomerAccountToolTests.cs`: create→get→list→update→delete customer; create account starts at `0.00`; blank name → `invalid_input`; unknown ID → `not_found`; delete customer with accounts → `has_dependents`; delete account with nonzero balance or history → `has_dependents`

### Implementation for User Story 1

- [ ] T012 [P] [US1] Add `CreateCustomerRequest` and `UpdateCustomerRequest` records (fields per `data-model.md`: `Name` required non-blank; optional `EnglishName`, `Email`, `Phone`, `AddressLine`, `Subdistrict`, `District`, `Province`, `Postcode`, `DateOfBirth`, `Occupation`, `NationalId`) in `src/backend/Banking.Api/Contracts/CustomerWriteDtos.cs`
- [ ] T013 [US1] In `src/backend/Banking.Api/Services/BankingService.cs` add `CreateCustomerAsync` (server-generated `Id`/`CreatedAt`; `Name` must be non-blank else `BankingProblem.InvalidInput`), `UpdateCustomerAsync` (partial: only supplied fields change; `Name` cannot be blanked; not found → `BankingProblem.NotFound()`), `DeleteCustomerAsync` ("allowed only if no accounts exist" else `BankingProblem.HasDependents`); return `CustomerDto`
- [ ] T014 [US1] In `BankingService.cs` add `CreateAccountAsync(Guid customerId)` (customer must exist else `NotFound`; `Currency = "THB"`, `Balance = 0.00`, `CreatedAt` set) and `DeleteAccountAsync(Guid id)` ("allowed only if balance is `0.00` and no transaction entries/transfers reference it" else `HasDependents`); return `AccountDto`
- [ ] T015 [US1] Implement tools in `src/backend/Banking.Api/Mcp/CustomerTools.cs`: `list_customers`, `get_customer`, `create_customer`, `update_customer`, `delete_customer` (`[McpServerTool(Name = ...)]` with `[Description]` on tool and every parameter; wrap calls with `McpErrors`; delete returns `{ deleted: true }`; defaults `page=1`, `pageSize=50`)
- [ ] T016 [US1] Implement tools in `src/backend/Banking.Api/Mcp/AccountTools.cs`: `list_accounts` (optional `customerId` → `CustomerAccountsAsync`, else `AccountsAsync`), `get_account`, `create_account`, `delete_account`

**Checkpoint**: US1 tests pass; MVP demoable.

---

## Phase 4: User Story 2 - Transfer funds (P2)

**Goal**: Assistants can fund accounts and transfer money with existing validation.

**Independent Test**: Deposit 1000.00 into A, transfer 250.00 A→B; balances 750.00/250.00; invalid transfers change nothing.

### Tests for User Story 2

- [ ] T017 [P] [US2] Tests in `src/backend/Banking.Api.Tests/Mcp/TransferToolTests.cs`: successful transfer (balances and two entries `TRANSFER_OUT`/`TRANSFER_IN`); `insufficient_funds`; `invalid_amount` (zero/negative/3 decimals); `same_account`; unknown account `not_found`; balances unchanged after each rejection; deposit/withdraw happy path and `insufficient_funds` on withdraw

### Implementation for User Story 2

- [ ] T018 [US2] Implement `transfer_funds` (`sourceAccountId`, `destinationAccountId`, `amount` string; currency fixed `"THB"`) calling `BankingService.TransferAsync` in `src/backend/Banking.Api/Mcp/TransferTools.cs`
- [ ] T019 [US2] Implement `deposit` and `withdraw` (`accountId`, `amount` string) calling `BankingService.ChangeBalanceAsync(id, amount, "THB", deposit, ct)` in `src/backend/Banking.Api/Mcp/AccountTools.cs`

**Checkpoint**: US1 + US2 work independently.

---

## Phase 5: User Story 3 - Transaction history (P3)

**Goal**: Assistants can view account history newest first.

**Independent Test**: After a deposit and transfer, history for the source returns `TRANSFER_OUT` then `DEPOSIT`.

### Tests for User Story 3

- [ ] T020 [P] [US3] Tests in `src/backend/Banking.Api.Tests/Mcp/HistoryToolTests.cs`: newest-first ordering with date/type/amount/counterparty; empty history returns empty items; unknown account `not_found`; paging via `page`/`pageSize`

### Implementation for User Story 3

- [ ] T021 [US3] Implement `get_transaction_history` (`accountId`, `page=1`, `pageSize=50`) calling `BankingService.HistoryAsync` in `src/backend/Banking.Api/Mcp/HistoryTools.cs`

**Checkpoint**: All stories functional.

---

## Phase 6: Polish & Cross-Cutting

- [ ] T022 [P] Add `src/backend/Banking.Api/test.http` from `specs/002-mcp-banking-server/contracts/test.http`; run the API, execute it, confirm actual response format (JSON vs SSE) and correct the header comments and `# Expect` notes
- [ ] T023 [P] REST regression test in `src/backend/Banking.Api.Tests/Mcp/RestUnchangedTests.cs`: `/health` and `/api/customers`, `/api/accounts`, `/api/transfers` still return expected status codes and shapes; run the full existing suite
- [ ] T024 [P] Update README/docs (find the existing banking API doc, e.g. under `docs/`) with MCP endpoint, tool list, `.vscode/mcp.json` snippet, and "demo only, no authentication, loopback only" warning
- [ ] T025 Run `dotnet test src/backend/Banking.Api.Tests` and walk through `specs/002-mcp-banking-server/quickstart.md`; verify SC-005 (typical call < 2 s)

---

## Dependencies & Execution Order

- Phase 1 → Phase 2 → stories → Polish.
- T006–T008 before T005; T009 before all MCP tests.
- US1 first (MVP). US2 needs accounts to exist (use US1 tools or fixtures); US3 needs US2 for meaningful history (or fixtures via `ChangeBalanceAsync`). `AccountTools.cs` is shared by T016 and T019 — do sequentially.
- Tests within each story are written first and should fail before implementation.

## Parallel Examples

- Setup/Foundational: T006, T007, T008 together.
- US1: T010, T011, T012 together; then T013→T014; T015 and T016 in parallel after T014.
- Polish: T022, T023, T024 together.

## Implementation Strategy

- **MVP**: Phases 1–3 (customers and accounts over `/mcp`), validate, then add transfers, then history.
- Commit after each task or group; keep REST routes and payloads untouched.
