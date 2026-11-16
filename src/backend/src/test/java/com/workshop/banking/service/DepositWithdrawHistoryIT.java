package com.workshop.banking.service;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.workshop.banking.support.PostgresIntegrationTest;
import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.UUID;
import java.util.concurrent.Callable;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;
import org.junit.jupiter.api.Test;
import org.springframework.http.MediaType;

class DepositWithdrawHistoryIT extends PostgresIntegrationTest {

    private void amount(String path, UUID id, String amount, int expected) throws Exception {
        mvc.perform(post(path, id).contentType(MediaType.APPLICATION_JSON).content("{\"amount\":\"" + amount + "\"}"))
                .andExpect(status().is(expected));
    }

    @Test
    void depositThenWithdrawUpdatesBalanceAndHistory() throws Exception {
        UUID id = pickAccounts("500.00").get(0);
        BigDecimal start = balance(id);
        int entries = history(id).size();

        amount("/api/accounts/{id}/deposits", id, "250.25", 201);
        amount("/api/accounts/{id}/withdrawals", id, "100.00", 201);

        assertThat(balance(id)).isEqualByComparingTo(start.add(new BigDecimal("150.25")));
        var h = history(id);
        assertThat(h).hasSize(entries + 2);
        assertThat(h.get(h.size() - 2).get("type")).isEqualTo("DEPOSIT");
        assertThat(h.get(h.size() - 1).get("type")).isEqualTo("WITHDRAWAL");
        assertThat((BigDecimal) h.get(h.size() - 1).get("resulting_balance")).isEqualByComparingTo(balance(id));
    }

    @Test
    void rejectedMovementsChangeNothing() throws Exception {
        UUID id = pickAccounts("10.00").get(0);
        BigDecimal start = balance(id);
        var before = history(id);

        mvc.perform(post("/api/accounts/{id}/withdrawals", id).contentType(MediaType.APPLICATION_JSON)
                        .content("{\"amount\":\"" + start.add(BigDecimal.ONE).toPlainString() + "\"}"))
                .andExpect(status().isUnprocessableEntity())
                .andExpect(jsonPath("$.code").value("INSUFFICIENT_FUNDS"));
        for (String bad : new String[] {"0", "-1", "1.001", "x"}) {
            amount("/api/accounts/{id}/deposits", id, bad, 400);
            amount("/api/accounts/{id}/withdrawals", id, bad, 400);
        }
        amount("/api/accounts/{id}/deposits", UUID.randomUUID(), "1.00", 404);

        assertThat(balance(id)).isEqualByComparingTo(start);
        assertThat(history(id)).isEqualTo(before);
    }

    @Test
    void concurrentWithdrawalsNeverGoNegative() throws Exception {
        UUID customer = jdbc.queryForObject("select id from customer limit 1", UUID.class);
        UUID id = UUID.randomUUID();
        jdbc.update("insert into account (id, customer_id, currency, balance, created_at) values (?,?,?,?,now())",
                id, customer, "THB", new BigDecimal("100.00"));
        ExecutorService pool = Executors.newFixedThreadPool(8);
        List<Future<Integer>> results = new ArrayList<>();
        for (int i = 0; i < 20; i++) {
            Callable<Integer> call = () -> mvc.perform(post("/api/accounts/{id}/withdrawals", id)
                            .contentType(MediaType.APPLICATION_JSON).content("{\"amount\":\"10.00\"}"))
                    .andReturn().getResponse().getStatus();
            results.add(pool.submit(call));
        }
        int ok = 0;
        for (Future<Integer> f : results) {
            if (f.get() == 201) {
                ok++;
            }
        }
        pool.shutdown();
        assertThat(ok).isEqualTo(10);
        assertThat(balance(id)).isEqualByComparingTo(BigDecimal.ZERO);
    }

    @Test
    void opposingConcurrentTransfersDoNotDeadlockOrLoseMoney() throws Exception {
        var accounts = pickAccounts("1000.00");
        UUID a = accounts.get(0);
        UUID b = accounts.get(1);
        BigDecimal total = balance(a).add(balance(b));
        ExecutorService pool = Executors.newFixedThreadPool(8);
        List<Future<Integer>> results = new ArrayList<>();
        for (int i = 0; i < 20; i++) {
            UUID from = i % 2 == 0 ? a : b;
            UUID to = i % 2 == 0 ? b : a;
            Callable<Integer> call = () -> mvc.perform(post("/api/transfers").contentType(MediaType.APPLICATION_JSON)
                            .content("{\"sourceAccountId\":\"" + from + "\",\"destinationAccountId\":\"" + to
                                    + "\",\"amount\":\"1.00\"}"))
                    .andReturn().getResponse().getStatus();
            results.add(pool.submit(call));
        }
        for (Future<Integer> f : results) {
            assertThat(f.get()).isEqualTo(201);
        }
        pool.shutdown();
        assertThat(balance(a).add(balance(b))).isEqualByComparingTo(total);
        assertThat(balance(a)).isGreaterThanOrEqualTo(BigDecimal.ZERO);
    }

    @Test
    void historyIsNewestFirstAndPagesDoNotOverlap() throws Exception {
        UUID id = jdbc.queryForObject("select account_id from transaction_entry group by account_id "
                + "having count(*) >= 5 order by count(*) desc, account_id limit 1", UUID.class);
        int total = history(id).size();
        Set<String> seen = new HashSet<>();
        int pages = (total + 3) / 4;
        for (int p = 0; p < pages; p++) {
            String body = mvc.perform(get("/api/accounts/{id}/transactions?page={p}&size=4", id, p))
                    .andExpect(status().isOk())
                    .andExpect(jsonPath("$.totalElements").value(total))
                    .andReturn().getResponse().getContentAsString();
            var m = java.util.regex.Pattern.compile("\"sequence\":(\\d+)").matcher(body);
            long previous = Long.MAX_VALUE;
            while (m.find()) {
                long seq = Long.parseLong(m.group(1));
                assertThat(seen.add(m.group(1))).isTrue();
                assertThat(seq).isLessThan(previous);
                previous = seq;
            }
        }
        assertThat(seen).hasSize(total);
        mvc.perform(get("/api/accounts/{id}/transactions?size=0", id)).andExpect(status().isBadRequest());
        mvc.perform(get("/api/accounts/{id}/transactions?size=101", id)).andExpect(status().isBadRequest());
        mvc.perform(get("/api/accounts/{id}/transactions", UUID.randomUUID())).andExpect(status().isNotFound());
    }
}
