# Research: MCP Banking Server

## R1: Transport and sessions
- **Decision**: Streamable HTTP at `/mcp`, stateless (`HttpServerTransportOptions.Stateless = true`). No `Mcp-Session-Id`; every POST is self-contained. stdio and legacy SSE are out of scope.
- **Rationale**: Simplest, works behind restarts/multiple instances, and tools need no per-session state. Clients still send `initialize` + `notifications/initialized`; the server answers `initialize` but doesn't require it before `tools/list` or `tools/call`.
- **Alternatives**: Stateful (needs session header handling, memory); stdio (needs separate process, not hosted in API); legacy SSE (deprecated).

## R2: SDK
- **Decision**: `ModelContextProtocol.AspNetCore` 2.4.0 (stable on nuget.org, .NET 8+). Tools declared with `[McpServerToolType]`/`[McpServerTool]` and DI-injected `BankingService`.
- **Rationale**: Official SDK; compatible with `net10.0`. Repo uses package lock files (`RestorePackagesWithLockFile`), so `packages.lock.json` must be regenerated.
- **Alternatives**: Hand-rolled JSON-RPC (more code, protocol drift).

## R3: Reuse of business logic
- **Decision**: Tools call `BankingService` (`TransferAsync`, `ChangeBalanceAsync`, `CustomersAsync`, `HistoryAsync`, …). `BankingService` is scoped; the SDK resolves tool-class constructor dependencies per request, so scoped DI works in stateless mode.
- **Gap found**: `BankingService` has **no** create/update/delete for customers or accounts, and REST has none either (only GETs, deposits, withdrawals, transfers). Full CRUD (spec FR-002/003) requires new service methods. They are exposed **only via MCP** to keep REST unchanged.

## R4: Delete rules (FKs are `Restrict`)
- **Decision**: Delete customer only when they own no accounts; delete account only when it has no transactions/transfers and zero balance. Otherwise return a `409 conflict`-style tool error (`has_dependents`). No cascade.
- **Rationale**: Preserves ledger integrity; matches existing Restrict foreign keys; resolves spec edge case.

## R5: Account funding
- **Decision**: `create_account` creates a zero-balance THB account. Funding uses `deposit`/`withdraw` tools wrapping `ChangeBalanceAsync`, so every balance change has a ledger entry.
- **Rationale**: Avoids balances without history. Deposit/withdraw are a small addition beyond the spec's list, needed to make transfers demonstrable; they already exist in REST.

## R6: Error mapping
- **Decision**: Catch `BankingProblem` in tools and return an MCP tool error result (`isError: true`) with text `"<code>: <detail>"` (e.g. `insufficient_funds`, `not_found`, `invalid_input`). Unexpected exceptions return a generic error without internals.
- **Rationale**: Gives assistants readable, recoverable errors (FR-008). The REST `IExceptionHandler` is not involved for `/mcp` tool errors.

## R7: Money and IDs
- **Decision**: Amounts are decimal strings (`"100.00"`), currency fixed `THB`, IDs are GUID strings — identical to REST DTOs.

## R8: REST Client test file
- **Decision**: `test.http` with `@baseUrl`, required headers `Content-Type: application/json` and `Accept: application/json, text/event-stream`, JSON-RPC bodies, `initialize`, `notifications/initialized` (expect 202), `tools/list`, and tool calls. Responses to POST may arrive as SSE (`event: message` / `data: {...}`), which REST Client cannot parse as JSON, so IDs are copied manually into `@customerId`, `@accountA`, etc. file variables; expected results are stated in comments.
- **Verify at implementation**: Confirm whether SDK 2.4.0 returns `application/json` or `text/event-stream` in stateless mode and adjust comments.

## R9: Security
- **Decision**: No authentication per requirement; bind to loopback by default (existing `127.0.0.1:8080`); document as demo-only. Browser-origin/DNS-rebinding risk is mitigated by loopback binding.
