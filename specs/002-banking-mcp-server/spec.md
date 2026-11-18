# Feature Specification: Banking MCP Server

**Feature Branch**: `002-banking-mcp-server`

**Created**: 2026-11-18

**Status**: Draft

**Input**: User description: "Add a Model Context Protocol (MCP) server to the existing banking demo so AI assistants can manage customers, accounts, transfers, and view transaction history. Keep it simple without authentication."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Look up customers, accounts, and history (Priority: P1)

An AI assistant connected to the banking demo answers questions such as "Which accounts does this customer have?", "What is the balance of account X?", and "Show the recent transactions for account X".

**Why this priority**: Read-only access is the safest, most broadly useful capability and proves the assistant can reach the banking data.

**Independent Test**: Connect an assistant, ask it to list customers, open one customer's accounts, check a balance, and list transaction history; all answers match the existing banking data.

**Acceptance Scenarios**:

1. **Given** seeded customers exist, **When** the assistant asks for customers, **Then** it receives the list of customers with their identifying details.
2. **Given** a customer with accounts, **When** the assistant asks for that customer's accounts, **Then** it receives each account with its current balance.
3. **Given** an account with prior activity, **When** the assistant asks for its transactions, **Then** it receives the entries in reverse chronological order.
4. **Given** an unknown customer or account identifier, **When** the assistant asks for it, **Then** it receives a clear "not found" message.

---

### User Story 2 - Manage customers and accounts (Priority: P2)

An assistant creates a customer, updates the customer's details, opens an account for a customer, updates account details, and removes a customer or closes an account when allowed.

**Why this priority**: Enables the "manage" half of the request; depends on lookup to verify results.

**Independent Test**: Create a customer, open an account, update details, then close the account and delete the customer; verify each step through lookups.

**Acceptance Scenarios**:

1. **Given** valid details, **When** the assistant creates a customer, **Then** the customer exists and is retrievable.
2. **Given** an existing customer, **When** the assistant updates their details, **Then** later lookups show the new values.
3. **Given** an existing customer, **When** the assistant opens an account, **Then** the account appears under that customer with a zero or specified opening balance.
4. **Given** an account with a zero balance, **When** the assistant closes it, **Then** it is marked closed, accepts no further deposits, withdrawals, or transfers, and its history remains viewable.
5. **Given** an account with a non-zero balance, **When** the assistant tries to close it, **Then** the request is refused with an explanation.
6. **Given** a customer with no accounts, **When** the assistant deletes them, **Then** the customer no longer appears; **Given** a customer with accounts, **Then** deletion is refused with an explanation.

---

### User Story 3 - Move money (Priority: P3)

An assistant transfers funds between two accounts and records deposits and withdrawals on a single account.

**Why this priority**: State-changing financial operations build on lookup and account management.

**Independent Test**: Deposit into an account, withdraw from it, transfer between two accounts, then confirm balances and history reflect every operation.

**Acceptance Scenarios**:

1. **Given** two open accounts and sufficient funds, **When** the assistant transfers an amount, **Then** the source decreases, the destination increases, and both histories show the entry.
2. **Given** insufficient funds, **When** the assistant requests a transfer or withdrawal, **Then** it is refused, balances are unchanged, and the reason is explained.
3. **Given** an open account, **When** the assistant deposits a valid positive amount, **Then** the balance increases and a history entry is recorded.
4. **Given** an invalid amount (zero, negative) or the same source and destination, **When** the assistant submits it, **Then** it is rejected with an explanation.

---

### Edge Cases

- Missing or malformed inputs (blank names, non-numeric or negative amounts) are rejected with an explanation the assistant can relay.
- Operations on a closed account are refused.
- Simultaneous operations on the same account never produce an incorrect balance.
- Large transaction histories are returned in bounded pages rather than all at once.
- The assistant is unavailable or the banking data cannot be reached: a clear error is returned rather than silent failure.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST expose banking capabilities to AI assistants through a Model Context Protocol server reachable over the network alongside the existing banking application.
- **FR-002**: The system MUST NOT require authentication to use the server (demo only).
- **FR-003**: Assistants MUST be able to list, search, and view customers.
- **FR-004**: Assistants MUST be able to create and update customers, and delete a customer that has no accounts.
- **FR-005**: Assistants MUST be able to list a customer's accounts, view an account and its balance, open an account, and update account details.
- **FR-006**: Assistants MUST be able to close an account only when its balance is zero; closed accounts retain history and reject new money movement.
- **FR-007**: Assistants MUST be able to transfer funds between two open accounts.
- **FR-008**: Assistants MUST be able to deposit to and withdraw from an open account.
- **FR-009**: Assistants MUST be able to view an account's transaction history, newest first, in bounded pages.
- **FR-010**: All money-movement and validation rules already enforced by the existing banking application (e.g., sufficient funds, positive amounts) MUST apply identically to requests from assistants.
- **FR-011**: Every refusal or failure MUST return a clear, human-readable reason; no partial changes may be left behind.
- **FR-012**: Each capability MUST be described so an assistant can discover what it does and what inputs it needs.
- **FR-013**: Changes made through the assistant interface MUST be visible through the existing banking application, and vice versa.

### Key Entities

- **Customer**: A bank client with identifying and contact details; owns zero or more accounts.
- **Account**: A balance-holding account belonging to one customer; has a status (open or closed).
- **Transfer**: Movement of funds between two accounts.
- **Transaction Entry**: A dated history record of a deposit, withdrawal, or transfer leg on an account.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An assistant can complete each primary task (look up a customer's balance, create a customer with an account, transfer funds) in a single conversational request.
- **SC-002**: 100% of the capabilities listed in the requirements are discoverable by a connecting assistant without external documentation.
- **SC-003**: 100% of operations that violate banking rules are refused with an explanation, and no balance is altered by a refused operation.
- **SC-004**: After any assistant-driven change, the same data is visible in the existing application immediately (no manual refresh or sync step).
- **SC-005**: Lookups return results to the assistant in under 2 seconds under demo-scale data.
- **SC-006**: A new workshop participant can connect an assistant and complete a transfer within 10 minutes using the provided instructions.

## Assumptions

- The server is for the local workshop demo only; no authentication, authorization, or rate limiting is provided, and it must not be exposed to untrusted networks.
- The existing banking application and its data are the single source of truth; the new capability reuses its rules rather than duplicating them.
- Customer deletion and account closure rules (no accounts / zero balance; history retained on closed accounts) are reasonable defaults, since the existing application has no update or delete operations today.
- Updatable customer and account details are limited to descriptive fields (e.g., name, contact details, account label); balances change only through deposits, withdrawals, and transfers.
- Single currency, consistent with the existing application.
- Deposits and withdrawals are included in scope in addition to transfers.
- Documentation and examples remain client-neutral so the workshop can be reused with other customers.
