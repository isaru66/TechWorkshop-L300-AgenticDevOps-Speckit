# Banking interface

This client-neutral React/Vite interface browses fictional customers, accounts
and history and submits deposits, withdrawals, and transfers through `/api`.
Money stays in decimal strings; there is no login.

Run `npm.cmd ci` and `npm.cmd run dev` in this directory after starting the
backend. Vite listens on `127.0.0.1:5173` and proxies `/api` to the private
backend. Run `npm.cmd run lint`, `npm.cmd run test -- --run`, and
`npm.cmd run build` for local checks. See the
[quickstart](../../specs/001-local-banking-api/quickstart.md) for PostgreSQL,
the complete setup, Chromium tests, and non-destructive shutdown.
