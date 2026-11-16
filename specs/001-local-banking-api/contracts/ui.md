# UI Contract: Fictional Banking Workshop

## Visual direction

User-confirmed theme: Azure-inspired blue with a white background. Use blue accents (`#0078D4` decorative accent, `#005A9E` primary button), white surfaces, pale-blue selected states, dark slate text, and restrained borders. Use green/red plus explicit text for success/rejection; never color alone. Verify text contrast and keyboard focus during implementation.

Title: **Fictional Banking Workshop**. Include a short fictional-data notice. No customer names in branding, customer logos, Microsoft/Azure logos, login controls, or implication of a real bank. Seeded fictional names remain visible as data.

## Main dashboard

1. Customer browser: paginated customer list, English name when available, original name retained; select a customer to load accounts.
2. Account browser: UUID, currency, and current balance; clearly highlight the selected account.
3. Selected account panel: exact two-decimal balance with explicit `THB`, customer association, and full UUID.
4. Money forms: deposit and withdrawal against the selected account; transfer uses an explicitly labeled source and destination account. Destination can be any existing account, including another customer's account.
5. Account history: sequence, type, amount, THB currency, completion time with timezone indication, resulting balance, and transfer/counterparty references when present. Provide pagination that allows reaching every entry.
6. Transfer result: show shared transfer UUID, source and destination balances, and access to both refreshed account histories.

Desktop may use a customer/account column and a wider balance/action/history panel. Narrow layouts stack panels without hiding required controls or preventing access to UUIDs/history.

## Interaction rules

- Use English labels. Format decimal strings without conversion to binary floating point; show exactly two fractional digits and THB. Enter amounts using a labeled text field with decimal input mode.
- Preserve Thai customer text and ISO-like UUIDs correctly. Do not transliterate or rewrite imported data.
- All data comes from `/api` REST requests; there is no UI-owned balance or mocked transaction history.
- Selecting an account loads balance and history. Ignore stale in-flight responses from a previously selected account.
- Destination account selection excludes the source in normal use; the service remains authoritative for same-account validation.
- While a mutation is pending, disable all mutation submission controls that could resubmit it. A click represents one request; no automatic retry on timeout or network failure.
- After success, refresh the affected account balance/history; transfers refresh or invalidate cached data for both accounts. Retain the completed result even if the subsequent read fails and show a separate refresh error.
- After rejection, show the stable problem code and readable explanation; refresh affected data without optimistic balance changes.
- On an ambiguous network outcome, explain that completion is unknown and offer a read-only refresh, not an automatic replay.
- Loading, empty history, no accounts, service unavailable, validation failure, and refresh failure have explicit visible states.
- Keyboard navigation, programmatic labels, visible focus, and an announcement region for operation outcomes are required.

## Interface verification

Use the two funded UUID fixtures and snapshot procedure in `..\quickstart.md`. Verify success and insufficient-funds rejection through the UI and then compare server values. Verify both histories, not just displayed totals. Confirm unchanged history remains reachable across pagination.

The UI runs on loopback port 5173, proxies `/api` to loopback port 8080, and contains no sign-in page, public links for access, or deployment action. Forwarded ports remain private.
