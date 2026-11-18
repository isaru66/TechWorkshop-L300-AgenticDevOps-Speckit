package com.workshop.banking.mcp;

import com.workshop.banking.api.dto.ApiDtos.AccountResponse;
import com.workshop.banking.api.dto.ApiDtos.BalanceResponse;
import com.workshop.banking.api.dto.ApiDtos.CustomerInput;
import com.workshop.banking.api.dto.ApiDtos.CustomerResponse;
import com.workshop.banking.api.dto.ApiDtos.PageResponse;
import com.workshop.banking.api.dto.ApiDtos.TransactionEntryResponse;
import com.workshop.banking.api.dto.ApiDtos.TransferResponse;
import com.workshop.banking.service.BankingException;
import com.workshop.banking.service.BankingService;
import io.modelcontextprotocol.spec.McpSchema.CallToolResult;
import java.time.LocalDate;
import java.time.format.DateTimeParseException;
import java.util.Map;
import java.util.UUID;
import java.util.function.Supplier;
import org.springframework.ai.mcp.annotation.McpTool;
import org.springframework.ai.mcp.annotation.McpTool.McpAnnotations;
import org.springframework.ai.mcp.annotation.McpToolParam;
import org.springframework.stereotype.Component;
import tools.jackson.databind.json.JsonMapper;

/** MCP tools; thin adapters that delegate every rule to {@link BankingService}. */
@Component
public class BankingMcpTools {

    private static final int DEFAULT_SIZE = 20;

    private final BankingService service;
    private final JsonMapper json;

    public BankingMcpTools(BankingService service, JsonMapper json) {
        this.service = service;
        this.json = json;
    }

    // ---- customers ---------------------------------------------------------

    @McpTool(name = "list_customers", description = "List customers, paged (page starts at 0, size 1-100).",
            annotations = @McpAnnotations(readOnlyHint = true))
    public CallToolResult listCustomers(
            @McpToolParam(description = "Page number, default 0", required = false) Integer page,
            @McpToolParam(description = "Page size 1-100, default 20", required = false) Integer size) {
        return run(() -> PageResponse.of(service.listCustomers(p(page), s(size)), CustomerResponse::from));
    }

    @McpTool(name = "search_customers",
            description = "Search customers by name, English name, email or phone (case-insensitive, partial match).",
            annotations = @McpAnnotations(readOnlyHint = true))
    public CallToolResult searchCustomers(
            @McpToolParam(description = "Text to search for") String query,
            @McpToolParam(description = "Page number, default 0", required = false) Integer page,
            @McpToolParam(description = "Page size 1-100, default 20", required = false) Integer size) {
        return run(() -> PageResponse.of(service.searchCustomers(query, p(page), s(size)), CustomerResponse::from));
    }

    @McpTool(name = "get_customer", description = "Get one customer by id.",
            annotations = @McpAnnotations(readOnlyHint = true))
    public CallToolResult getCustomer(@McpToolParam(description = "Customer UUID") String customerId) {
        return run(() -> CustomerResponse.from(service.getCustomer(uuid(customerId, "customerId"))));
    }

    @McpTool(name = "create_customer", description = "Create a customer. Only name is required.")
    public CallToolResult createCustomer(
            @McpToolParam(description = "Full name (required)") String name,
            @McpToolParam(description = "English name", required = false) String englishName,
            @McpToolParam(description = "Email address", required = false) String email,
            @McpToolParam(description = "Phone number", required = false) String phone,
            @McpToolParam(description = "Address line", required = false) String addressLine,
            @McpToolParam(description = "Subdistrict", required = false) String subdistrict,
            @McpToolParam(description = "District", required = false) String district,
            @McpToolParam(description = "Province", required = false) String province,
            @McpToolParam(description = "Postcode", required = false) String postcode,
            @McpToolParam(description = "Date of birth, ISO format yyyy-MM-dd, in the past", required = false)
            String dateOfBirth,
            @McpToolParam(description = "Occupation", required = false) String occupation,
            @McpToolParam(description = "National id (fictional values only)", required = false) String nationalId) {
        return run(() -> CustomerResponse.from(service.createCustomer(input(name, englishName, email, phone,
                addressLine, subdistrict, district, province, postcode, dateOfBirth, occupation, nationalId))));
    }

    @McpTool(name = "update_customer",
            description = "Replace a customer's descriptive fields. Fields left out are cleared; name is required.")
    public CallToolResult updateCustomer(
            @McpToolParam(description = "Customer UUID") String customerId,
            @McpToolParam(description = "Full name (required)") String name,
            @McpToolParam(description = "English name", required = false) String englishName,
            @McpToolParam(description = "Email address", required = false) String email,
            @McpToolParam(description = "Phone number", required = false) String phone,
            @McpToolParam(description = "Address line", required = false) String addressLine,
            @McpToolParam(description = "Subdistrict", required = false) String subdistrict,
            @McpToolParam(description = "District", required = false) String district,
            @McpToolParam(description = "Province", required = false) String province,
            @McpToolParam(description = "Postcode", required = false) String postcode,
            @McpToolParam(description = "Date of birth, ISO format yyyy-MM-dd, in the past", required = false)
            String dateOfBirth,
            @McpToolParam(description = "Occupation", required = false) String occupation,
            @McpToolParam(description = "National id (fictional values only)", required = false) String nationalId) {
        return run(() -> CustomerResponse.from(service.updateCustomer(uuid(customerId, "customerId"),
                input(name, englishName, email, phone, addressLine, subdistrict, district, province, postcode,
                        dateOfBirth, occupation, nationalId))));
    }

    @McpTool(name = "delete_customer",
            description = "Permanently delete a customer. Refused with CUSTOMER_HAS_ACCOUNTS if the customer has any "
                    + "account (open or closed).",
            annotations = @McpAnnotations(destructiveHint = true))
    public CallToolResult deleteCustomer(@McpToolParam(description = "Customer UUID") String customerId) {
        return run(() -> {
            UUID id = uuid(customerId, "customerId");
            service.deleteCustomer(id);
            return Map.of("deleted", true, "customerId", id);
        });
    }

    // ---- accounts ----------------------------------------------------------

    @McpTool(name = "list_customer_accounts", description = "List a customer's accounts with balance and status.",
            annotations = @McpAnnotations(readOnlyHint = true))
    public CallToolResult listCustomerAccounts(@McpToolParam(description = "Customer UUID") String customerId) {
        return run(() -> service.listCustomerAccounts(uuid(customerId, "customerId")).stream()
                .map(AccountResponse::from).toList());
    }

    @McpTool(name = "get_account", description = "Get one account by id.",
            annotations = @McpAnnotations(readOnlyHint = true))
    public CallToolResult getAccount(@McpToolParam(description = "Account UUID") String accountId) {
        return run(() -> AccountResponse.from(service.getAccount(uuid(accountId, "accountId"))));
    }

    @McpTool(name = "get_account_balance", description = "Get the current balance of an account.",
            annotations = @McpAnnotations(readOnlyHint = true))
    public CallToolResult getAccountBalance(@McpToolParam(description = "Account UUID") String accountId) {
        return run(() -> BalanceResponse.from(service.getAccount(uuid(accountId, "accountId"))));
    }

    @McpTool(name = "open_account",
            description = "Open a new THB account for a customer with a 0.00 balance and status OPEN.")
    public CallToolResult openAccount(
            @McpToolParam(description = "Customer UUID") String customerId,
            @McpToolParam(description = "Optional label, max 100 characters", required = false) String label) {
        return run(() -> AccountResponse.from(service.openAccount(uuid(customerId, "customerId"), label)));
    }

    @McpTool(name = "update_account", description = "Set or clear an account's label (max 100 characters).")
    public CallToolResult updateAccount(
            @McpToolParam(description = "Account UUID") String accountId,
            @McpToolParam(description = "New label; omit or leave blank to clear", required = false) String label) {
        return run(() -> AccountResponse.from(service.updateAccount(uuid(accountId, "accountId"), label)));
    }

    @McpTool(name = "close_account",
            description = "Close an account. The balance must be exactly 0.00 (NON_ZERO_BALANCE otherwise). A closed "
                    + "account rejects deposits, withdrawals and transfers; its history stays readable.",
            annotations = @McpAnnotations(destructiveHint = true))
    public CallToolResult closeAccount(@McpToolParam(description = "Account UUID") String accountId) {
        return run(() -> AccountResponse.from(service.closeAccount(uuid(accountId, "accountId"))));
    }

    // ---- money movement ----------------------------------------------------

    @McpTool(name = "deposit", description = "Deposit money (THB) into an open account.")
    public CallToolResult deposit(
            @McpToolParam(description = "Account UUID") String accountId,
            @McpToolParam(description = "Positive decimal string with at most 2 decimals, e.g. \"250.00\"")
            String amount) {
        return run(() -> TransactionEntryResponse.from(service.deposit(uuid(accountId, "accountId"), amount)));
    }

    @McpTool(name = "withdraw",
            description = "Withdraw money (THB) from an open account. INSUFFICIENT_FUNDS if the balance is too low.")
    public CallToolResult withdraw(
            @McpToolParam(description = "Account UUID") String accountId,
            @McpToolParam(description = "Positive decimal string with at most 2 decimals, e.g. \"100.00\"")
            String amount) {
        return run(() -> TransactionEntryResponse.from(service.withdraw(uuid(accountId, "accountId"), amount)));
    }

    @McpTool(name = "transfer_funds",
            description = "Transfer money (THB) between two different open accounts, atomically.")
    public CallToolResult transferFunds(
            @McpToolParam(description = "Source account UUID") String sourceAccountId,
            @McpToolParam(description = "Destination account UUID") String destinationAccountId,
            @McpToolParam(description = "Positive decimal string with at most 2 decimals, e.g. \"300.00\"")
            String amount) {
        return run(() -> TransferResponse.from(service.transfer(uuid(sourceAccountId, "sourceAccountId"),
                uuid(destinationAccountId, "destinationAccountId"), amount)));
    }

    @McpTool(name = "get_transaction_history",
            description = "Get an account's transaction history, newest first, paged. Works for closed accounts.",
            annotations = @McpAnnotations(readOnlyHint = true))
    public CallToolResult getTransactionHistory(
            @McpToolParam(description = "Account UUID") String accountId,
            @McpToolParam(description = "Page number, default 0", required = false) Integer page,
            @McpToolParam(description = "Page size 1-100, default 20", required = false) Integer size) {
        return run(() -> PageResponse.of(service.getHistory(uuid(accountId, "accountId"), p(page), s(size)),
                TransactionEntryResponse::from));
    }

    // ---- helpers -----------------------------------------------------------

    private CallToolResult run(Supplier<Object> action) {
        try {
            Object value = action.get();
            return McpErrorMapper.success(json.writeValueAsString(value), value);
        } catch (Exception e) {
            return McpErrorMapper.failure(e);
        }
    }

    private static int p(Integer page) {
        return page == null ? 0 : page;
    }

    private static int s(Integer size) {
        return size == null ? DEFAULT_SIZE : size;
    }

    private static UUID uuid(String value, String field) {
        try {
            return UUID.fromString(value == null ? "" : value.trim());
        } catch (IllegalArgumentException e) {
            throw BankingException.invalid(field + " must be a valid UUID");
        }
    }

    private static CustomerInput input(String name, String englishName, String email, String phone,
                                       String addressLine, String subdistrict, String district, String province,
                                       String postcode, String dateOfBirth, String occupation, String nationalId) {
        LocalDate dob = null;
        if (dateOfBirth != null && !dateOfBirth.isBlank()) {
            try {
                dob = LocalDate.parse(dateOfBirth.trim());
            } catch (DateTimeParseException e) {
                throw BankingException.invalid("dateOfBirth must be an ISO date (yyyy-MM-dd)");
            }
        }
        return new CustomerInput(name, englishName, email, phone, addressLine, subdistrict, district, province,
                postcode, dob, occupation, nationalId);
    }
}
