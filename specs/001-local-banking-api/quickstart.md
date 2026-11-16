# Quickstart: Local Banking API

Run everything on a private machine. Nothing here needs login, and no port may be exposed publicly or forwarded as public.

## Prerequisites

- JDK 21 target. Set `JAVA_HOME` in each new PowerShell session, since it does not persist. Only JDK 25 is installed here, so build with it and compile at Java 21 level (`maven.compiler.release=21` in `pom.xml`), or install a JDK 21:

  ```powershell
  $env:JAVA_HOME = "C:\Users\isarar\.jdks\ms-25.0.4.1"
  $env:Path = "$env:JAVA_HOME\bin;$env:Path"
  ```
- Maven 3.9.x
- Node.js 20+ and npm
- Docker Desktop running (local PostgreSQL and integration tests)
- Database settings in your own environment: `DB_URL`, `DB_USER`, `DB_PASSWORD` (see [research.md R8](research.md)). Keep them in your git-ignored `.env` or shell; do not commit them.

## 1. Database

Start your local PostgreSQL container with the loopback-only port binding, using the values you hold privately. Confirm it accepts connections before starting the backend.

## 2. Build and test the backend

From `src/backend`:

```powershell
mvn -B clean verify
```

Expected: all tests pass, including import idempotency, transfer success and rejection, and concurrency tests (Testcontainers needs Docker running).

## 3. Start the backend (port 8080)

```powershell
mvn spring-boot:run
```

Expected: the log reports the seed import result on the first start (102 customers, 150 accounts, 101 transfers, 1002 entries inserted). On a second start it reports 0 inserted and all skipped.

- Swagger UI: http://localhost:8080/swagger-ui.html
- OpenAPI JSON: http://localhost:8080/v3/api-docs (must match [contracts/openapi.yaml](contracts/openapi.yaml))

## 4. Repeatability check

1. Note the counts and a few balances (`GET /api/accounts?size=100`).
2. Deposit into one account, then stop and restart the backend.
3. Confirm the counts are unchanged, the deposit is still present, and that account's balance still includes the deposit (no overwrite, no replay).

## 5. REST verification: transfer before/after comparison

Pick two distinct accounts with the same currency, where the source holds at least the amount you plan to move. For example, take any two accounts from `GET /api/accounts?size=100` with `balance >= 1000.00`.

```powershell
$base = "http://localhost:8080"
$src = "<source-uuid>"; $dst = "<destination-uuid>"
function Snap($id) { @{ account = Invoke-RestMethod "$base/api/accounts/$id/balance"; history = Invoke-RestMethod "$base/api/accounts/$id/transactions?size=100" } }
$before = @{ src = Snap $src; dst = Snap $dst }
```

**Successful transfer** (amount <= source balance):

```powershell
$body = @{ sourceAccountId = $src; destinationAccountId = $dst; amount = "100.00" } | ConvertTo-Json
Invoke-RestMethod -Method Post "$base/api/transfers" -ContentType application/json -Body $body
$after = @{ src = Snap $src; dst = Snap $dst }
```

Expected: source balance down by 100.00, destination up by 100.00, combined total unchanged, each history has exactly one new entry (`TRANSFER_OUT`/`TRANSFER_IN`) with the same `transferId`.

**Rejected transfer** (amount greater than the source balance):

```powershell
$before2 = @{ src = Snap $src; dst = Snap $dst }
$body = @{ sourceAccountId = $src; destinationAccountId = $dst; amount = "999999999.00" } | ConvertTo-Json
try { Invoke-RestMethod -Method Post "$base/api/transfers" -ContentType application/json -Body $body } catch { $_.Exception.Response.StatusCode }
$after2 = @{ src = Snap $src; dst = Snap $dst }
```

Expected: HTTP 422 with code `INSUFFICIENT_FUNDS`; `$before2` and `$after2` are identical for both balances and histories.

Also try: same source and destination (422 `SAME_ACCOUNT`), an unknown UUID (404), a zero or 3-decimal amount (400).

## 6. Frontend

From `src/frontend`:

```powershell
npm install
npm run build
npm test
npm run dev        # http://localhost:5173, proxies /api to port 8080
```

## 7. UI verification

With the backend running, open http://localhost:5173 (Azure-style blue and white theme, no login screen) and confirm:

1. Customer list and a customer's full record, including their accounts and balances.
2. Create a customer, then open an account (0.00 THB).
3. Deposit, then withdraw, from an account; the balance and history update.
4. Run the same successful and insufficient-funds transfers as step 5 from the transfer page; the error message is clear and balances and histories are unchanged after the rejection.
5. History pages newest first, with no repeated entries across pages.

## Teardown

Stop the frontend and backend with Ctrl+C, then stop the database container. Keep all ports private.
