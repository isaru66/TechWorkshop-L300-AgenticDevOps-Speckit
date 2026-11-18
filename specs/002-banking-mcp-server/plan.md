# Implementation Plan: Banking MCP Server

**Branch**: `002-banking-mcp-server` | **Date**: 2026-11-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-banking-mcp-server/spec.md`

## Summary

Add an unauthenticated MCP server to the existing Spring Boot banking app, exposed at `/mcp` on port 8080 (localhost only) using Streamable HTTP in **stateless** mode. MCP tools are thin adapters over the existing `BankingService`, so PostgreSQL data, business rules, and REST APIs are shared and unchanged in behavior. Customer update/delete and account update/close are new operations: they are added to `BankingService` and also exposed in REST for consistency (new endpoints only; existing ones untouched). A Flyway migration adds account `status` and `label`. A `test.http` file for the VS Code REST Client exercises initialize, tool listing, and every tool.

## Technical Context

**Language/Version**: Java 21, Spring Boot 4.1.2 (existing)

**Primary Dependencies**: existing stack + `org.springframework.ai:spring-ai-starter-mcp-server-webmvc` via `spring-ai-bom` 2.0.2 (Spring AI 2.x is the Boot 4 line; annotation-based `@McpTool`)

**Storage**: PostgreSQL via JPA/Flyway (existing); new migration `V2__account_status_and_label.sql`

**Testing**: JUnit 5 + Testcontainers PostgreSQL (existing `*Test` / `*IT` conventions); MCP integration tests call `/mcp` over HTTP

**Target Platform**: Local workshop machine / devcontainer, bound to 127.0.0.1:8080

**Project Type**: Web service (existing `src/backend`)

**Performance Goals**: Lookups < 2 s at demo scale (SC-005)

**Constraints**: No auth (demo only, localhost bind retained); REST contract backward compatible; history never deleted; stateless transport so no session handling

**Scale/Scope**: 16 MCP tools; demo-scale data

## Constitution Check

`.specify/memory/constitution.md` is still the unfilled template, so there are no enforceable gates. Self-imposed principles applied: reuse the service layer (no duplicated rules), controllers/tools only delegate (existing `ControllerLayeringTest` extended to MCP tools), tests accompany each tool group, client-neutral docs. **Result: PASS (no violations). Post-design re-check: PASS.**

## Project Structure

### Documentation (this feature)

```text
specs/002-banking-mcp-server/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── mcp-tools.md          # MCP tool catalogue
│   └── rest-additions.yaml   # new REST endpoints
└── tasks.md                  # created by /speckit-tasks
```

### Source Code (repository root)

```text
src/backend/
├── pom.xml                                   # + spring-ai-bom, MCP starter
├── test.http                                 # VS Code REST Client: MCP + REST samples
└── src/
    ├── main/
    │   ├── java/com/workshop/banking/
    │   │   ├── mcp/BankingMcpTools.java      # @McpTool adapters -> BankingService
    │   │   ├── mcp/McpErrorMapper.java       # BankingException -> tool error result
    │   │   ├── service/BankingService.java   # + update/delete customer, update/close account, search
    │   │   ├── api/CustomerController.java   # + PUT/DELETE
    │   │   ├── api/AccountController.java    # + PATCH, POST /close
    │   │   └── domain/Account.java           # + status, label
    │   └── resources/
    │       ├── application.yml               # spring.ai.mcp.server.* (stateless, /mcp)
    │       └── db/migration/V2__account_status_and_label.sql
    └── test/java/com/workshop/banking/
        ├── mcp/McpToolsIT.java               # initialize, tools/list, calls, error cases
        └── service/AccountLifecycleIT.java   # close/delete rules, closed-account rejections
```

**Structure Decision**: Keep the single existing backend module; add one `mcp` package. No new modules or services.

## Complexity Tracking

No violations.
