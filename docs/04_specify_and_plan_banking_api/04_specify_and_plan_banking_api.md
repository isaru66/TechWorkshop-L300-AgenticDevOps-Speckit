---
title: 'Exercise 04: Specify and plan the banking API'
layout: default
nav_order: 5
has_children: true
has_toc: true
---

# Exercise 04: Specify and plan the banking API

> **Workshop scope:** Build a simple fictional banking API without user authentication. Run everything locally and never expose it publicly.

## Scenario

Follow the short Spec Kit workflow: **01. Specify**, **02. Plan**, **03. Tasks**, **04. Implement**, and **05. Converge**. Build a simple banking API using seeded fictional accounts and simulated transactions. Run each skill separately in Copilot chat and review its result before continuing.

Choose one backend path: **Java Spring Boot 4.1** with JDK 21 and Maven 3.9, or an **ASP.NET Core 10** controller-based Web API with the .NET 10 SDK. Both use a React interface and local PostgreSQL. Check the [prerequisite software]({% link docs/01_setup_development_environment/01_02.md %}) for your chosen path before planning.

The documentation's stack tabs show Java by default and, when browser storage is available, remember an explicit selection separately from operating-system tabs. Choosing a tab does not select a technology in Spec Kit or change generated artifacts; use the corresponding planning prompt and implement only that backend. You do not need to run both.

The .NET path is supported through **Exercise 04 only**. Exercises 05–07 retain Java-oriented MCP scaffolding and are not a completed .NET continuation.

## Objectives

- Specify the banking API behavior without user authentication.
- Generate and review a technical plan for the selected backend and dependency-ordered tasks.
- Implement and test the banking REST API and a simple ReactJS interface.
- Converge the implementation against the approved specification, plan, and tasks.
