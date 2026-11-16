# Feature Specification: Local Banking API

**Feature Branch**: `001-local-banking-api`

**Created**: 2026-11-16

**Status**: Draft

**Input**: User description: "Specify a simple fictional local banking API without user authentication, using the existing customers.csv, accounts.csv, transfers.csv, and transaction_entries.csv files under src\data\seed as the baseline. Preserve their UUIDs, THB currency, relationships, existing balances, and transaction histories. Include customers, accounts, balances, deposits, withdrawals, transfers, and history. Require repeatable import without duplicates or replaying history into imported balances. For acceptance checks, choose distinct same-currency UUID accounts with sufficient funds; compare both balances and histories before and after a successful transfer and a rejected insufficient-funds transfer. Keep the interface client-neutral and services and forwarded ports private; do not use real financial data or add login or public deployment."

## Clarifications

### Session 2026-11-16

- Q: How much of a customer record does the interface return? → A: The full record, including the fictional DEMO contact fields and national identifier.
- Q: Can customers and accounts be created through the interface? → A: Yes. Both can be created and read.
- Q: What does a repeat import do, and how is history returned? → A: Insert-only. Records whose UUID already exists are skipped. History is paginated, newest first.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Load the fictional baseline repeatably (Priority: P1)

A workshop participant starts the local banking service with an empty store and loads the fictional seed data (customers, accounts, transfers, transaction entries). They can run the load again at any time without creating duplicates or changing any balance.

**Why this priority**: Every other capability depends on the baseline data being present and trustworthy.

**Independent Test**: Run the import on an empty store, record counts and balances, run it again, and confirm counts, balances and histories are identical.

**Acceptance Scenarios**:

1. **Given** an empty store, **When** the import runs, **Then** 102 customers, 150 accounts, 101 transfers and 1002 transaction entries exist with their original UUIDs, THB currency, relationships, timestamps and balances.
2. **Given** the import already completed, **When** it runs again, **Then** no records are added or changed, and the run reports that existing records were skipped.
3. **Given** the import has completed, **When** any account is inspected, **Then** its balance equals the seed balance, not a total recomputed by replaying the history.
4. **Given** seed data containing a duplicate ID or a reference that does not resolve, **When** the import runs, **Then** it is rejected with a clear message and no partial data is kept.

---

### User Story 2 - Browse customers, accounts and balances (Priority: P1)

A client application reads customers (full fictional record) and accounts, and looks up the current balance of any account by its UUID.

**Why this priority**: Reading is needed to choose accounts for every money movement and to verify results.

**Independent Test**: After import, fetch a customer, list that customer's accounts, and fetch one account's balance. Compare with the seed files.

**Acceptance Scenarios**:

1. **Given** imported data, **When** a customer is requested by UUID, **Then** the full fictional record is returned, including the DEMO contact fields and national identifier.
2. **Given** a customer with accounts, **When** their accounts are requested, **Then** every account linked to that customer is returned with currency and balance.
3. **Given** an unknown UUID, **When** it is requested, **Then** a clear "not found" response is returned.

---

### User Story 3 - Create customers and accounts (Priority: P2)

A client creates a new fictional customer and opens a new THB account for that customer, with an opening balance of zero.

**Why this priority**: Lets participants extend the baseline without editing seed files.

**Independent Test**: Create a customer, open an account, then read both back and confirm the account starts at 0.00 THB with an empty history.

**Acceptance Scenarios**:

1. **Given** valid customer details, **When** the customer is created, **Then** a new UUID is assigned and the customer can be read back.
2. **Given** an existing customer, **When** an account is opened, **Then** it is created in THB with balance 0.00 and linked to that customer.
3. **Given** a non-existent customer UUID, **When** an account is opened, **Then** the request is rejected and nothing is created.
4. **Given** missing or invalid customer details, **When** creation is attempted, **Then** the request is rejected with the specific problem identified.

---

### User Story 4 - Deposit and withdraw (Priority: P2)

A client deposits money into, or withdraws money from, an account. Each successful operation changes the balance and appends a history entry.

**Why this priority**: These are the basic single-account money movements.

**Independent Test**: Deposit, then withdraw, on one account. Confirm the balance moves by the exact amounts and two new history entries appear.

**Acceptance Scenarios**:

1. **Given** an account, **When** a positive THB amount is deposited, **Then** the balance rises by that amount and a deposit entry records the amount and resulting balance.
2. **Given** an account with sufficient funds, **When** a withdrawal is made, **Then** the balance falls by that amount and a withdrawal entry is recorded.
3. **Given** an account with insufficient funds, **When** a withdrawal is requested, **Then** it is rejected and neither balance nor history changes.
4. **Given** a zero, negative or malformed amount, **When** a deposit or withdrawal is requested, **Then** it is rejected and nothing changes.

---

### User Story 5 - Transfer between accounts (Priority: P1)

A client transfers money from one account to a different account in the same currency. The transfer is all-or-nothing and appears in the history of both accounts.

**Why this priority**: Transfers are the core banking behaviour and carry the highest consistency risk.

**Independent Test**: Choose two distinct same-currency accounts (by UUID), the source having sufficient funds. Capture both balances and both histories. Perform the transfer and compare. Then attempt a transfer larger than the source balance and compare again.

**Acceptance Scenarios**:

1. **Given** distinct same-currency accounts A (sufficient funds) and B, **When** an amount is transferred from A to B, **Then** A decreases and B increases by exactly that amount, the total across both is unchanged, and each history gains one new entry linked to the same transfer.
2. **Given** account A with insufficient funds, **When** a transfer is requested, **Then** it is rejected with an insufficient-funds message, and the balances and histories of both accounts are identical to before the attempt.
3. **Given** the same account as source and destination, **When** a transfer is requested, **Then** it is rejected and nothing changes.
4. **Given** accounts in different currencies, **When** a transfer is requested, **Then** it is rejected and nothing changes.
5. **Given** an unknown source or destination UUID, **When** a transfer is requested, **Then** it is rejected and nothing changes.

---

### User Story 6 - View transaction history (Priority: P2)

A client views an account's history of deposits, withdrawals and transfers, newest first, in pages.

**Why this priority**: History is how results of every other operation are verified.

**Independent Test**: Request the first page and the next page of a seeded account's history and confirm order, completeness and no overlap.

**Acceptance Scenarios**:

1. **Given** an account with history, **When** history is requested, **Then** entries are returned newest first with type, amount, currency, completion time, resulting balance, and any related transfer and counterparty account.
2. **Given** more entries than one page holds, **When** successive pages are requested, **Then** every entry appears exactly once across pages.
3. **Given** an account with no history, **When** history is requested, **Then** an empty result is returned, not an error.

---

### Edge Cases

- Two operations on the same account happen at the same time: balances must never go negative or lose an update.
- An amount has more than two decimal places or is not a number: it is rejected.
- A withdrawal or transfer equals the exact balance: it succeeds and leaves 0.00.
- Import is interrupted partway: the next run completes the data without duplicates.
- A seed row exists with the same UUID but different values: it is skipped, not overwritten.
- The page size requested is zero, negative or too large: it is rejected or capped with a clear message.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST import customers, accounts, transfers and transaction entries from the four seed files in dependency order, preserving UUIDs, THB currency, relationships, timestamps, balances and histories.
- **FR-002**: The import MUST be insert-only and repeatable: records whose UUID already exists are skipped, and no duplicates are created.
- **FR-003**: The import MUST NOT replay historical transfers or entries against imported balances; imported balances equal the seed balances.
- **FR-004**: The import MUST validate unique IDs and that every populated reference resolves, and MUST reject invalid data without keeping partial results.
- **FR-005**: The system MUST allow customers to be created and read, returning the full record including the fictional DEMO contact fields and national identifier.
- **FR-006**: The system MUST allow accounts to be opened for an existing customer (THB, opening balance 0.00), read, and listed per customer.
- **FR-007**: The system MUST report the current balance of any account by UUID.
- **FR-008**: The system MUST support deposits and withdrawals of positive THB amounts with at most two decimal places, recording a history entry with the resulting balance.
- **FR-009**: The system MUST reject withdrawals and transfers that exceed available funds, and no account may ever have a negative balance.
- **FR-010**: The system MUST support transfers between two distinct accounts of the same currency that are atomic: both sides apply or neither does.
- **FR-011**: A successful transfer MUST record one transfer and a history entry for each account, linked to that transfer and to each other as counterparty.
- **FR-012**: A rejected operation MUST leave all balances and histories unchanged.
- **FR-013**: The system MUST return history per account, newest first, in pages, in which every entry appears exactly once.
- **FR-014**: The system MUST return clear, specific errors for invalid input, unknown UUIDs and business-rule rejections.
- **FR-015**: The interface MUST be client-neutral: it must not assume any particular consuming application, brand or customer.
- **FR-016**: The system MUST NOT require login, and MUST NOT include authentication or authorisation features.
- **FR-017**: The service and any forwarded ports MUST be reachable only from the local machine or a private trusted environment, and the feature MUST NOT include public deployment.
- **FR-018**: Only fictional data may be used; no real personal or financial data may be loaded, created or documented.

### Key Entities

- **Customer**: A fictional person identified by UUID, with name, English name, contact details, address, date of birth, occupation and a fictional national identifier. Owns zero or more accounts.
- **Account**: Identified by UUID, belongs to one customer, holds a currency (THB) and a current balance, and has a creation time.
- **Transfer**: A completed movement of an amount between two distinct accounts of the same currency, with a completion time.
- **Transaction Entry**: One line in an account's history: type (deposit, withdrawal or the transfer sides), amount, currency, completion time, resulting balance, and optional transfer and counterparty account references.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After the first import, record counts match the seed files exactly (102 / 150 / 101 / 1002), and a second import adds or changes zero records.
- **SC-002**: 100% of account balances after import equal the seed balances.
- **SC-003**: For a successful transfer between two distinct same-currency accounts with sufficient funds, the source falls and the destination rises by exactly the amount, the combined total is unchanged, and each history gains exactly one entry.
- **SC-004**: For an insufficient-funds transfer, 100% of before/after comparisons of both balances and both histories are identical.
- **SC-005**: No sequence of deposits, withdrawals and transfers, including concurrent ones, produces a negative balance or a mismatch between balance and latest history entry.
- **SC-006**: A participant can run the import and complete a full deposit, withdrawal and transfer check in under 10 minutes using only the documentation.
- **SC-007**: The service cannot be reached from outside the local or private environment, and no operation requires credentials.

## Assumptions

- This is a workshop and demonstration system for a single private, trusted developer environment, not a production bank.
- Seed files are the baseline under `src\data\seed`; they are treated as read-only inputs.
- THB is the only supported currency; amounts use two decimal places.
- Customers are not deleted, and accounts are not closed or deleted, in this version.
- Seed customer contact data is fictional (DEMO values) and is returned in full, as clarified; nothing here is real.
- Concrete runtime and storage settings are decided at the planning stage and are intentionally not recorded in this specification.
- The seed README's import order (customers, accounts, transfers, entries) is followed.
