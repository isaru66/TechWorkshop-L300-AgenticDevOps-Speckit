# Local Banking frontend

Vite + React + TypeScript UI for the fictional, local-only Local Banking API.
There is no login. Use fictional data only and keep the environment private;
see the [root README](../../README.md).

Money values are decimal strings end to end and are never converted to floats.

## Commands

Run from `src/frontend` (Node.js 20+):

```powershell
npm install        # install dependencies
npm run dev        # dev server at http://localhost:5173
npm run build      # type-check and production build into dist/
npm test           # run the Vitest suite once (API is mocked)
```

The dev server proxies `/api` to the backend at `http://localhost:8080`, so
start the backend first (see [`../backend/README.md`](../backend/README.md)).
