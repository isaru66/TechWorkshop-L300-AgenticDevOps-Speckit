package com.workshop.banking.service;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.workshop.banking.support.PostgresIntegrationTest;
import java.math.BigDecimal;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import org.springframework.http.MediaType;

class TransferServiceIT extends PostgresIntegrationTest {

    private void transfer(UUID src, UUID dst, String amount, int expectedStatus, String expectedCode)
            throws Exception {
        var result = mvc.perform(post("/api/transfers").contentType(MediaType.APPLICATION_JSON)
                .content("{\"sourceAccountId\":\"" + src + "\",\"destinationAccountId\":\"" + dst
                        + "\",\"amount\":\"" + amount + "\"}"));
        result.andExpect(status().is(expectedStatus));
        if (expectedCode != null) {
            result.andExpect(jsonPath("$.code").value(expectedCode));
        }
    }

    @Test
    void successfulTransferMovesBothBalancesAndHistories() throws Exception {
        List<UUID> accounts = pickAccounts("1000.00");
        UUID src = accounts.get(0);
        UUID dst = accounts.get(1);
        BigDecimal srcBefore = balance(src);
        BigDecimal dstBefore = balance(dst);
        int srcEntries = history(src).size();
        int dstEntries = history(dst).size();

        transfer(src, dst, "100.00", 201, null);

        assertThat(balance(src)).isEqualByComparingTo(srcBefore.subtract(new BigDecimal("100.00")));
        assertThat(balance(dst)).isEqualByComparingTo(dstBefore.add(new BigDecimal("100.00")));
        assertThat(balance(src).add(balance(dst))).isEqualByComparingTo(srcBefore.add(dstBefore));

        List<Map<String, Object>> srcHistory = history(src);
        List<Map<String, Object>> dstHistory = history(dst);
        assertThat(srcHistory).hasSize(srcEntries + 1);
        assertThat(dstHistory).hasSize(dstEntries + 1);
        Map<String, Object> out = srcHistory.get(srcHistory.size() - 1);
        Map<String, Object> in = dstHistory.get(dstHistory.size() - 1);
        assertThat(out.get("type")).isEqualTo("TRANSFER_OUT");
        assertThat(in.get("type")).isEqualTo("TRANSFER_IN");
        assertThat(out.get("transfer_id")).isNotNull().isEqualTo(in.get("transfer_id"));
        assertThat(out.get("counterparty_account_id")).isEqualTo(dst);
        assertThat(in.get("counterparty_account_id")).isEqualTo(src);
        assertThat((BigDecimal) out.get("resulting_balance")).isEqualByComparingTo(balance(src));
        assertThat((BigDecimal) in.get("resulting_balance")).isEqualByComparingTo(balance(dst));
    }

    @Test
    void insufficientFundsLeavesBalancesAndHistoriesUnchanged() throws Exception {
        List<UUID> accounts = pickAccounts("1.00");
        UUID src = accounts.get(0);
        UUID dst = accounts.get(1);
        BigDecimal srcBefore = balance(src);
        BigDecimal dstBefore = balance(dst);
        var srcHistory = history(src);
        var dstHistory = history(dst);
        int transfersBefore = jdbc.queryForObject("select count(*) from transfer", Integer.class);

        transfer(src, dst, srcBefore.add(new BigDecimal("0.01")).toPlainString(), 422, "INSUFFICIENT_FUNDS");

        assertThat(balance(src)).isEqualByComparingTo(srcBefore);
        assertThat(balance(dst)).isEqualByComparingTo(dstBefore);
        assertThat(history(src)).isEqualTo(srcHistory);
        assertThat(history(dst)).isEqualTo(dstHistory);
        assertThat(jdbc.queryForObject("select count(*) from transfer", Integer.class)).isEqualTo(transfersBefore);
    }

    @Test
    void invalidTransfersAreRejectedWithoutChanges() throws Exception {
        List<UUID> accounts = pickAccounts("10.00");
        UUID src = accounts.get(0);
        UUID dst = accounts.get(1);
        BigDecimal srcBefore = balance(src);
        int entries = jdbc.queryForObject("select count(*) from transaction_entry", Integer.class);

        transfer(src, src, "1.00", 422, "SAME_ACCOUNT");
        transfer(src, UUID.randomUUID(), "1.00", 404, "NOT_FOUND");
        transfer(UUID.randomUUID(), dst, "1.00", 404, "NOT_FOUND");
        transfer(src, dst, "0", 400, "VALIDATION_ERROR");
        transfer(src, dst, "-5.00", 400, "VALIDATION_ERROR");
        transfer(src, dst, "1.234", 400, "VALIDATION_ERROR");
        transfer(src, dst, "abc", 400, "VALIDATION_ERROR");

        assertThat(balance(src)).isEqualByComparingTo(srcBefore);
        assertThat(jdbc.queryForObject("select count(*) from transaction_entry", Integer.class)).isEqualTo(entries);
    }

    @Test
    void transferringExactBalanceLeavesZero() throws Exception {
        UUID src = UUID.randomUUID();
        UUID customer = jdbc.queryForObject("select id from customer limit 1", UUID.class);
        UUID dst = pickAccounts("1.00").get(0);
        jdbc.update("insert into account (id, customer_id, currency, balance, created_at) values (?,?,?,?,now())",
                src, customer, "THB", new BigDecimal("25.50"));
        transfer(src, dst, "25.50", 201, null);
        assertThat(balance(src)).isEqualByComparingTo(BigDecimal.ZERO);
    }
}
