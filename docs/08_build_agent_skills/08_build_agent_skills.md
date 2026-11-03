---
title: 'Exercise 08: Build agent skills'
layout: default
nav_order: 9
has_children: true
has_toc: true
---

# Exercise 08: Build agent skills

## Scenario

Get the `pptx` skill from [skills.sh](https://skills.sh/), then improve a copy named `blue-brand-pptx`. Use a customer-provided sample PDF to extract reusable presentation styles, icons, and images. Test the new skill by creating a presentation about **"compare GPT-6.1-sol with Claude Opus 5.5"**.

The repository supplies the walkthrough, not the skill, reference PDF, extracted assets, or finished presentation. Participants create these during the exercise.

## Prerequisites

- VS Code with Copilot Agent mode and agent skills support.
- Node.js and npm compatible with the [skills CLI](https://skills.sh/docs/cli); check its current runtime requirement before installing.
- **Sample PDF: `<customer to provide sample PDF>`.** Use a non-sensitive reference approved for processing in the workshop's AI environment, with permission to reuse its design and assets.
- The tools required by the installed `pptx` skill for generation, PDF inspection, asset extraction, and slide rendering. Review its instructions before installing dependencies.

Keep customer-specific PDFs and assets in the approved participant workspace. Do not commit them to this reusable workshop repository or send restricted material to an unapproved service.

## Objectives

- Understand the role and structure of an agent skill.
- Install and inspect an appropriately licensed `pptx` skill from skills.sh.
- Copy `.agents/skills/pptx` to `.agents/skills/blue-brand-pptx` without changing the original.
- Extract the supplied PDF's style and reusable icons/images into the new skill.
- Generate, inspect, and refine a branded model-comparison presentation without inventing model facts.

## Tasks

1. [Get the PPTX skill from skills.sh](08_01.md).
2. [Create the blue-brand-pptx skill](08_02.md).
3. [Extract the PDF style and assets](08_03.md).
4. [Test and refine the skill](08_04.md).
