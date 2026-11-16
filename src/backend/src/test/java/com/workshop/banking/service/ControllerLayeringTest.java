package com.workshop.banking.service;

import static org.assertj.core.api.Assertions.assertThat;

import java.nio.file.Files;
import java.nio.file.Path;
import java.util.stream.Stream;
import org.junit.jupiter.api.Test;

class ControllerLayeringTest {

    @Test
    void controllersOnlyDelegateToBankingService() throws Exception {
        try (Stream<Path> files = Files.list(Path.of("src/main/java/com/workshop/banking/api"))) {
            files.filter(p -> p.getFileName().toString().endsWith("Controller.java")).forEach(p -> {
                try {
                    String src = Files.readString(p);
                    assertThat(src).as(p.toString()).doesNotContain("repository.").doesNotContain("Repository");
                    assertThat(src).as(p.toString()).contains("BankingService");
                } catch (Exception e) {
                    throw new IllegalStateException(e);
                }
            });
        }
    }
}
