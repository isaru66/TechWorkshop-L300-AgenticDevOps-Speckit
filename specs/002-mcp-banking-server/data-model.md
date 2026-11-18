# Data Model: MCP Banking Server

No schema change. Existing entities (`Banking.Api/Data/Entities`) are reused.

## Customer
- `Id` (Guid), `Name` (required), `CreatedAt`, optional: `EnglishName`, `Email`, `Phone`, `AddressLine`, `Subdistrict`, `District`, `Province`, `Postcode`, `DateOfBirth`, `Occupation`, `NationalId`.
- **Create**: `Name` required (non-blank); `Id`/`CreatedAt` server-generated.
- **Update**: partial — only supplied fields change; `Name` cannot be blanked.
- **Delete**: allowed only if no accounts exist.

## Account
- `Id`, `CustomerId` (FK Restrict), `Currency` (always `THB`), `Balance` (0–9,999,999,999,999,999.99), `CreatedAt`.
- **Create**: customer must exist; starts at `0.00`.
- **Update**: no mutable fields (balance changes only via deposit/withdraw/transfer). "Update" for accounts is therefore not offered; see contracts.
- **Delete**: allowed only if balance is `0.00` and no transaction entries/transfers reference it.

## Transfer
- `Id`, `SourceAccountId`, `DestinationAccountId`, `Amount`, `Currency`, `CompletedAt`. Created only by `transfer_funds`; immutable.
- Rules (existing): different accounts, positive amount ≤ 2 decimals, THB, sufficient funds, balance limit.

## TransactionEntry
- `Id`, `Sequence`, `AccountId`, `Type` (`DEPOSIT`, `WITHDRAWAL`, `TRANSFER_OUT`, `TRANSFER_IN`), `Amount`, `Currency`, `CompletedAt`, `ResultingBalance`, `TransferId?`, `CounterpartyAccountId?`. Append-only; history returned newest first (existing `HistoryAsync`).

## New service operations (BankingService)
`CreateCustomerAsync`, `UpdateCustomerAsync`, `DeleteCustomerAsync`, `CreateAccountAsync`, `DeleteAccountAsync` — plus new `BankingProblem` code `has_dependents` (409).
