package com.workshop.banking.seed;

import static org.assertj.core.api.Assertions.assertThat;

import com.workshop.banking.support.PostgresIntegrationTest;
import java.math.BigDecimal;
import java.nio.file.Path;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;

class SeedImporterIT extends PostgresIntegrationTest {

    private static final Path SEED = Path.of("../data/seed");

    @Autowired
    SeedImporter importer;

    private int count(String table) {
        return jdbc.queryForObject("select count(*) from " + table, Integer.class);
    }

    private void reset() {
        jdbc.execute("truncate transaction_entry, transfer, account, customer cascade");
    }

    @Test
    void importsBaselineOnceAndIsRepeatableWithoutOverwritingChanges() {
        reset();
        var first = importer.importFrom(SEED);
        assertThat(first.total()).isEqualTo(102 + 150 + 101 + 1002);
        assertThat(count("customer")).isEqualTo(102);
        assertThat(count("account")).isEqualTo(150);
        assertThat(count("transfer")).isEqualTo(101);
        assertThat(count("transaction_entry")).isEqualTo(1002);

        // Imported balances equal the seed file, not a replay of the history.
        var rows = SeedData.load(SEED).accounts();
        for (var row : rows) {
            assertThat(balance(row.id())).isEqualByComparingTo(row.balance());
        }

        // A later change must survive a re-import.
        UUID account = rows.get(0).id();
        jdbc.update("update account set balance = balance + 5.00 where id = ?", account);
        BigDecimal changed = balance(account);

        var second = importer.importFrom(SEED);
        assertThat(second.total()).isZero();
        assertThat(count("customer")).isEqualTo(102);
        assertThat(count("account")).isEqualTo(150);
        assertThat(count("transfer")).isEqualTo(101);
        assertThat(count("transaction_entry")).isEqualTo(1002);
        assertThat(balance(account)).isEqualByComparingTo(changed);
    }

    @Test
    void newEntriesSortAfterImportedHistory() {
        reset();
        importer.importFrom(SEED);
        Long next = jdbc.queryForObject("select nextval('transaction_entry_seq')", Long.class);
        assertThat(next).isGreaterThan(1002L);
    }
}
