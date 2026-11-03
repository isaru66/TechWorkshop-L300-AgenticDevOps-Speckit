---
title: 'Exercise 05: Add an MCP server'
layout: default
nav_order: 6
has_children: true
has_toc: true
---

# Exercise 05: Add an MCP server

> **Workshop scope:** Add a simple MCP server to the existing fictional banking demo without authentication. Run everything locally and never expose it publicly.

## Scenario

Continue from the banking demo built in Exercise 04. Follow **01. Specify**, **02. Plan**, **03. Tasks**, **04. Implement**, and **05. Converge** to let AI assistants manage customers, accounts, simulated transfers, and transaction history through MCP. Use **06. Test with HTTP file** to verify a Streamable HTTP server.

Run each Spec Kit skill separately in Copilot chat and review its result before continuing. Reuse the existing banking business logic and PostgreSQL data; preserve the REST APIs and ReactJS interface.

## Objectives

- Specify the MCP capabilities and clarify which read and write operations are allowed.
- Plan a compatible Java Spring Boot MCP integration and choose a local transport.
- Implement MCP tools using the existing banking services without duplicating business rules.
- Verify tool discovery, invocation, data consistency, and unchanged REST behavior.
