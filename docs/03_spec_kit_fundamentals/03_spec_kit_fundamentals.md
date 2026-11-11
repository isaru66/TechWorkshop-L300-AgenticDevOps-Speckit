---
title: 'Exercise 03: Spec Kit fundamentals'
layout: default
nav_order: 4
has_children: true
has_toc: true
---

# Exercise 03: Spec Kit fundamentals

## Scenario

Install the Specify CLI and initialize Spec Kit in your own workshop workspace. The starter repository is not pre-initialized for Spec Kit.

The CLI installation, initialization command, script type choices, and `/speckit-*` skill names are the same for Java and .NET. Choose the script type for your shell, not your backend.

## Objectives

- Install and verify the Specify CLI.
- Initialize the participant's workspace and review the generated files.
- Choose the appropriate script type, reload VS Code, and verify the Spec Kit skills in Copilot Chat.
- Understand how a shared specification leads to a backend-specific plan and implementation tasks.

## Tasks

1. [Install the Specify CLI]({% link docs/03_spec_kit_fundamentals/03_01.md %}) - install or verify the pinned `v1.2.6` release.
2. [Initialize Spec Kit]({% link docs/03_spec_kit_fundamentals/03_02.md %}) - set up the Copilot integration and verify the available chat skills.
3. [Explore the spec-driven workflow]({% link docs/03_spec_kit_fundamentals/03_03.md %}) - distinguish project principles, specifications, plans, and tasks.

The walkthrough uses screenshots from a Windows PowerShell workspace. Choose the installation tab and script type appropriate to your own environment; paths and platform details may differ.

Before Exercise 04, check the [prerequisite software]({% link docs/01_setup_development_environment/01_02.md %}) for your chosen backend and the shared tools. The Java path uses JDK 21 and Maven 3.9; the .NET path uses the .NET 10 SDK. You only need to implement and run one backend.
