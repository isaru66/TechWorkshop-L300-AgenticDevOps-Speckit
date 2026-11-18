# Feature Specification: MCP Banking Server

**Feature Branch**: `002-mcp-banking-server`

**Created**: 2026-11-18

**Status**: Draft

**Input**: User description: "Add a Model Context Protocol (MCP) server to the existing banking demo so AI assistants can manage customers, accounts, transfers, and view transaction history. Keep it simple without authentication."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manage customers and accounts via an AI assistant (Priority: P1)

An AI assistant connected to the banking demo can create, list, view, update, and delete customers and their accounts on behalf of a user who asks in natural language.

**Why this priority**: Customers and accounts are the foundation; transfers and history depend on them.

**Independent Test**: Connect an MCP-capable assistant, ask it to create a customer with an account, then view, update, and delete them; results match the banking demo's data.

**Acceptance Scenarios**:

1. **Given** the banking demo is running, **When** the assistant requests creation of a customer with valid details, **Then** the customer is created and its details are returned.
2. **Given** an existing customer, **When** the assistant opens an account for them, **Then** the account is created with the customer as owner and a starting balance.
3. **Given** existing customers/accounts, **When** the assistant lists or looks up one by identifier, **Then** current details are returned.
4. **Given** an existing customer or account, **When** the assistant updates or deletes it, **Then** the change is reflected in subsequent lookups.
5. **Given** a nonexistent identifier, **When** the assistant looks up, updates, or deletes it, **Then** a clear "not found" error is returned.

---

### User Story 2 - Transfer funds between accounts (Priority: P2)

The assistant can move money from one account to another and receive a confirmation.

**Why this priority**: The core banking action users want to delegate.

**Independent Test**: Transfer an amount between two accounts and verify both balances change correctly.

**Acceptance Scenarios**:

1. **Given** two accounts and sufficient funds, **When** the assistant requests a transfer, **Then** the source is debited, the destination is credited, and a confirmation is returned.
2. **Given** insufficient funds, a non-positive amount, a same-account transfer, or an unknown account, **When** a transfer is requested, **Then** it is rejected with a clear reason and no balances change.

---

### User Story 3 - View transaction history (Priority: P3)

The assistant can show the transaction history for an account.

**Why this priority**: Read-only insight that supports the other flows.

**Independent Test**: After performing a transfer, request history for either account and see the transaction.

**Acceptance Scenarios**:

1. **Given** an account with transactions, **When** the assistant requests its history, **Then** transactions are returned most recent first with date, type, amount, and counterparty.
2. **Given** an account with no transactions, **When** history is requested, **Then** an empty result is returned.
3. **Given** an unknown account, **When** history is requested, **Then** a "not found" error is returned.

---

### Edge Cases

- Deleting a customer who still has accounts or accounts with a nonzero balance or history.
- Invalid or missing input (blank names, malformed identifiers, negative amounts).
- Concurrent transfers touching the same account.
- Assistant retries the same transfer request.
- Banking demo data unavailable when a request arrives.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST expose the banking capabilities to AI assistants through a Model Context Protocol server, reachable over an HTTP endpoint, as part of the existing banking application.
- **FR-002**: The system MUST let assistants create, list, retrieve, update, and delete customers.
- **FR-003**: The system MUST let assistants create, list, retrieve, update, and delete accounts belonging to customers.
- **FR-004**: The system MUST let assistants transfer funds between two accounts, atomically updating both balances.
- **FR-005**: The system MUST reject invalid transfers (insufficient funds, non-positive amount, same source and destination, unknown account) without changing balances.
- **FR-006**: The system MUST let assistants retrieve the transaction history of an account, newest first.
- **FR-007**: The system MUST apply the same business rules and validation as the existing banking application, and operate on the same data.
- **FR-008**: The system MUST return clear, human-readable errors for validation failures and missing records.
- **FR-009**: The system MUST describe each capability (name, purpose, inputs) so assistants can discover and use them without extra documentation.
- **FR-010**: The system MUST NOT require authentication (demo scope only).

### Key Entities

- **Customer**: A bank client with identifying and contact details; owns zero or more accounts.
- **Account**: A balance-holding account owned by one customer.
- **Transfer**: A movement of funds from a source account to a destination account.
- **Transaction**: A recorded debit or credit on an account, with date, amount, type, and counterparty.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An assistant can discover all customer, account, transfer, and history capabilities with no manual configuration beyond the server address.
- **SC-002**: A user can complete create-customer, open-account, transfer, and view-history in a single assistant conversation in under 3 minutes.
- **SC-003**: 100% of invalid transfer attempts in the test suite are rejected with no balance change.
- **SC-004**: Results obtained through the assistant match those of the existing banking application for the same data in 100% of tested cases.
- **SC-005**: Typical requests return results within 2 seconds.

## Assumptions

- Intended for local demo/workshop use; no authentication or authorization, and not for production data.
- Hosted within the existing banking application, reusing its data and rules; existing functionality is unchanged.
- Delivered over an HTTP endpoint; local stdio is out of scope.
- A single currency; no external payment networks.
- Deleting a customer with accounts follows the existing banking application's rules.
