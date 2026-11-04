# Agentic DevOps with Spec Kit workshop

This repository is a **workshop starter**, not a finished application. It
provides guided exercises for practicing specification-driven development
and agentic DevOps with GitHub Copilot and Spec Kit. Start with a standalone
browser-game warm-up, then plan and build a fictional banking demo.

The intended result is a Java Spring Boot backend with REST and Model Context
Protocol (MCP) interfaces, a React frontend, and PostgreSQL for persistence.
The application is deliberately designed **without application authentication**.
Its illustrative scope includes fictional customers and accounts, simulated
deposits, withdrawals and transfers, and transaction history.

> [!CAUTION]
> **Fictional data only. No application authentication. Private use only.**
> Use the eventual application only in a private, trusted development
> environment. Anyone who can reach its REST or MCP interfaces may be able
> to access or change its data. Do not expose the application or forwarded
> ports to the public internet, and never use real personal, financial, or
> production data. This workshop is not a production security model.

## What you will learn

- Use Copilot **Plan mode** to explore requirements before handing an
  approved approach to **Agent mode**.
- Follow **Plan, Build, Review**: keep people responsible for decisions,
  inspect generated changes, and verify behavior.
- Initialize Spec Kit and work through specifications, technical plans,
  dependency-ordered tasks, implementation, and convergence.
- Build and check REST and MCP capabilities, connect a React frontend,
  and configure a private MCP client.
- Explore reusable agent skills using a generic, non-sensitive brief.

## Step by Step Instructions

The step by step instructions for this lab can be found in the [AgenticDevOps-Speckit](https://isaru66.github.io/TechWorkshop-L300-AgenticDevOps-Speckit)

| Exercise | Focus |
|---|---|
| [01. Set up the development environment](docs/01_setup_development_environment/01_setup_development_environment.md) | Prepare a local workspace or Codespace, verify tools, and check Copilot access. |
| [02. Introduction to GitHub Copilot](docs/02_intro_to_github_copilot/02_intro_to_github_copilot.md) | Plan, build, and play **Copilot Flappy**, a standalone browser-game warm-up. |
| [03. Spec Kit fundamentals](docs/03_spec_kit_fundamentals/03_spec_kit_fundamentals.md) | Install or verify the Specify CLI, initialize the workspace, and explore the spec-driven workflow. |
| [04. Specify and plan the banking API](docs/04_specify_and_plan_banking_api/04_specify_and_plan_banking_api.md) | Specify, plan, break down tasks, implement, and converge a fictional banking API and simple React interface. |
| [05. Implement the backend and MCP](docs/05_implement_backend_and_mcp/05_implement_backend_and_mcp.md) | Build and check the Spring Boot REST/MCP backend and local PostgreSQL setup. |
| [06. Implement the frontend](docs/06_implement_frontend/06_implement_frontend.md) | Build accessible React views and connect them to the private backend. |
| [07. Connect an MCP client](docs/07_connect_mcp_client/07_connect_mcp_client.md) | Configure VS Code to discover and safely test the local MCP capability. |
| [08. Build agent skills](docs/08_build_agent_skills/08_build_agent_skills.md) | Install a PPTX skill, create `blue-brand-pptx` from an approved sample PDF, and test a model-comparison deck. |

**Content status:** Exercises 01-04 contain setup instructions and guided
workflows. Exercises 05-07 are currently scaffolding and require facilitator
review and expansion before delivery. Exercise 08 provides a skill-building
walkthrough and requires a customer-supplied, approved sample PDF. No finished
PPTX skill or presentation is supplied.

## What is included

The initial `src` tree contains only README placeholders. It does not include
application code, a database, CSV records, a Compose file, or an initialized
Spec Kit project. Attendees create the application and any fictional data as
part of the workshop. There is no application build or start command to run
immediately after cloning.

The repository does include:

- Workshop pages, task walkthroughs, screenshots, and a Copilot mascot asset.
- A development-container configuration with the workshop toolchain.
- A Jekyll documentation site using the Just the Docs theme.
- Shared operating-system tabs and code-copy controls for documentation.
- A GitHub Actions workflow for publishing the documentation to Pages.

## Intended application architecture

```text
React frontend ---- REST ----> Spring Boot backend ----> PostgreSQL
MCP client -------- MCP -----> Spring Boot backend
```

Both interfaces belong to the attendee-created backend. Keep all components
private and use fictional records only. The browser-game warm-up is separate
from this banking application.

## Prerequisites

You need a GitHub account, GitHub Copilot access, and a modern browser.
In Copilot Chat, confirm that **Plan** and **Agent** are available.
No Azure subscription, Azure resource provisioning, or AI Gateway setup is
required for this workshop.

## Getting started

### Option A: GitHub Codespaces

1. Fork the repository to keep workshop changes in your own copy.
2. In your fork, use **Code > Codespaces > Create codespace**. Your account
   needs permission and available quota to create a Codespace.
3. Wait for development-container setup to finish. It supplies JDK 21,
   Maven 3.9, Node.js 22 with npm, Python, uv, and Docker-in-Docker with
   Docker Compose v2.
4. Confirm that Copilot Chat responds, then follow the
   [setup exercise](docs/01_setup_development_environment/01_setup_development_environment.md).

Container creation also installs the Specify CLI (Spec Kit `v1.2.6`) with
`uv`; it does **not** run `specify init`. Java, Spring Boot, Copilot, REST
Client, and PostgreSQL VS Code extensions are configured in the
[development container](.devcontainer/devcontainer.json).

Keep any forwarded application ports private. Codespaces access controls do
not add authentication to the application or its REST and MCP interfaces.

### Option B: Local development

Install Git and an editor such as Visual Studio Code, plus the tools used in
the exercises:

- Java Development Kit (JDK) 21 and Maven 3.9.
- Node.js 22 and npm.
- Python 3 and [uv](https://docs.astral.sh/uv/).
- GitHub Copilot access for its exercises, or another compatible AI coding
  assistant where the exercise permits one.
- Docker with Docker Compose for the PostgreSQL exercise. Attendees create
  their own Compose configuration during the workshop; none is supplied
  here.

Fork the repository, clone **your fork**, and open the working copy in
VS Code. Follow the [prerequisite installation guide](docs/01_setup_development_environment/01_02.md)
for platform-specific installation and verification, then
[check Copilot access](docs/01_setup_development_environment/01_03.md).

In [Exercise 03](docs/03_spec_kit_fundamentals/03_spec_kit_fundamentals.md),
install the pinned Specify CLI with `uv` if needed and initialize Spec Kit in
the working copy. Do not assume generated specifications or agent instructions
already exist.

### Development-container ports

The container declares these forwarding ports for later use; it does not
start the corresponding services.

| Port | Intended use |
|---|---|
| 8080 | Spring Boot backend |
| 5173 | React frontend |
| 5432 | PostgreSQL |
| 4000 | Documentation preview |

Keep forwarded ports **private**, including when sharing a workshop workspace.

## Repository layout

- [`src/`](src/README.md): placeholders for the attendee-created application.
- [`src/backend/`](src/backend/README.md): future Spring Boot backend.
- [`src/frontend/`](src/frontend/README.md): future React frontend.
- [`src/data/seed/`](src/data/seed/README.md): reserved for attendee-created
  fictional CSV data.
- [`docs/`](docs/): workshop documentation and exercise pages.
- [`.devcontainer/`](.devcontainer/devcontainer.json): toolchain, extensions,
  Specify CLI installation, and port-forwarding configuration.
- [`.vscode/`](.vscode/): empty apart from `.gitkeep`; attendees add their
  own editor and MCP configuration later.
- [`media/`](media/): screenshots and workshop image assets.
- [`_includes/`](_includes/), [`_sass/`](_sass/), and [`assets/`](assets/):
  documentation layout helpers, theme styling, and browser behavior.
- [`_config.yml`](_config.yml) and [`Gemfile`](Gemfile): documentation-site
  configuration and Ruby dependencies.
- [`.github/workflows/jekyll-gh-pages.yml`](.github/workflows/jekyll-gh-pages.yml):
  documentation build and deployment workflow.

## Previewing the guide

With Ruby 3.3 and Bundler installed, start the documentation site locally.
They are not preinstalled in the Codespace development container.

```shell
bundle install
bundle exec jekyll serve
```

Open the address printed by Jekyll. To access the preview through a private
forwarded port in a Codespace, bind the preview server to the container's
network interface:

```shell
bundle exec jekyll serve --host 0.0.0.0
```

For a build-only check when editing site content or templates:

```shell
bundle exec jekyll build
```

These commands build the documentation, not the workshop application.

## Authoring operating-system tabs

Use the shared `os-tabs.html` include for instructions that differ between
Windows, macOS, and Linux / GitHub Codespaces. Keep identical commands outside
the tabs. In the Linux content, distinguish local installation from verification
of preinstalled Codespaces tools.

Capture each panel as Markdown, then pass the captures to the include:

````liquid
{% capture windows %}
```powershell
python --version
```
{% endcapture %}
{% capture macos %}
```bash
python3 --version
```
{% endcapture %}
{% capture linux %}
Python is already installed in the workshop Codespace.

```bash
python3 --version
```
{% endcapture %}
{% include os-tabs.html id="python-check" label="Check Python version" windows=windows macos=macos linux=linux %}
````

Supply all three panels and a descriptive `label`. Use a unique, lowercase,
hyphenated `id` per group on each page. Keep capture/include tags on separate
lines with blank lines around fenced code. The include renders Markdown, so do
not pre-render captures with `markdownify`. It can be used inside the existing
`<details markdown="block">` solutions; do not nest tab groups.

Tabs detect the initial browser OS (Windows fallback), synchronize on each page,
and remember explicit selections across pages. Select Linux / GitHub Codespaces
manually when working in a Codespace from a Windows or Mac browser. Without
JavaScript, all panels remain readable.

Just the Docs-style clipboard icons appear at the top-right of all code blocks
on hover or keyboard focus (always visible on touch devices), both inside and
outside OS tabs, including pages without tabs. They copy the exact code text using the
Clipboard API on HTTPS or localhost, with explicit success/failure feedback and
a manual selection option when copying is denied. The theme's global copy
buttons are disabled to prevent duplicate controls.

## Publishing the documentation

To publish the guide for a repository where you have administrative access,
open **Settings > Pages** and set **Build and deployment > Source** to
**GitHub Actions**. The Pages workflow must complete successfully before the
site is available. It runs on pushes to `main` or can be started manually,
builds with Ruby 3.3, and uses the Pages-provided site URL and base path.
The project-site address normally follows
`https://<owner>.github.io/<repository>/`; forks and custom domains can differ.
Publishing documentation does not deploy or secure the workshop application.

## Support and security

See [SUPPORT.md](SUPPORT.md) for questions and [SECURITY.md](SECURITY.md) for
private vulnerability reporting and safe use. This starter is distributed
under the [MIT License](LICENSE).

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft
trademarks or logos is subject to and must follow
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
