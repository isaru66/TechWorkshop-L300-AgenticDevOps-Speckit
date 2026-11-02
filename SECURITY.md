# Security

## Safe workshop use

This is a workshop starter, not a production application. The intended
application deliberately has no application authentication. Use only
fictional data in a private, trusted development environment; do not expose
REST or MCP endpoints or forwarded ports publicly. Never put real personal
or financial data, production data, passwords, tokens, or private keys in
the repository or workshop data.

Keep local credentials out of Git and review changes before sharing them.
If a credential is exposed, revoke or rotate it promptly; removing it from a
file alone does not invalidate it.

## Reporting a vulnerability

Do not post exploit details, credentials, or personal data in a public issue.
If private vulnerability reporting is enabled, use the repository's
**Security** tab and select **Report a vulnerability**. Include a sanitized
description, affected revision, reproduction steps, and potential impact.

If that option is unavailable, ask the maintainers for a private reporting
channel without including sensitive details in the public request. Reports
are handled on a best-effort basis; no response or remediation timeframe is
guaranteed.
