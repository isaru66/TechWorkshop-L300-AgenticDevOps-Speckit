package com.workshop.banking.seed;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

import com.workshop.banking.seed.SeedData.SeedException;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

class SeedValidationTest {

    private static final Path SEED = Path.of("../data/seed");

    @TempDir
    Path tmp;

    private Path copyWith(String file, String replaceFrom, String replaceTo) throws IOException {
        for (String f : new String[] {"customers.csv", "accounts.csv", "transfers.csv", "transaction_entries.csv"}) {
            Files.copy(SEED.resolve(f), tmp.resolve(f), StandardCopyOption.REPLACE_EXISTING);
        }
        Path target = tmp.resolve(file);
        Files.writeString(target, Files.readString(target).replace(replaceFrom, replaceTo));
        return tmp;
    }

    @Test
    void realSeedIsValid() {
        var data = SeedData.load(SEED);
        assertThat(data.customers()).hasSize(102);
        assertThat(data.accounts()).hasSize(150);
        assertThat(data.transfers()).hasSize(101);
        assertThat(data.entries()).hasSize(1002);
    }

    @Test
    void rejectsDuplicateId() throws IOException {
        String firstId = Files.readAllLines(SEED.resolve("accounts.csv")).get(1).split(",")[0];
        String secondId = Files.readAllLines(SEED.resolve("accounts.csv")).get(2).split(",")[0];
        Path dir = copyWith("accounts.csv", secondId, firstId);
        assertThatThrownBy(() -> SeedData.load(dir)).isInstanceOf(SeedException.class).hasMessageContaining("duplicate");
    }

    @Test
    void rejectsUnresolvedReference() throws IOException {
        String customerId = Files.readAllLines(SEED.resolve("accounts.csv")).get(1).split(",")[1];
        Path dir = copyWith("accounts.csv", customerId, "00000000-0000-0000-0000-000000000000");
        assertThatThrownBy(() -> SeedData.load(dir)).isInstanceOf(SeedException.class)
                .hasMessageContaining("does not resolve");
    }

    @Test
    void rejectsNonThbCurrency() throws IOException {
        Path dir = copyWith("accounts.csv", ",THB,", ",USD,");
        assertThatThrownBy(() -> SeedData.load(dir)).isInstanceOf(SeedException.class).hasMessageContaining("currency");
    }

    @Test
    void rejectsBadAmount() throws IOException {
        String line = Files.readAllLines(SEED.resolve("transfers.csv")).get(1);
        String amount = line.split(",")[3];
        Path dir = copyWith("transfers.csv", "," + amount + ",", ",12.345,");
        assertThatThrownBy(() -> SeedData.load(dir)).isInstanceOf(SeedException.class).hasMessageContaining("amount");
    }
}
