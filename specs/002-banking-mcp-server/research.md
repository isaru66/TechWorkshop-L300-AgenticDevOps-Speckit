# Research: Banking MCP Server

## R1. MCP SDK / integration
- **Decision**: Spring AI MCP server starter `spring-ai-starter-mcp-server-webmvc` (BOM `spring-ai-bom:2.0.2`), annotation-based tools (`@McpTool`, `@McpToolParam`).
- **Rationale**: User choice; existing app is Spring MVC (`spring-boot-starter-webmvc`); Spring AI 2.0.x is the line built for Spring Boot 4 (1.x targets Boot 3). Latest GA at planning time is 2.0.2 (2.1.0 only RC).
- **Alternatives**: official MCP Java SDK directly (more wiring, no annotation model); Spring AI 1.1.x (Boot 3 only); 2.1.0-RC (not GA).
- **Verify at implementation**: exact annotation package/property names in 2.0.2 (e.g. `org.springframework.ai.mcp.annotation.McpTool`), and that the starter's auto-config works with Boot 4.1.2; fall back to the nearest compatible 2.0.x if not.

## R2. Transport
- **Decision**: Streamable HTTP, **stateless** (`spring.ai.mcp.server.protocol=STATELESS`), endpoint `/mcp` on the existing port/binding (127.0.0.1:8080).
- **Rationale**: User choice. Stateless means no `Mcp-Session-Id`; each POST is self-contained, so `test.http` needs no session capture. Simplest for a demo and for REST Client.
- **Consequences**: No server-initiated messages/notifications; no SSE stream. `initialize` is still sent by clients (and in `test.http`) and answered, but later calls do not depend on it. Clients must send `Accept: application/json, text/event-stream` and `Content-Type: application/json`.
- **Alternatives**: stateful Streamable HTTP; legacy SSE (deprecated).

## R3. Tool surface and read/write scope
- **Decision**: Reads: list/search/get customers, list customer accounts, get account, get balance, transaction history. Writes: create/update/delete customer, open/update/close account, deposit, withdraw, transfer. Destructive tools delete/close included per user decision, enforced by rules in R5.
- **Rationale**: Matches spec FR-003..FR-009. Tool names verb_noun snake_case with descriptions for discoverability (FR-012).
- **Alternatives**: read-only first; omit deletes (rejected by user).

## R4. Reuse and consistency
- **Decision**: Tools call `BankingService` only (no repositories). New lifecycle operations are added to `BankingService`, then exposed by both MCP tools and new REST endpoints. Existing REST endpoints/DTOs unchanged.
- **Rationale**: One source of rules (FR-010, FR-013); user chose REST parity.

## R5. Lifecycle rules and schema
- **Decision**: Migration `V2` adds `account.status` (`OPEN`|`CLOSED`, default `OPEN`, CHECK) and nullable `account.label`. Close requires balance = 0 and sets `CLOSED`; deposit/withdraw/transfer on closed accounts rejected (`ACCOUNT_CLOSED`). Customer delete requires zero accounts (`CUSTOMER_HAS_ACCOUNTS`); closed accounts still count so history is never orphaned. Update changes only descriptive fields.
- **Rationale**: Existing schema has no status; FK from transaction history prevents hard-deleting accounts anyway.
- **Alternatives**: hard-delete accounts (loses history); cascade customer delete (destroys financial records).

## R6. Search
- **Decision**: `search_customers` takes a case-insensitive `query` matched against name, English name, email, phone; paged. Add repository query method.
- **Alternatives**: client-side filtering of full list (not bounded).

## R7. Amounts and ids
- **Decision**: Tool params mirror REST: ids as UUID strings, amounts as decimal strings (existing `parseAmount` regex), pagination `page`/`size` with max 100.
- **Rationale**: Avoids float precision issues; identical validation (FR-010).

## R8. Errors
- **Decision**: `BankingException` is converted to an MCP tool result with `isError=true` and text `"<CODE>: <message>"`; unexpected exceptions return a generic error. No partial writes since service methods are transactional.

## R9. Security
- **Decision**: No auth; keep `server.address=127.0.0.1`; document the risk in quickstart. Seed/sample data is fictional and client-neutral.

## R10. `test.http`
- **Decision**: `src/backend/test.http` using REST Client variables (`@baseUrl`, `@customerId`, ...), named requests (`# @name`) and `{{name.response.body.$.result...}}` chaining; includes initialize, `notifications/initialized`, `tools/list`, every tool call, error cases, and `# Expected:` comments. Stateless, so no session header. Responses may be JSON or SSE-framed (`data:` lines) depending on Accept; samples send `application/json, text/event-stream`, and the doc notes how to read either.
