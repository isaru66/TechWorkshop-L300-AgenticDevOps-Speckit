package com.workshop.banking.support;

import static org.assertj.core.api.Assertions.assertThat;

import java.math.BigDecimal;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.webmvc.test.autoconfigure.AutoConfigureMockMvc;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.test.context.DynamicPropertyRegistry;
import org.springframework.test.context.DynamicPropertySource;
import org.springframework.test.web.servlet.MockMvc;
import org.testcontainers.postgresql.PostgreSQLContainer;

/** Runs against a throwaway PostgreSQL container, never the local development database. */
@SpringBootTest
@AutoConfigureMockMvc
public abstract class PostgresIntegrationTest {

    static final PostgreSQLContainer POSTGRES = new PostgreSQLContainer("postgres:17");

    static {
        POSTGRES.start();
    }

    @DynamicPropertySource
    static void datasource(DynamicPropertyRegistry registry) {
        registry.add("spring.datasource.url", POSTGRES::getJdbcUrl);
        registry.add("spring.datasource.username", POSTGRES::getUsername);
        registry.add("spring.datasource.password", POSTGRES::getPassword);
        registry.add("banking.seed.path", () -> "../data/seed");
    }

    @Autowired
    protected MockMvc mvc;
    @Autowired
    protected JdbcTemplate jdbc;

    @BeforeEach
    void seedIsLoaded() {
        assertThat(jdbc.queryForObject("select count(*) from account", Integer.class)).isGreaterThan(0);
    }

    /** Distinct same-currency accounts; the first one holds at least {@code minBalance}. */
    protected List<UUID> pickAccounts(String minBalance) {
        List<UUID> ids = jdbc.queryForList(
                "select id from account where balance >= ? order by balance desc, id limit 2",
                UUID.class, new BigDecimal(minBalance));
        assertThat(ids).hasSize(2);
        return ids;
    }

    protected BigDecimal balance(UUID id) {
        return jdbc.queryForObject("select balance from account where id = ?", BigDecimal.class, id);
    }

    protected List<Map<String, Object>> history(UUID id) {
        return jdbc.queryForList("select id, sequence, type, amount, resulting_balance, transfer_id, "
                + "counterparty_account_id from transaction_entry where account_id = ? order by sequence", id);
    }
}
