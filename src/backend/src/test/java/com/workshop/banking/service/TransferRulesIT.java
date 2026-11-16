package com.workshop.banking.service;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.doThrow;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

import com.workshop.banking.domain.Account;
import com.workshop.banking.repository.AccountRepository;
import com.workshop.banking.repository.TransactionEntryRepository;
import com.workshop.banking.support.PostgresIntegrationTest;
import java.math.BigDecimal;
import java.time.Instant;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.test.context.bean.override.mockito.MockitoSpyBean;

class TransferRulesIT extends PostgresIntegrationTest {

    @Autowired
    BankingService service;

    @MockitoSpyBean
    TransactionEntryRepository entryRepository;

    @Test
    void currencyMismatchIsRejectedWithoutChangingAnything() {
        AccountRepository accounts = mock(AccountRepository.class);
        Account thb = new Account(UUID.randomUUID(), UUID.randomUUID(), "THB", new BigDecimal("100.00"), Instant.now());
        Account usd = new Account(UUID.randomUUID(), UUID.randomUUID(), "USD", new BigDecimal("100.00"), Instant.now());
        when(accounts.findByIdForUpdate(thb.getId())).thenReturn(Optional.of(thb));
        when(accounts.findByIdForUpdate(usd.getId())).thenReturn(Optional.of(usd));
        BankingService isolated = new BankingService(null, accounts, null, null, mock(JdbcTemplate.class));

        assertThatThrownBy(() -> isolated.transfer(thb.getId(), usd.getId(), "10.00"))
                .isInstanceOfSatisfying(BankingException.class,
                        e -> assertThat(e.getCode()).isEqualTo("CURRENCY_MISMATCH"));
        assertThat(thb.getBalance()).isEqualByComparingTo("100.00");
        assertThat(usd.getBalance()).isEqualByComparingTo("100.00");
    }

    @Test
    void failureAfterBalanceUpdateRollsBackBalancesAndHistories() {
        List<UUID> accounts = pickAccounts("100.00");
        UUID src = accounts.get(0);
        UUID dst = accounts.get(1);
        BigDecimal srcBalance = balance(src);
        BigDecimal dstBalance = balance(dst);
        var srcHistory = history(src);
        var dstHistory = history(dst);
        int transfers = jdbc.queryForObject("select count(*) from transfer", Integer.class);

        doThrow(new IllegalStateException("injected")).when(entryRepository).save(any());

        assertThatThrownBy(() -> service.transfer(src, dst, "10.00")).isInstanceOf(IllegalStateException.class);

        assertThat(balance(src)).isEqualByComparingTo(srcBalance);
        assertThat(balance(dst)).isEqualByComparingTo(dstBalance);
        assertThat(history(src)).isEqualTo(srcHistory);
        assertThat(history(dst)).isEqualTo(dstHistory);
        assertThat(jdbc.queryForObject("select count(*) from transfer", Integer.class)).isEqualTo(transfers);
    }
}
