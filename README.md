# Agentic DevOps with Spec Kit workshop

This repository is a **workshop starter**, not a finished application. It
provides a place to practice specification-driven development and agentic
DevOps workflows while building a fictional banking demo in the exercises.

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

## Workshop guide

- Start with the [workshop home](index.md) and the [documentation](docs/).
  Exercise pages are scaffolding until their instructions are written.
- The [intended GitHub Pages site](https://isaru66.github.io/TechWorkshop-L300-AgenticDevOps-Speckit/)
  is available only after Pages is configured and the site is deployed. A
  fork or custom domain will have a different address.

The initial `src` tree contains only README placeholders. It does not include
application code, a database, CSV records, a Compose file, or an initialized
Spec Kit project. Attendees create the application and any fictional data as
part of the workshop.

## Prerequisites

No Azure subscription or Azure resource provisioning is required for this
workshop.

### GitHub Codespaces

You need a GitHub account with access to this repository and permission to
create a Codespace. Use **Code > Codespaces > Create codespace**, then allow
the repository's development container to finish setting up. Container
creation also installs the Specify CLI (Spec Kit `v1.2.6`) with `uv`; it does
not run `specify init`. The planned
GitHub Copilot exercises require Copilot access, which may depend on your
account's entitlement or subscription.

Keep any forwarded application ports private. Codespaces access controls do
not add authentication to the application or its REST and MCP interfaces.

### Local development

Install Git and an editor such as Visual Studio Code, plus the tools used in
the exercises:

- Java Development Kit (JDK) 21 and Maven.
- Node.js 22 and npm.
- Python 3 and [uv](https://docs.astral.sh/uv/).
- GitHub Copilot access for its exercises, or another compatible AI coding
  assistant where the exercise permits one.
- Docker with Docker Compose for the PostgreSQL exercise. Attendees create
  their own Compose configuration during the workshop; none is supplied
  here.

When using the development container, the Specify CLI is installed
automatically. Otherwise, in the relevant exercise, follow the guide to install
Spec Kit with `uv`. In either case, initialize it with `specify init` in the working copy. Spec Kit is not
initialized in this starter; do not assume generated specifications or agent
instructions already exist.

## Repository layout

- [`src/`](src/README.md): placeholders for the attendee-created application.
- [`src/backend/`](src/backend/README.md): future Spring Boot backend.
- [`src/frontend/`](src/frontend/README.md): future React frontend.
- [`src/data/seed/`](src/data/seed/README.md): reserved for attendee-created
  fictional CSV data.
- [`docs/`](docs/): workshop documentation and exercise pages.
- [`.vscode/`](.vscode/): empty apart from `.gitkeep`; attendees add their
  own editor and MCP configuration later.

## Previewing the guide

With Ruby 3.3 and Bundler installed, start the documentation site locally.
They are not preinstalled in the Codespace development container.

```shell
bundle install
bundle exec jekyll serve
```

Open the address printed by Jekyll. In a Codespace, use the forwarded port.

## Publishing the documentation

To publish the guide for a repository where you have administrative access,
open **Settings > Pages** and set **Build and deployment > Source** to
**GitHub Actions**. The Pages workflow must complete successfully before the
site is available. The project-site address normally follows
`https://<owner>.github.io/<repository>/`; forks and custom domains can differ.
Publishing documentation does not deploy or secure the workshop application.

## Support and security

See [SUPPORT.md](SUPPORT.md) for questions and [SECURITY.md](SECURITY.md) for
private vulnerability reporting and safe use. This starter is distributed
under the [MIT License](LICENSE).
