# Implementation Plan: MCP Banking Server

**Branch**: `002-mcp-banking-server` | **Date**: 2026-11-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-mcp-banking-server/spec.md`

## Summary

Add an MCP server endpoint (`/mcp`, Streamable HTTP, stateless, no auth) inside the existing `Banking.Api` ASP.NET Core app using the official `ModelContextProtocol.AspNetCore` SDK. Tools are thin wrappers over `BankingService`, so business rules, PostgreSQL data, and locking/ledger behavior are shared with REST. Because `BankingService` currently has no create/update/delete for customers or accounts, those operations are added to the service (exposed via MCP only, so REST stays unchanged). A runnable `test.http` for the VS Code REST Client is delivered alongside.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`, SDK 10.0.404)

**Primary Dependencies**: Existing ASP.NET Core, EF Core 10 + Npgsql; new `ModelContextProtocol.AspNetCore` 2.4.0 (stable, .NET 8+)

**Storage**: Existing PostgreSQL via `BankingDbContext`; no schema change

**Testing**: xUnit + `Microsoft.AspNetCore.Mvc.Testing` + existing `PostgresFixture`/`BankingApiFactory` in `Banking.Api.Tests`; manual `test.http`

**Target Platform**: Local/workshop, Kestrel at `http://127.0.0.1:8080`

**Project Type**: web-service (single existing backend project)

**Performance Goals**: Typical tool call < 2 s (SC-005)

**Constraints**: No auth; stateless transport; REST routes/contracts unchanged; money as THB decimal strings

**Scale/Scope**: Demo scale; 13 tools

## Constitution Check

`.specify/memory/constitution.md` is an unfilled template, so there are no ratified gates. Applied spec principles instead: simplicity (reuse service, no extra project), shared business rules (FR-007), tests in the existing test project. **Pass** before and after design.

## Project Structure

### Documentation (this feature)

```text
specs/002-mcp-banking-server/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── mcp-tools.md     # Tool catalog and schemas
│   └── test.http        # Draft; implemented at src/backend/Banking.Api/test.http
└── tasks.md             # Created by /speckit-tasks
```

### Source Code (repository root)

```text
src/backend/
├── Banking.Api/
│   ├── Banking.Api.csproj          # + ModelContextProtocol.AspNetCore
│   ├── Program.cs                  # AddMcpServer().WithHttpTransport(stateless).WithTools<...>(); MapMcp("/mcp")
│   ├── Mcp/
│   │   ├── CustomerTools.cs
│   │   ├── AccountTools.cs
│   │   ├── TransferTools.cs
│   │   ├── HistoryTools.cs
│   │   └── McpErrors.cs            # BankingProblem -> tool error result
│   ├── Services/BankingService.cs  # + customer/account create/update/delete
│   ├── Contracts/                  # + write request DTOs
│   └── test.http
└── Banking.Api.Tests/
    └── Mcp/                        # tool listing, CRUD, transfer, history, REST regression
```

**Structure Decision**: Single web project; MCP tools in an `Mcp/` folder depending only on `BankingService`.

## Complexity Tracking

No violations.
