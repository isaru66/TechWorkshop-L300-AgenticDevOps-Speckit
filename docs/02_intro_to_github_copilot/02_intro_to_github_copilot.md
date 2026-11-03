---
title: 'Exercise 02: Introduction to GitHub Copilot'
layout: default
nav_order: 3
has_children: true
has_toc: true
---

# Exercise 02: Introduction to GitHub Copilot

## Scenario

Build **Copilot Flappy**, a small Flappy Bird-style browser game with the
GitHub Copilot mascot as the main character. Guide the mascot through gaps
between obstacles, earn points, and restart after a collision.

Use **Plan mode first**, review the proposed approach, then hand the approved
plan to **Agent mode** to build it. Finish by playing the game and reviewing
the changes. You are writing prompts and evaluating the result, not copying
a finished implementation.

This is a standalone warm-up, separate from the workshop's banking application.
Use original obstacle and background visuals rather than copying the original
Flappy Bird artwork.

## Prerequisites

- VS Code (locally or in GitHub Codespaces) with GitHub Copilot access.
- The **Plan** and **Agent** options available in Copilot Chat.
- Node.js 22 and npm, plus a modern browser.
- The supplied [Copilot mascot image](../../media/common/Copilot%20mascot.png).

## Exercise flow

1. [Explore Copilot and prepare the game context](02_01.md).
2. [Create and review a plan in Plan mode](02_02.md).
3. [Build the approved plan in Agent mode](02_03.md).
4. [Play, review, and refine the result](02_04.md).

## Objectives

- Explore Copilot's role and the participant's responsibility for decisions.
- Follow **Plan, Build, Review** in that order.
- Provide an image asset and clear acceptance criteria as context.
- Build and verify a playable game with keyboard and pointer controls.
- Avoid sending secrets or real customer data to an assistant.
