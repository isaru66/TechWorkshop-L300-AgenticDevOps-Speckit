---
title: Introduction
layout: home
nav_order: 1
---

# Agentic development workshop

> **Placeholder content:** This is a sample workshop structure, not a finished course or a working application. Facilitators should refine and verify the instructions before use.

## Scenario and safety

Participants will plan and build a small fictional banking application using GitHub Copilot, Spec Kit, a Spring Boot REST/MCP backend, PostgreSQL, and a React frontend. The application is deliberately **unauthenticated**, is for **fictional data only**, and must **never be publicly exposed**. Run it in a private local environment; do not use real financial or personal information. No Azure provisioning is part of this workshop.

The illustrative target includes fictional customers and accounts, simulated deposits, withdrawals and transfers, and transaction history. These are example features to refine in the workshop, not a connection to a real banking system.

The application code, database seed content, Spec Kit project files, Compose file, and MCP client configuration are not supplied as a completed solution. Participants will create or initialize them during the relevant exercises.

## Illustrative architecture

![High-level application architecture]({{ '/media/Solution/app-highlevel-architecture.svg' | relative_url }})

- The React frontend sends REST requests to the Spring Boot backend.
- An MCP client interacts with the backend through its MCP interface.
- The backend reads and writes fictional records in PostgreSQL.

## Prerequisites

Bring a development environment where you can install the required tools and use VS Code and GitHub Copilot. The first exercise covers tool and access checks. A local container runtime is needed for the planned PostgreSQL exercise.

## Exercises

1. [Set up the development environment]({% link docs/01_setup_development_environment/01_setup_development_environment.md %})
2. [Introduction to GitHub Copilot]({% link docs/02_intro_to_github_copilot/02_intro_to_github_copilot.md %})
3. [Spec Kit fundamentals]({% link docs/03_spec_kit_fundamentals/03_spec_kit_fundamentals.md %})
4. [Specify and plan the banking API]({% link docs/04_specify_and_plan_banking_api/04_specify_and_plan_banking_api.md %})
5. [Add an MCP server]({% link docs/05_implement_backend_and_mcp/05_implement_backend_and_mcp.md %})
6. [Implement the frontend]({% link docs/06_implement_frontend/06_implement_frontend.md %})
7. [Test with MCP Inspector]({% link docs/07_connect_mcp_client/07_connect_mcp_client.md %})
8. [Build agent skills]({% link docs/08_build_agent_skills/08_build_agent_skills.md %})
