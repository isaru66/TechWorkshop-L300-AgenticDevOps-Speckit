---
title: Introduction
layout: home
nav_order: 1
---

# Agentic development workshop

> **Placeholder content:** This is a sample workshop structure, not a finished course or a working application. Facilitators should refine and verify the instructions before use.

## Scenario and safety

Through Exercise 04, participants plan and build a small fictional banking application using GitHub Copilot, Spec Kit, and one REST backend: Java Spring Boot or ASP.NET Core 10 on .NET 10. Both paths use PostgreSQL and a React frontend. The application is deliberately **unauthenticated**, is for **fictional data only**, and must **never be publicly exposed**. Run it in a private local environment; do not use real financial or personal information. No Azure provisioning is part of this workshop.

The illustrative target includes fictional customers and accounts, simulated deposits, withdrawals and transfers, and transaction history. These are example features to refine in the workshop, not a connection to a real banking system.

The repository tracks four fictional CSV seed files in `src/data/seed/`. Application code, a populated database, Spec Kit project files, a Compose file, and MCP client configuration are not supplied as a completed solution. Participants create or initialize those pieces and plan the seed import during the relevant exercises.

## Illustrative architecture

The following Java/Spring Boot diagram is an illustration of the longer-term architecture, including the later MCP concept. It does not depict a completed .NET path.

![Java-oriented illustration of the longer-term application architecture]({{ '/media/Solution/app-highlevel-architecture.svg' | relative_url }})

- The React frontend sends REST requests to the selected Java Spring Boot or ASP.NET Core 10 backend.
- The backend reads and writes fictional records in PostgreSQL.
- Exercises 05-07 are later Java/MCP-oriented scaffolding, not a completed .NET continuation.

## Prerequisites

Bring a development environment where you can use VS Code and GitHub Copilot. For local work, install the shared tools (Git, Node.js 22 with npm, Python 3, uv, and Docker with Compose) and the selected backend's toolchain: JDK 21 with Maven 3.9 for Java, or the .NET 10 SDK (10.0) for .NET. The development container supplies both backend toolchains; rebuild an existing container to pick up updates. The first exercise covers installation and access checks.

## Exercises

1. [Set up the development environment]({% link docs/01_setup_development_environment/01_setup_development_environment.md %})

   **Exercise 01 Alternative:** [Use GitHub Codespaces]({% link docs/01_alternative_github_codespaces/01_alternative_github_codespaces.md %})

2. [Introduction to GitHub Copilot]({% link docs/02_intro_to_github_copilot/02_intro_to_github_copilot.md %})
3. [Spec Kit fundamentals]({% link docs/03_spec_kit_fundamentals/03_spec_kit_fundamentals.md %})
4. [Specify and plan the banking API]({% link docs/04_specify_and_plan_banking_api/04_specify_and_plan_banking_api.md %})
5. [Add an MCP server]({% link docs/05_implement_backend_and_mcp/05_implement_backend_and_mcp.md %})
6. [Implement the frontend]({% link docs/06_implement_frontend/06_implement_frontend.md %})
7. [Test with MCP Inspector]({% link docs/07_connect_mcp_client/07_connect_mcp_client.md %})
8. [Build agent skills]({% link docs/08_build_agent_skills/08_build_agent_skills.md %})
