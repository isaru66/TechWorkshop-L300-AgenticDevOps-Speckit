package com.workshop.banking.mcp;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;

import com.workshop.banking.support.PostgresIntegrationTest;
import java.math.BigDecimal;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.UUID;
import java.util.concurrent.Callable;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.stream.StreamSupport;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.MediaType;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.json.JsonMapper;

class McpToolsIT extends PostgresIntegrationTest {

    private static final Set<String> TOOLS = Set.of("list_customers", "search_customers", "get_customer",
            "create_customer", "update_customer", "delete_customer", "list_customer_accounts", "get_account",
            "get_account_balance", "open_account", "update_account", "close_account", "deposit", "withdraw",
            "transfer_funds", "get_transaction_history");

    @Autowired
    JsonMapper json;

    private final AtomicInteger nextId = new AtomicInteger(1);

    // ---- protocol helpers --------------------------------------------------

    private JsonNode rpc(String method, Object params) throws Exception {
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("jsonrpc", "2.0");
        body.put("id", nextId.getAndIncrement());
        body.put("method", method);
        body.put("params", params);
        String raw = mvc.perform(post("/mcp").contentType(MediaType.APPLICATION_JSON)
                        .accept(MediaType.APPLICATION_JSON, MediaType.TEXT_EVENT_STREAM)
                        .content(json.writeValueAsString(body)))
                .andReturn().getResponse().getContentAsString();
        return json.readTree(payload(raw));
    }

    /** The response is plain JSON or SSE-framed ("data: {...}"); both carry the same message. */
    private static String payload(String raw) {
        String trimmed = raw.trim();
        if (trimmed.startsWith("{")) {
            return trimmed;
        }
        return trimmed.lines().filter(l -> l.startsWith("data:")).map(l -> l.substring(5).trim())
                .findFirst().orElseThrow(() -> new AssertionError("No JSON payload in: " + raw));
    }

    private JsonNode call(String tool, Object... kv) throws Exception {
        Map<String, Object> args = new LinkedHashMap<>();
        for (int i = 0; i < kv.length; i += 2) {
            args.put((String) kv[i], kv[i + 1]);
        }
        return rpc("tools/call", Map.of("name", tool, "arguments", args)).get("result");
    }

    private String text(JsonNode result) {
        return result.get("content").get(0).get("text").asString();
    }

    private JsonNode ok(JsonNode result) throws Exception {
        assertThat(result.get("isError").asBoolean()).as(text(result)).isFalse();
        return json.readTree(text(result));
    }

    private void assertError(JsonNode result, String code) {
        assertThat(result.get("isError").asBoolean()).isTrue();
        assertThat(text(result)).startsWith(code + ":");
    }

    private JsonNode restGet(String path) throws Exception {
        return json.readTree(mvc.perform(get(path)).andReturn().getResponse().getContentAsString());
    }

    private String newCustomerId(String name) throws Exception {
        return ok(call("create_customer", "name", name)).get("id").asString();
    }

    private String newAccount(String customerId) throws Exception {
        return ok(call("open_account", "customerId", customerId)).get("id").asString();
    }

    // ---- protocol ----------------------------------------------------------

    @Test
    void initializeAndListTools() throws Exception {
        JsonNode init = rpc("initialize", Map.of("protocolVersion", "2025-06-18", "capabilities", Map.of(),
                "clientInfo", Map.of("name", "it", "version", "1")));
        assertThat(init.get("result").get("serverInfo").get("name").asString()).isEqualTo("local-banking-mcp");
        assertThat(init.get("result").get("capabilities").has("tools")).isTrue();

        JsonNode tools = rpc("tools/list", Map.of()).get("result").get("tools");
        List<String> names = StreamSupport.stream(tools.spliterator(), false)
                .map(t -> t.get("name").asString()).toList();
        assertThat(names).containsExactlyInAnyOrderElementsOf(TOOLS);
        tools.forEach(t -> {
            assertThat(t.get("description").asString()).isNotBlank();
            assertThat(t.get("inputSchema").has("properties")).isTrue();
        });
    }

    // ---- US1: reads --------------------------------------------------------

    @Test
    void readToolsMatchRest() throws Exception {
        JsonNode restCustomers = restGet("/api/customers?page=0&size=5");
        JsonNode mcpCustomers = ok(call("list_customers", "page", 0, "size", 5));
        assertThat(mcpCustomers).isEqualTo(restCustomers);

        JsonNode first = restCustomers.get("items").get(0);
        String id = first.get("id").asString();
        assertThat(ok(call("get_customer", "customerId", id))).isEqualTo(first);

        JsonNode accounts = ok(call("list_customer_accounts", "customerId", id));
        assertThat(accounts).isEqualTo(restGet("/api/customers/" + id + "/accounts"));
        String accountId = accounts.get(0).get("id").asString();
        assertThat(ok(call("get_account", "accountId", accountId))).isEqualTo(restGet("/api/accounts/" + accountId));
        assertThat(ok(call("get_account_balance", "accountId", accountId)))
                .isEqualTo(restGet("/api/accounts/" + accountId + "/balance"));
        assertThat(ok(call("get_transaction_history", "accountId", accountId, "size", 10)))
                .isEqualTo(restGet("/api/accounts/" + accountId + "/transactions?size=10"));
    }

    @Test
    void searchMatchesCaseInsensitivelyAndPartially() throws Exception {
        String unique = "Zyxwv" + UUID.randomUUID().toString().substring(0, 8);
        String id = newCustomerId(unique + " Testperson");

        JsonNode found = ok(call("search_customers", "query", unique.toUpperCase().substring(0, 10)));
        assertThat(found.get("totalElements").asInt()).isEqualTo(1);
        assertThat(found.get("items").get(0).get("id").asString()).isEqualTo(id);
        assertThat(ok(call("search_customers", "query", "no-such-" + UUID.randomUUID())).get("totalElements").asInt())
                .isZero();
        assertError(call("search_customers", "query", " "), "VALIDATION_ERROR");
        // LIKE wildcards are literal text, not patterns
        assertThat(ok(call("search_customers", "query", "%")).get("totalElements").asInt()).isZero();
        assertThat(ok(call("search_customers", "query", "_")).get("totalElements").asInt()).isZero();
    }

    @Test
    void readErrors() throws Exception {
        assertError(call("get_customer", "customerId", UUID.randomUUID().toString()), "NOT_FOUND");
        assertError(call("get_account", "accountId", UUID.randomUUID().toString()), "NOT_FOUND");
        assertError(call("get_account", "accountId", "not-a-uuid"), "VALIDATION_ERROR");
        assertError(call("list_customers", "size", 101), "VALIDATION_ERROR");
    }

    // ---- US2: manage -------------------------------------------------------

    @Test
    void customerAndAccountLifecycle() throws Exception {
        JsonNode created = ok(call("create_customer", "name", "Lifecycle Tester", "email", "t@example.com",
                "dateOfBirth", "1990-05-01"));
        String customerId = created.get("id").asString();
        assertThat(created.get("email").asString()).isEqualTo("t@example.com");

        JsonNode updated = ok(call("update_customer", "customerId", customerId, "name", "Renamed Tester",
                "phone", "0800000000"));
        assertThat(updated.get("name").asString()).isEqualTo("Renamed Tester");
        assertThat(updated.get("createdAt")).isEqualTo(ok(call("get_customer", "customerId", customerId)).get("createdAt"));
        assertThat(restGet("/api/customers/" + customerId).get("phone").asString()).isEqualTo("0800000000");

        JsonNode account = ok(call("open_account", "customerId", customerId, "label", "Savings"));
        String accountId = account.get("id").asString();
        assertThat(account.get("balance").asString()).isEqualTo("0.00");
        assertThat(account.get("status").asString()).isEqualTo("OPEN");
        assertThat(account.get("label").asString()).isEqualTo("Savings");
        assertThat(ok(call("update_account", "accountId", accountId, "label", "Daily")).get("label").asString())
                .isEqualTo("Daily");

        assertError(call("delete_customer", "customerId", customerId), "CUSTOMER_HAS_ACCOUNTS");

        ok(call("deposit", "accountId", accountId, "amount", "10.00"));
        assertError(call("close_account", "accountId", accountId), "NON_ZERO_BALANCE");
        ok(call("withdraw", "accountId", accountId, "amount", "10.00"));

        assertThat(ok(call("close_account", "accountId", accountId)).get("status").asString()).isEqualTo("CLOSED");
        assertError(call("close_account", "accountId", accountId), "ACCOUNT_CLOSED");
        assertError(call("deposit", "accountId", accountId, "amount", "1.00"), "ACCOUNT_CLOSED");
        assertThat(ok(call("get_transaction_history", "accountId", accountId)).get("totalElements").asInt())
                .isEqualTo(2);
        assertError(call("delete_customer", "customerId", customerId), "CUSTOMER_HAS_ACCOUNTS");
    }

    @Test
    void deleteCustomerWithoutAccounts() throws Exception {
        String id = newCustomerId("Short Lived");
        JsonNode result = ok(call("delete_customer", "customerId", id));
        assertThat(result.get("deleted").asBoolean()).isTrue();
        assertError(call("get_customer", "customerId", id), "NOT_FOUND");
        assertError(call("delete_customer", "customerId", id), "NOT_FOUND");
    }

    @Test
    void manageValidation() throws Exception {
        assertError(call("create_customer", "name", " "), "VALIDATION_ERROR");
        assertError(call("create_customer", "name", "X", "email", "nope"), "VALIDATION_ERROR");
        assertError(call("create_customer", "name", "X", "dateOfBirth", "2999-01-01"), "VALIDATION_ERROR");
        assertError(call("create_customer", "name", "X", "dateOfBirth", "yesterday"), "VALIDATION_ERROR");
        String customerId = newCustomerId("Label Tester");
        assertError(call("open_account", "customerId", customerId, "label", "x".repeat(101)), "VALIDATION_ERROR");
        assertError(call("open_account", "customerId", UUID.randomUUID().toString()), "NOT_FOUND");
        assertError(call("update_customer", "customerId", UUID.randomUUID().toString(), "name", "N"), "NOT_FOUND");
    }

    // ---- US3: money --------------------------------------------------------

    @Test
    void depositWithdrawTransferAndHistory() throws Exception {
        String customerId = newCustomerId("Money Mover");
        String a = newAccount(customerId);
        String b = newAccount(customerId);

        ok(call("deposit", "accountId", a, "amount", "1000.00"));
        ok(call("withdraw", "accountId", a, "amount", "200.00"));
        JsonNode transfer = ok(call("transfer_funds", "sourceAccountId", a, "destinationAccountId", b,
                "amount", "300.00"));
        assertThat(transfer.get("amount").asString()).isEqualTo("300.00");

        assertThat(ok(call("get_account_balance", "accountId", a)).get("balance").asString()).isEqualTo("500.00");
        assertThat(ok(call("get_account_balance", "accountId", b)).get("balance").asString()).isEqualTo("300.00");

        List<String> types = StreamSupport.stream(
                ok(call("get_transaction_history", "accountId", a)).get("items").spliterator(), false)
                .map(e -> e.get("type").asString()).toList();
        assertThat(types).containsExactly("TRANSFER_OUT", "WITHDRAWAL", "DEPOSIT");
        // Visible through REST too
        assertThat(restGet("/api/accounts/" + b + "/transactions").get("items").get(0).get("type").asString())
                .isEqualTo("TRANSFER_IN");
        assertThat(balance(UUID.fromString(a))).isEqualByComparingTo(new BigDecimal("500.00"));
    }

    @Test
    void refusedMoneyOperationsChangeNothing() throws Exception {
        String customerId = newCustomerId("Rule Checker");
        String a = newAccount(customerId);
        String b = newAccount(customerId);
        ok(call("deposit", "accountId", a, "amount", "50.00"));

        assertError(call("withdraw", "accountId", a, "amount", "50.01"), "INSUFFICIENT_FUNDS");
        assertError(call("transfer_funds", "sourceAccountId", a, "destinationAccountId", b, "amount", "50.01"),
                "INSUFFICIENT_FUNDS");
        assertError(call("deposit", "accountId", a, "amount", "0"), "VALIDATION_ERROR");
        assertError(call("deposit", "accountId", a, "amount", "-5.00"), "VALIDATION_ERROR");
        assertError(call("deposit", "accountId", a, "amount", "1.234"), "VALIDATION_ERROR");
        assertError(call("transfer_funds", "sourceAccountId", a, "destinationAccountId", a, "amount", "1.00"),
                "SAME_ACCOUNT");
        assertError(call("transfer_funds", "sourceAccountId", a, "destinationAccountId",
                UUID.randomUUID().toString(), "amount", "1.00"), "NOT_FOUND");

        assertThat(balance(UUID.fromString(a))).isEqualByComparingTo(new BigDecimal("50.00"));
        assertThat(balance(UUID.fromString(b))).isEqualByComparingTo(BigDecimal.ZERO);
        assertThat(history(UUID.fromString(a))).hasSize(1);
    }

    @Test
    void concurrentOperationsKeepBalanceConsistent() throws Exception {
        String account = newAccount(newCustomerId("Concurrent Mover"));
        ok(call("deposit", "accountId", account, "amount", "100.00"));

        ExecutorService pool = Executors.newFixedThreadPool(8);
        try {
            List<Future<Boolean>> results = new java.util.ArrayList<>();
            for (int i = 0; i < 20; i++) {
                results.add(pool.submit(callable("deposit", account, "5.00")));
                results.add(pool.submit(callable("withdraw", account, "3.00")));
            }
            int deposits = 0;
            int withdrawals = 0;
            for (int i = 0; i < results.size(); i++) {
                if (results.get(i).get()) {
                    if (i % 2 == 0) {
                        deposits++;
                    } else {
                        withdrawals++;
                    }
                }
            }
            assertThat(deposits).isEqualTo(20);
            assertThat(withdrawals).isEqualTo(20);
        } finally {
            pool.shutdown();
        }
        assertThat(balance(UUID.fromString(account))).isEqualByComparingTo(new BigDecimal("140.00"));
        List<Map<String, Object>> h = history(UUID.fromString(account));
        assertThat(h).hasSize(41);
        assertThat((BigDecimal) h.get(h.size() - 1).get("resulting_balance")).isEqualByComparingTo("140.00");
    }

    @Test
    void closeRacingWithDepositNeverLeavesClosedAccountWithMoney() throws Exception {
        for (int round = 0; round < 5; round++) {
            String account = newAccount(newCustomerId("Race " + round));
            ExecutorService pool = Executors.newFixedThreadPool(2);
            try {
                Future<JsonNode> deposit = pool.submit(() -> call("deposit", "accountId", account, "amount", "1.00"));
                Future<JsonNode> close = pool.submit(() -> call("close_account", "accountId", account));
                deposit.get();
                close.get();
            } finally {
                pool.shutdown();
            }
            JsonNode state = ok(call("get_account", "accountId", account));
            boolean closed = "CLOSED".equals(state.get("status").asString());
            assertThat(closed && new BigDecimal(state.get("balance").asString()).signum() != 0)
                    .as("closed account holds money: %s", state).isFalse();
        }
    }

    @Test
    void unexpectedFailureReturnsGenericError() throws Exception {
        String account = newAccount(newCustomerId("Overflow Tester"));
        ok(call("deposit", "accountId", account, "amount", "99999999999999999.99"));
        // Pushes the balance past numeric(19,2): an unexpected database failure, not a business rule
        JsonNode result = call("deposit", "accountId", account, "amount", "99999999999999999.99");
        assertError(result, "INTERNAL_ERROR");
        assertThat(text(result)).doesNotContain("Exception").doesNotContain("at com.").doesNotContain("SQL");
        assertThat(balance(UUID.fromString(account))).isEqualByComparingTo(new BigDecimal("99999999999999999.99"));
        // Server stays usable
        assertThat(ok(call("get_account_balance", "accountId", account)).get("balance").asString())
                .isEqualTo("99999999999999999.99");
    }

    private Callable<Boolean> callable(String tool, String accountId, String amount) {
        return () -> !call(tool, "accountId", accountId, "amount", amount).get("isError").asBoolean();
    }

    @Test
    void closedAccountRejectsTransfers() throws Exception {
        String customerId = newCustomerId("Closed Transfer");
        String open = newAccount(customerId);
        String closed = newAccount(customerId);
        ok(call("close_account", "accountId", closed));
        ok(call("deposit", "accountId", open, "amount", "20.00"));

        assertError(call("transfer_funds", "sourceAccountId", open, "destinationAccountId", closed,
                "amount", "5.00"), "ACCOUNT_CLOSED");
        assertError(call("transfer_funds", "sourceAccountId", closed, "destinationAccountId", open,
                "amount", "5.00"), "ACCOUNT_CLOSED");
        assertThat(balance(UUID.fromString(open))).isEqualByComparingTo(new BigDecimal("20.00"));
    }
}
