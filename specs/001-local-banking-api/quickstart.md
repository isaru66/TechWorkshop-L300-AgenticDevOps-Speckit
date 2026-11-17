# Quickstart and Validation Guide

This guide covers the implemented local application, its import, and its verification.

Run PowerShell from the repository root. Filesystem paths below are Windows-style; HTTP paths use normal URL syntax. Use only the supplied fictional data.

## Prerequisites

- .NET SDK 10.0.404, pinned in `global.json`.
- Node.js 22.12+ and npm (tested with Node.js 22.14.0).
- Docker Desktop with Linux containers and Docker Compose v2.
- A modern browser; Playwright installs its own Chromium for end-to-end checks.
- Available loopback ports 8080, 5173, 5432, and optional test port 5433. Resolve conflicts rather than changing the backend's required port.

The project exposes these paths:

```text
BankingWorkshop.slnx
compose.yaml
.config\dotnet-tools.json
src\backend\Banking.Api\Banking.Api.csproj
src\backend\Banking.Api.Tests\Banking.Api.Tests.csproj
src\frontend\package.json
scripts\verify-banking.ps1
```

## 1. Configure and start PostgreSQL

In the setup terminal, choose a local-only password and keep it available for later sessions. It is not an application login.

```powershell
$root = (Get-Location).Path
$env:POSTGRES_PASSWORD = Read-Host 'Local workshop PostgreSQL password'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ConnectionStrings__Banking = "Host=127.0.0.1;Port=5432;Database=banking;Username=banking;Password=$env:POSTGRES_PASSWORD"

docker compose up -d --wait db
docker compose ps
docker compose exec -T db pg_isready -U banking -d banking
```

Expected: `db` is healthy and published only on `127.0.0.1:5432`. Compose must require `POSTGRES_PASSWORD` from the environment, set database/user `banking`, and mount a persistent named volume at `/var/lib/postgresql/data`. Do not commit credentials. Reuse the original password when restarting an existing volume; changing the environment variable does not change an initialized database password.

Store the development connection in the API's configured user-secrets store so a new backend terminal can read it:

```powershell
dotnet user-secrets set 'ConnectionStrings:Banking' $env:ConnectionStrings__Banking --project .\src\backend\Banking.Api\Banking.Api.csproj
```

## 2. Restore, build, and initialize

```powershell
dotnet tool restore
dotnet restore .\BankingWorkshop.slnx --locked-mode
dotnet build .\BankingWorkshop.slnx --no-restore
dotnet ef database update --project .\src\backend\Banking.Api\Banking.Api.csproj --startup-project .\src\backend\Banking.Api\Banking.Api.csproj

$seedPath = Join-Path $root 'src\data\seed'
dotnet run --project .\src\backend\Banking.Api\Banking.Api.csproj --no-launch-profile -- --import-seed --seed-path $seedPath
dotnet run --project .\src\backend\Banking.Api\Banking.Api.csproj --no-launch-profile -- --import-seed --seed-path $seedPath

Push-Location .\src\frontend
npm.cmd ci
npm.cmd run build
Pop-Location
```

On an empty database, the first import inserts 102 customers, 150 accounts, 101 transfers, and 1,002 entries. The second reports zero inserted and those counts skipped; balances/history are unchanged. Import does not run HTTP services or replay historical operations. An already-used development database retains its current balances.

## 3. Run automated checks

The `test` Compose profile provides a separate `test-db`, database/user `banking_test`, loopback port 5433, the same local password, a health check, and temporary data storage.

```powershell
docker compose --profile test up -d --wait test-db
$env:BANKING_TEST_CONNECTION = "Host=127.0.0.1;Port=5433;Database=banking_test;Username=banking_test;Password=$env:POSTGRES_PASSWORD"

dotnet test .\src\backend\Banking.Api.Tests\Banking.Api.Tests.csproj --no-restore
dotnet format .\BankingWorkshop.slnx --verify-no-changes --no-restore

Push-Location .\src\frontend
npm.cmd run lint
npm.cmd run test -- --run
Pop-Location
```

Tests must refuse a connection targeting `banking` or a missing test connection; do not silently skip real-database checks. Each integration fixture owns a uniquely named schema and never resets the development schema/database. Required cases:

- Exact imported fields/references and counts; UTF-8 Thai text; history is not replayed.
- Repeat import before and after new operations; invalid rows, UUID collisions, modified/missing accepted source records, and cross-file conflicts roll back completely.
- Valid deposits/withdrawals, exact-balance spend, zero/negative/precision/range failures.
- Transfer success and insufficient-funds rejection comparing both balances and complete histories.
- Concurrent competing debits cannot overdraw; opposite-direction transfers complete without lock-order deadlocks.
- Failure injected after balance changes but before commit rolls back both accounts, transfer, and entries.
- API money-string contract, ProblemDetails codes, pagination, and documentation routes absent outside Development.
- UI selection, empty/error states, exact money display, stale-response handling, pending submission protection, and both-account refresh.

## 4. Start API and UI

In a new backend terminal at repository root:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --project .\src\backend\Banking.Api\Banking.Api.csproj --no-launch-profile -- --urls http://127.0.0.1:8080
```

In a new frontend terminal:

```powershell
Set-Location .\src\frontend
npm.cmd run dev -- --host 127.0.0.1 --port 5173 --strictPort
```

Expected addresses: API `http://127.0.0.1:8080`, UI `http://127.0.0.1:5173`, SwaggerUI `http://127.0.0.1:8080/swagger`, generated document `http://127.0.0.1:8080/openapi/v1.json`.

Vite must proxy `/api` to port 8080. No wildcard host or CORS settings. If using IDE port forwarding, keep every forwarded port **Private**; no public forwarding or deployment is part of this exercise.

Check readiness from the setup terminal:

```powershell
Invoke-RestMethod 'http://127.0.0.1:8080/health'
$document = Invoke-RestMethod 'http://127.0.0.1:8080/openapi/v1.json'
$document.paths.PSObject.Properties.Name
```

Expected: readiness succeeds and the generated document lists the [REST contract](contracts/rest-api.md), including POST deposits, withdrawals, and transfers. SwaggerUI loads that document and has no authentication requirement.

## 5. Verify REST transfers with complete snapshots

Use the supplied funded UUIDs. The following validation runs change fictional development balances/history. Run without other writers, and fetch every page rather than comparing just the first page.

```powershell
$base = 'http://127.0.0.1:8080/api'
$source = '00209a31-c483-41bb-a9fd-35530f8f680b'
$destination = 'fc99d3e9-f19d-439e-87e4-c7a54b585c7d'

function Get-AllPages([string]$path) {
    $page = 1
    do {
        $result = Invoke-RestMethod "$base/$($path)?page=$page&pageSize=100"
        $result.items
        $page++
    } while (($page - 1) * $result.pageSize -lt $result.totalCount)
}

$beforeSource = Invoke-RestMethod "$base/accounts/$source"
$beforeDestination = Invoke-RestMethod "$base/accounts/$destination"
$beforeSourceHistory = @(Get-AllPages "accounts/$source/transactions")
$beforeDestinationHistory = @(Get-AllPages "accounts/$destination/transactions")
$beforeTransfers = @(Get-AllPages 'transfers')

if ($source -eq $destination -or $beforeSource.currency -ne 'THB' -or
    $beforeDestination.currency -ne 'THB' -or [decimal]$beforeSource.balance -lt [decimal]'100.00') {
    throw 'Select distinct existing THB UUID accounts with sufficient source funds.'
}

$body = @{
    sourceAccountId = $source
    destinationAccountId = $destination
    amount = '100.00'
    currency = 'THB'
} | ConvertTo-Json
$completed = Invoke-RestMethod "$base/transfers" -Method Post -ContentType 'application/json' -Body $body

$afterSource = Invoke-RestMethod "$base/accounts/$source"
$afterDestination = Invoke-RestMethod "$base/accounts/$destination"
$afterSourceHistory = @(Get-AllPages "accounts/$source/transactions")
$afterDestinationHistory = @(Get-AllPages "accounts/$destination/transactions")
$afterTransfers = @(Get-AllPages 'transfers')
```

Success expectations:

- Source is its captured balance minus 100.00; destination is its captured balance plus 100.00; the combined balance is unchanged.
- Each history retains every original entry and field, and gains exactly one appropriate outgoing/incoming entry.
- New entries reference `$completed.transfer.id`, each other account, THB 100.00, the completion instant, and the correct resulting balances.
- Completed transfers retain all original records and gain exactly one.
- On the untouched baseline: balances change from 52,608.86 / 47,043.92 to 52,508.86 / 47,143.92; history counts change from 4 / 6 to 5 / 7.

Now capture fresh rejection snapshots and attempt more than the current source balance:

```powershell
$rejectedSourceBefore = Invoke-RestMethod "$base/accounts/$source"
$rejectedDestinationBefore = Invoke-RestMethod "$base/accounts/$destination"
$rejectedSourceHistoryBefore = @(Get-AllPages "accounts/$source/transactions")
$rejectedDestinationHistoryBefore = @(Get-AllPages "accounts/$destination/transactions")
$rejectedTransfersBefore = @(Get-AllPages 'transfers')

$excess = ([decimal]$rejectedSourceBefore.balance + [decimal]'0.01').ToString('F2', [Globalization.CultureInfo]::InvariantCulture)
$body = @{
    sourceAccountId = $source
    destinationAccountId = $destination
    amount = $excess
    currency = 'THB'
} | ConvertTo-Json

try {
    Invoke-RestMethod "$base/transfers" -Method Post -ContentType 'application/json' -Body $body
    throw 'Expected an insufficient-funds rejection.'
} catch {
    if ($null -eq $_.Exception.Response -or [int]$_.Exception.Response.StatusCode -ne 409) { throw }
    if (($_.ErrorDetails.Message | ConvertFrom-Json).code -ne 'insufficient_funds') { throw }
}

$rejectedSourceAfter = Invoke-RestMethod "$base/accounts/$source"
$rejectedDestinationAfter = Invoke-RestMethod "$base/accounts/$destination"
$rejectedSourceHistoryAfter = @(Get-AllPages "accounts/$source/transactions")
$rejectedDestinationHistoryAfter = @(Get-AllPages "accounts/$destination/transactions")
$rejectedTransfersAfter = @(Get-AllPages 'transfers')
```

Rejection expectations: both account records, both complete histories (all identities and field values), and all completed transfer records equal their corresponding fresh pre-rejection snapshots. The response is 409 with code `insufficient_funds`, with no new completed records.

The implementation must supply an assertion-based runner for these scenarios, not only console output:

```powershell
.\scripts\verify-banking.ps1 -BaseUrl 'http://127.0.0.1:8080/api' -SourceAccountId $source -DestinationAccountId $destination -Amount 100.00
```

The runner fetches all pages, compares exact decimal amounts and complete prior entries, checks reciprocal new transfer entries and transfer records, captures new snapshots before rejection, and exits nonzero on any failure. It accepts actual current balances and does not reset or modify seed files.

## 6. Verify repeat import and restart persistence

Capture all account balances, all histories, and all transfers; rerun the identical import command from section 2; then compare complete snapshots. It must report no newly inserted baseline records, preserve current balances/new history, and not duplicate anything.

Stop and restart the API normally. Also test a PostgreSQL container restart:

```powershell
docker compose restart db
docker compose up -d --wait db
Invoke-RestMethod 'http://127.0.0.1:8080/health'
```

After readiness returns, all prior snapshots must remain equal. Do not invoke resets, recreate schemas, or delete volumes to make a repeatability test pass.

Conflicting-source tests use fixture copies in the isolated integration-test environment, never edits to tracked baseline files or destructive changes to the development database.

## 7. Verify React end to end

Open `http://127.0.0.1:5173` and verify:

1. White background, Azure-inspired blue accents, English labels, no client branding or login, and a fictional-data notice.
2. Select a customer and account; confirm UUID, THB balance, original fictional profile text, and reachable full history against REST.
3. Deposit and withdraw a valid amount; check exact balance changes, one appended entry each, and clear outcomes.
4. Transfer between distinct funded THB accounts; capture both balances/histories first, then confirm both refreshed sides and shared transfer ID.
5. Attempt a transfer exceeding available funds; capture both sides immediately before and confirm neither changed after rejection.
6. Check zero/negative/three-decimal values, empty history, unavailable backend, narrow layout, keyboard focus, and disabled pending submission.

For automated Chromium verification, keep the local services running. Tests use actual current balances, make fictional money operations, and never reset development data:

```powershell
Push-Location .\src\frontend
npx.cmd playwright install chromium
npm.cmd run test:e2e
Pop-Location
```

The `test:e2e` script runs Playwright with `baseURL` `http://127.0.0.1:5173` and proxies REST requests to `http://127.0.0.1:8080/api`; mutation scenarios run serially and assert both-account before/after snapshots.

## 8. Stop without losing data

Stop host API/frontend processes with Ctrl+C in their terminals.

```powershell
docker compose --profile test stop
```

The development named volume remains intact. No volume deletion, database reset, public deployment, authentication setup, or MCP work is required.
