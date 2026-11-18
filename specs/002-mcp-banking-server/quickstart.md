# Quickstart: MCP Banking Server

## Prerequisites
- .NET 10 SDK, PostgreSQL reachable via the `Banking` connection string (see `appsettings.Development.json`).
- VS Code with the REST Client extension.

## Run
```powershell
cd src\backend\Banking.Api
dotnet run
```
API listens on `http://127.0.0.1:8080`; MCP at `http://127.0.0.1:8080/mcp`.

## Validate with REST Client
1. Open `src/backend/Banking.Api/test.http` (draft: [contracts/test.http](contracts/test.http)).
2. Send requests 1–3: initialize → 200, initialized → 202, tools/list → 13 tools.
3. Send 4–9, pasting returned IDs into `@customerId`, `@accountA`, `@accountB`; compare to the `# Expect` comments.
4. Send 10–11 to confirm error results; send 13 to confirm REST is unchanged.

## Validate with an assistant
Add to VS Code `.vscode/mcp.json`:
```json
{ "servers": { "banking": { "type": "http", "url": "http://127.0.0.1:8080/mcp" } } }
```
Ask: "Create a customer, open two accounts, deposit 1000, transfer 250, show history."

## Automated tests
```powershell
dotnet test src\backend\Banking.Api.Tests
```
Tool contract: [contracts/mcp-tools.md](contracts/mcp-tools.md). Data rules: [data-model.md](data-model.md).
