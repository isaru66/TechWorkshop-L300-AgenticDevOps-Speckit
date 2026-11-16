# Implementation Plan: Local Banking API

**Branch**: `001-local-banking-api` | **Date**: 2026-11-16 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-local-banking-api/spec.md`

## Summary

A fictional, local-only banking system: a Spring Boot REST backend (port 8080) backed by PostgreSQL, plus a React + TypeScript UI. A single banking service holds all rules (validation, insufficient-funds, atomic transfers) and is used by thin controllers. On startup the backend imports the four seed CSVs insert-only and in one transaction, so repeat starts never duplicate, overwrite later changes, or replay history into balances. OpenAPI documentation and Swagger UI are enabled for local development. The UI uses an Azure-style blue and white theme, has no login, and talks to the backend through a dev-server proxy.

## Technical Context

**Language/Version**: Java 21 (backend); TypeScript 5.x on Node.js 20+ (frontend)

**Primary Dependencies**: Spring Boot 4.1.x (4.1.2 is the latest 4.1 release; see research R1) with web, data-jpa, validation, actuator; Flyway; springdoc-openapi (Spring Boot 4 line); React with Vite, React Router, TanStack Query

**Storage**: PostgreSQL in a local Docker container, with money as `numeric(19,2)` mapped to `BigDecimal`. Connection settings come only from environment variables and are not documented in the repo.

**Testing**: JUnit 5 + Spring Boot Test + Testcontainers (PostgreSQL) for backend; Vitest + React Testing Library for frontend; scripted REST verification in the quickstart

**Target Platform**: Developer workstation (Windows/WSL/Linux); private, loopback only

**Project Type**: Web application (backend + frontend)

**Performance Goals**: Import of ~1,400 rows under 10 s; typical API calls under 200 ms locally

**Constraints**: Maven 3.9; backend on port 8080; all services and forwarded ports bound to loopback or private network; no authentication, public deployment, or MCP work; fictional data only; client-neutral UI

**Scale/Scope**: 102 customers, 150 accounts, 101 transfers, 1002 entries; one developer at a time, with concurrent-request safety required

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is still the unfilled template, so it defines no enforceable principles. The plan instead follows the spec's constraints (FR-015 to FR-018) and the repository safety guidance (fictional data, private environment, no authentication). **Result: PASS (no violations).** Re-check after design: PASS; no complexity exceptions are needed.

## Project Structure

### Documentation (this feature)

```text
specs/001-local-banking-api/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── openapi.yaml
├── checklists/
│   └── requirements.md
└── tasks.md             # created later by /speckit-tasks
```

### Source Code (repository root)

```text
src/
├── data/seed/                       # existing, read-only CSV baseline
├── backend/
│   ├── pom.xml
│   ├── src/main/java/<base-package>/
│   │   ├── BankingApplication.java
│   │   ├── api/                     # controllers, DTOs, error handler (no business rules)
│   │   ├── service/                 # BankingService: all banking rules
│   │   ├── domain/                  # JPA entities, enums
│   │   ├── repository/
│   │   └── seed/                    # SeedImporter (startup runner), CSV parsing and validation
│   ├── src/main/resources/
│   │   ├── application.yml          # reads DB settings from environment variables
│   │   └── db/migration/            # Flyway SQL
│   └── src/test/java/...            # unit, integration, contract tests
└── frontend/
    ├── package.json
    ├── vite.config.ts               # dev server 5173, proxy /api -> 8080
    └── src/
        ├── api/                     # typed REST client
        ├── components/
        ├── pages/                   # Customers, Accounts, AccountDetail, Transfer
        ├── theme/                   # Azure-style blue/white tokens
        └── test/
```

**Structure Decision**: Web application layout using the existing `src/backend` and `src/frontend` placeholders. The seed path stays at `src/data/seed`; the backend locates it through a configurable property, defaulting to the repository-relative path.

## Complexity Tracking

No constitution violations; nothing to justify.
