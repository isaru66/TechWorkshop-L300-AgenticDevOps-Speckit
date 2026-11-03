---
title: 'Exercise 01: Set up the development environment'
layout: default
nav_order: 2
has_children: true
has_toc: true
---

# Exercise 01: Set up the development environment

## Scenario

Prepare a workspace for the Agentic DevOps with Spec Kit workshop. You can install the tools on your local machine or use a GitHub Codespace with the repository's development container.

You will first use GitHub Copilot to build a standalone browser-game warm-up, then use Spec Kit to plan and build a fictional banking application with a Java Spring Boot REST/MCP backend, a React frontend, and PostgreSQL.

This repository is a **workshop starter**, not a working application. The application folders contain README placeholders; application code, fictional seed data, a Compose configuration, and MCP client configuration will be created during later exercises. No Azure subscription, Azure provisioning, or AI Gateway setup is required.

{: .warning }
> The planned banking application has **no application authentication**. Use fictional data only and keep the application in a private, trusted development environment. Never expose its REST or MCP interfaces, database, or forwarded ports publicly. Codespaces access controls do not add authentication to the application.

## Objectives

After you complete this exercise, you will be able to:

- Prepare your own repository copy and open it locally or in a GitHub Codespace.
- Install or verify the tools needed for the workshop.
- Confirm that GitHub Copilot Chat responds using an account with Copilot access.
- Understand which setup steps are automatic and which belong to later exercises.

## Prerequisites

- A GitHub account with access to the repository and permission to fork it.
- GitHub Copilot access through your account's entitlement or subscription.
- For local development, permission to install software on your machine.
- For Codespaces, permission and available quota to create a Codespace.

## Choose your environment

| Environment | What to prepare |
|---|---|
| **Local development** | Git, Visual Studio Code, JDK 21, Maven 3.9, Node.js 22 with npm, Python 3, and uv. A working Docker runtime with Docker Compose is needed for the PostgreSQL exercise. |
| **GitHub Codespaces** | Create a Codespace and wait for development-container setup to finish. It supplies JDK 21, Maven 3.9, Node.js 22, Python, uv, Docker-in-Docker with Compose v2, and the workshop VS Code extensions. |

The development container also installs the **Specify CLI (Spec Kit `v1.2.6`)** automatically, but does **not** initialize Spec Kit in the repository. Local CLI installation and workspace initialization are covered in [Exercise 03: Spec Kit fundamentals](../03_spec_kit_fundamentals/03_spec_kit_fundamentals.md).

## Tasks

1. [Fork and clone the repository](./01_01.md) - prepare your own copy, then clone it locally or create a Codespace. Use your fork's URL when working in your own copy.
2. [Install prerequisite software](./01_02.md) - install the local tools or verify the tools already provided by Codespaces.
3. [Check GitHub Copilot access](./01_03.md) - select the correct GitHub account and send a test message in Copilot Chat.

## Completion checklist

- Your workshop copy is open in VS Code or a fully initialized Codespace.
- Java and the Java compiler report version 21, Maven reports 3.9, and Node.js reports 22; npm, Python, and uv are available.
- Docker and Docker Compose are available for the later PostgreSQL exercise.
- Copilot Chat responds to a test message, and the **Plan** and **Agent** options are available for the next exercise.
- Any forwarded application ports remain **private**.

You do not need a running backend, frontend, database, or initialized Spec Kit project to complete this exercise.

## Duration

- **Estimated time:** 10 minutes if tools are already installed; allow additional time for downloads or Codespace creation.

## Next exercise

Continue to [Exercise 02: Introduction to GitHub Copilot](../02_intro_to_github_copilot/02_intro_to_github_copilot.md) to practice the **Plan, Build, Review** workflow.
