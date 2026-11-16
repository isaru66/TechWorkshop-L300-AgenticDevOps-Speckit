# Feature Specification: Fictional Local Banking API and UI

**Feature Branch**: `workshop/dotnet`

**Created**: 2026-11-16

**Status**: Draft

**Input**: User description: "Specify a simple fictional local banking API without user authentication, using the existing customers.csv, accounts.csv, transfers.csv, and transaction_entries.csv files under src\data\seed as the baseline. Preserve their UUIDs, THB currency, relationships, existing balances, and transaction histories. Include customers, accounts, balances, deposits, withdrawals, transfers, and history. Require repeatable import without duplicates or replaying history into imported balances. For acceptance checks, choose distinct same-currency UUID accounts with sufficient funds; compare both balances and histories before and after a successful transfer and a rejected insufficient-funds transfer. Keep the interface client-neutral and services and forwarded ports private; do not use real financial data or add login or public deployment. Ask questions for clarification and wait for my answers before writing."

**Clarified Scope**: Customer and account records are read-only; include a minimal client-neutral banking web UI alongside the API. Reimport preserves current balances and new history, skips unchanged seed records, and rejects conflicting seed data without partial changes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Explore the Fictional Banking Baseline (Priority: P1)

A workshop participant initializes the fictional baseline and browses customers, their accounts, current balances, and existing history without signing in.

**Why this priority**: Every demonstration depends on trustworthy baseline data and understandable account selection.

**Independent Test**: Initialize an empty local environment and browse the imported customer, account, and history records; compare them with all four source files.

**Acceptance Scenarios**:

1. **Given** an empty local environment and the supplied baseline, **When** initialization completes, **Then** there are exactly 102 customers, 150 accounts, 101 historical transfers, and 1,002 historical entries, preserving every supplied UUID, relationship, amount, balance, currency, timestamp, and historical sequence.
2. **Given** an initialized environment, **When** a participant selects a customer and one of that customer's accounts, **Then** both the API and web UI provide the correct account UUID, customer association, current THB balance, and complete retrievable history in deterministic recorded order.
3. **Given** an account with imported history, **When** the participant reads its balance, **Then** it equals the balance supplied in `accounts.csv`, not that balance plus replayed historical transactions.
4. **Given** existing records, **When** the participant uses the available customer and account views, **Then** they can browse records but cannot create, edit, delete, or close customers or accounts.

---

### User Story 2 - Deposit and Withdraw Fictional Funds (Priority: P2)

A participant deposits or withdraws fictional THB funds and immediately sees the resulting balance and history.

**Why this priority**: These operations demonstrate single-account balance changes and rejection behavior.

**Independent Test**: Capture one account's balance and history, perform a deposit and a valid withdrawal, and compare the resulting balance and appended entries after each action.

**Acceptance Scenarios**:

1. **Given** an existing account with balance B and history H, **When** a valid deposit of amount A succeeds, **Then** its balance becomes B + A and its history retains H unchanged and adds exactly one deposit entry for A with the resulting balance.
2. **Given** an account with sufficient funds, **When** a valid withdrawal of A succeeds, **Then** its balance becomes B - A and exactly one withdrawal entry is appended with that resulting balance.
3. **Given** a withdrawal exceeding the available balance, **When** it is submitted, **Then** it is rejected with a clear insufficient-funds outcome and the account balance and history remain exactly unchanged.
4. **Given** either operation, **When** the participant uses the web UI, **Then** the UI identifies the selected account and amount, displays success or a meaningful rejection, and refreshes the balance and history to reflect the actual outcome.

---

### User Story 3 - Transfer Between Existing Accounts (Priority: P2)

A participant transfers fictional funds between two distinct THB accounts and verifies both sides of the movement.

**Why this priority**: Transfers demonstrate coordinated changes without lost funds or partial history.

**Independent Test**: Use the two seeded accounts below in a fresh baseline, capture both balances and complete histories, run the successful transfer, then capture new snapshots and run the rejected transfer.

**Acceptance Scenarios**:

1. **Given** source `00209a31-c483-41bb-a9fd-35530f8f680b` with THB 52,608.86 and 4 history entries, and destination `fc99d3e9-f19d-439e-87e4-c7a54b585c7d` with THB 47,043.92 and 6 history entries, **When** a transfer of THB 100.00 succeeds, **Then** balances become THB 52,508.86 and THB 47,143.92 respectively, their combined balance remains THB 99,652.78, and their history counts become 5 and 7. Both original histories remain unchanged, and each account gains exactly one corresponding outgoing or incoming entry sharing the new transfer UUID and identifying the other account, amount, currency, completion time, and resulting balance. Exactly one transfer record is added.
2. **Given** both accounts' balances, complete histories, and transfer records captured immediately after scenario 1, **When** the participant attempts to transfer THB 52,508.87 from the same source to the same destination, **Then** the transfer is rejected for insufficient funds; both balances, both complete histories, and all transfer records remain exactly equal to those snapshots.
3. **Given** any current environment rather than a fresh baseline, **When** the transfer checks run, **Then** they first verify the two account UUIDs exist, are distinct and THB-denominated, and that the source has at least THB 100.00. They capture actual starting balances and complete histories, expect a successful transfer to change balances by exactly minus/plus THB 100.00 and append exactly one linked entry per side, then attempt an amount THB 0.01 greater than the source's new balance and verify exact non-mutation against fresh snapshots. Fixed baseline balances are not assumed in a modified environment.
4. **Given** valid source and destination selections in the web UI, **When** a transfer succeeds or fails, **Then** the participant receives a clear outcome and can inspect the refreshed balances and histories of both accounts.

---

### User Story 4 - Repeat Initialization Without Losing Workshop Activity (Priority: P2)

A participant can initialize the same baseline repeatedly, including after performing new transactions, without duplicating history or resetting balances.

**Why this priority**: Workshop setup must be repeatable without corrupting demonstrations already in progress.

**Independent Test**: Import twice, perform new money operations, import the identical files again, and compare all stored records before and after each repeat import.

**Acceptance Scenarios**:

1. **Given** a successful baseline import, **When** the identical files are imported again, **Then** no duplicate records appear and every balance and history remains unchanged.
2. **Given** completed new deposits, withdrawals, or transfers, **When** the identical baseline is imported again, **Then** current balances, new transfer records, new history entries, and imported records remain unchanged; legitimate balance changes are not treated as source-file conflicts.
3. **Given** a previously imported seed UUID whose source attributes have subsequently been changed in the import input, **When** that input is imported, **Then** the import is rejected with the conflicting record identified and no records or balances are partially changed. Conflict comparison uses the originally accepted seed values, not balances legitimately updated by later operations.
4. **Given** missing required files, invalid values, duplicate UUIDs within an entity, duplicate historical sequence values, or unresolved populated references, **When** import is attempted, **Then** it reports the invalid records and leaves the existing environment unchanged.

### Edge Cases

- Zero, negative, nonnumeric, missing, or more-than-two-decimal-place amounts are rejected without balance or history changes.
- Malformed UUIDs and unknown accounts produce a clear invalid-input or not-found outcome without mutation.
- A transfer to the same account or using mismatched currencies is rejected without changing either account; currency conversion is out of scope.
- Withdrawal or transfer of exactly the available balance succeeds and leaves the source at zero; overdrafts are not permitted.
- If an operation cannot complete, no partial balance change, transfer record, or transaction entry remains.
- Concurrent withdrawals or transfers cannot jointly spend more than the source balance; accepted operations have consistent resulting balances and ordered histories.
- Accounts without entries show an empty history, not an error. When history spans multiple views, all entries remain retrievable without omission or duplication.
- Optional empty transfer and counterparty references in historical entries remain valid; populated references must resolve.
- Historical timestamps and resulting balances are preserved rather than rewritten to match current state.
- A rejected operation is not recorded as a completed transaction or transfer.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a fictional local banking API and a minimal client-neutral banking web UI without application authentication or login.
- **FR-002**: The system MUST allow listing and reading existing customers, their associated accounts, account balances, account histories, and details of completed transfers. Customer/account creation, editing, deletion, and closure MUST NOT be offered.
- **FR-003**: The system MUST initialize from `customers.csv`, `accounts.csv`, `transfers.csv`, and `transaction_entries.csv` under `src\data\seed`, preserving all supplied field values, UUIDs, THB currency, relationships, balances, timestamps, and historical sequence values.
- **FR-004**: Imported account balances MUST be treated as the current baseline. Imported transfers and entries MUST be stored as history only, never executed or replayed against those balances.
- **FR-005**: Initialization MUST validate the full input before applying changes: required fields, unique entity UUIDs, unique historical sequences, valid values, and all populated customer, account, transfer, and counterparty references. Invalid input MUST cause a clear error with no partial changes.
- **FR-006**: Reimport of identical baseline records MUST be a no-op for existing records, preserve later activity and current balances, and create no duplicate records. Source conflicts against previously accepted seed values MUST reject the entire import without mutation; later legitimate balance changes MUST NOT be classified as source conflicts.
- **FR-007**: A successful deposit MUST increase exactly one account balance by the accepted amount and append exactly one completed deposit entry containing a new UUID, account reference, amount, currency, completion time, historical order, and resulting balance.
- **FR-008**: A successful withdrawal MUST reduce exactly one account balance by the accepted amount and append exactly one completed withdrawal entry. Insufficient funds MUST cause rejection without mutation.
- **FR-009**: A successful transfer MUST use distinct existing same-currency accounts, decrease the source and increase the destination by the same amount, create exactly one new completed transfer record, and append exactly one linked outgoing/incoming history entry to each side. The two entries MUST share the transfer reference and identify their respective counterparties and resulting balances.
- **FR-010**: Money operations MUST accept only positive THB amounts with at most two decimal places, preserve exact two-decimal monetary values without rounding drift, and prevent negative balances.
- **FR-011**: Each accepted money operation MUST apply all its related balance and history changes together, including under concurrent requests. Rejected or interrupted operations MUST leave no partial changes.
- **FR-012**: History MUST preserve imported entries unchanged and include all subsequent completed operations in deterministic recorded order, with distinguishable types, amounts, currency, completion times, and resulting balances. New entries MUST have unique UUIDs and sequence values that follow existing history.
- **FR-013**: The web UI MUST support browsing/selecting customers and accounts, viewing balances and history, submitting deposits and withdrawals, selecting source and destination accounts for transfers, displaying clear outcomes, and refreshing affected account information after each outcome.
- **FR-014**: Invalid input, unknown records, same-account transfers, currency mismatches, and insufficient funds MUST produce distinguishable, understandable outcomes in the API and UI without unintended mutation.
- **FR-015**: Services and any forwarded ports MUST remain private to the trusted local development/workshop environment. The feature MUST NOT introduce public exposure, public deployment, real personal or financial data, credentials, login, or customer-specific branding.
- **FR-016**: Imported data and completed workshop activity MUST survive normal service restarts; restarting or repeating initialization MUST NOT reset balances or lose histories.

### Key Entities *(include if feature involves data)*

- **Customer**: An existing fictional customer identified by UUID, with the supplied fictional profile fields and creation timestamp; may have multiple accounts.
- **Account**: An existing UUID-identified account belonging to one customer, with creation timestamp, currency, and current balance.
- **Transfer**: A completed movement identified by UUID between a source and destination account, with amount, currency, and completion timestamp; linked to the two corresponding history entries.
- **Transaction Entry**: A UUID-identified historical account movement with recorded sequence, type, amount, currency, completion timestamp, resulting balance, and optional transfer and counterparty references.
- **Accepted Seed Baseline**: The originally accepted source values used to distinguish unchanged reimports from conflicting seed input without mistaking later legitimate activity for conflicts.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Initial import reproduces 100% of the supplied 102 customers, 150 accounts, 101 transfers, and 1,002 entries with no lost or altered values, unresolved references, duplicated records, or historical replay.
- **SC-002**: Repeating import at least twice, including once after new deposits, withdrawals, and transfers, changes zero preexisting records, current balances, or history entries; a conflicting or invalid import applies zero changes.
- **SC-003**: Every accepted deposit and withdrawal changes the selected account by exactly the submitted amount and adds exactly one entry; every accepted transfer preserves the two-account combined balance and adds exactly one transfer and two linked entries.
- **SC-004**: Every rejected money-operation acceptance case leaves all affected balances and complete histories unchanged. Both the successful and insufficient-funds transfer checks compare both accounts' before-and-after balances and complete histories.
- **SC-005**: A workshop participant can use the web UI to find a seeded account, inspect its balance and history, complete a valid operation, and understand a rejected operation without signing in or editing seed files.
- **SC-006**: All services and forwarded ports used for the workshop remain private, with zero login screens, public deployment steps, real-data requirements, or customer-specific branding.
- **SC-007**: After a normal service restart, 100% of imported records and successfully completed workshop operations remain available with unchanged balances and histories.

## Assumptions

- The user confirmed read-only customer/account management, inclusion of a minimal client-neutral web UI, and non-destructive reimport preserving later activity.
- The four repository seed files are the authoritative fictional baseline. Counts and fixed fixture balances above describe the files inspected on 2026-11-16; deliberate future baseline changes require corresponding acceptance-fixture updates.
- The audience is workshop participants in a trusted private development environment, not real banking customers. Authentication and public deployment are explicitly excluded.
- Existing seed profiles may contain fictional Thai text; it must remain intact. Client neutrality concerns branding and customer affiliation, not removal of the requested THB baseline or fictional profile data.
- New operations use THB only. Foreign-exchange conversion, fees, interest, external payments, scheduled transactions, reversals, profile maintenance, and account lifecycle management are out of scope.
- New deposit, withdrawal, and transfer submissions represent separate requests. Retry deduplication for money-operation requests is not included; repeatable import is a separate requirement. The UI must prevent duplicate submission while an operation is pending.
- Complete history comparisons include existing entry identities and field values, not merely counts or totals.
- The project constitution currently contains template placeholders and supplies no ratified additional principles.
