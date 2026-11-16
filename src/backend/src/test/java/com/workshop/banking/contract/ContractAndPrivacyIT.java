package com.workshop.banking.contract;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.workshop.banking.support.PostgresIntegrationTest;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Value;

class ContractAndPrivacyIT extends PostgresIntegrationTest {

    @Value("${server.address}")
    String serverAddress;

    @Test
    void generatedOpenApiContainsEveryDesignedOperation() throws Exception {
        String doc = mvc.perform(get("/v3/api-docs")).andExpect(status().isOk())
                .andReturn().getResponse().getContentAsString();
        List<String> paths = List.of("/api/customers", "/api/customers/{customerId}",
                "/api/customers/{customerId}/accounts", "/api/accounts", "/api/accounts/{accountId}",
                "/api/accounts/{accountId}/balance", "/api/accounts/{accountId}/deposits",
                "/api/accounts/{accountId}/withdrawals", "/api/accounts/{accountId}/transactions", "/api/transfers");
        for (String p : paths) {
            assertThat(doc).contains("\"" + p + "\"");
        }
        String contract = Files.readString(Path.of("../../specs/001-local-banking-api/contracts/openapi.yaml"));
        for (String p : paths) {
            assertThat(contract).contains("  " + p + ":");
        }
    }

    @Test
    void swaggerUiIsAvailableLocally() throws Exception {
        mvc.perform(get("/swagger-ui/index.html")).andExpect(status().isOk());
    }

    @Test
    void serverIsLoopbackOnlyAndHasNoSecurityLayer() {
        assertThat(serverAddress).isEqualTo("127.0.0.1");
        assertThat(isPresent("org.springframework.security.core.Authentication")).isFalse();
    }

    private static boolean isPresent(String cls) {
        try {
            Class.forName(cls);
            return true;
        } catch (ClassNotFoundException e) {
            return false;
        }
    }
}
