package com.workshop.banking.api;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.workshop.banking.support.PostgresIntegrationTest;
import java.math.BigDecimal;
import java.util.Map;
import java.util.UUID;
import org.junit.jupiter.api.Test;
import org.springframework.http.MediaType;

class BrowseAndCreateApiIT extends PostgresIntegrationTest {

    @Test
    void customerReturnsFullRecordAndAccountsMatchSeed() throws Exception {
        Map<String, Object> row = jdbc.queryForMap(
                "select c.id, c.national_id, c.email from customer c join account a on a.customer_id = c.id "
                        + "order by c.id limit 1");
        UUID id = (UUID) row.get("id");
        int expectedAccounts = jdbc.queryForObject("select count(*) from account where customer_id = ?",
                Integer.class, id);

        mvc.perform(get("/api/customers/{id}", id))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(id.toString()))
                .andExpect(jsonPath("$.nationalId").value(row.get("national_id")))
                .andExpect(jsonPath("$.email").value(row.get("email")));
        mvc.perform(get("/api/customers/{id}/accounts", id))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(expectedAccounts));
    }

    @Test
    void balanceMatchesStoredValueAsTwoDecimalString() throws Exception {
        UUID account = pickAccounts("1.00").get(0);
        mvc.perform(get("/api/accounts/{id}/balance", account))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.currency").value("THB"))
                .andExpect(jsonPath("$.balance").value(balance(account).setScale(2).toPlainString()));
    }

    @Test
    void unknownIdsReturnNotFoundAndBadPagingReturnsBadRequest() throws Exception {
        UUID unknown = UUID.randomUUID();
        mvc.perform(get("/api/customers/{id}", unknown)).andExpect(status().isNotFound())
                .andExpect(jsonPath("$.code").value("NOT_FOUND"));
        mvc.perform(get("/api/accounts/{id}/balance", unknown)).andExpect(status().isNotFound());
        mvc.perform(get("/api/accounts?size=101")).andExpect(status().isBadRequest());
        mvc.perform(get("/api/accounts?size=0")).andExpect(status().isBadRequest());
        mvc.perform(get("/api/customers?page=0&size=5")).andExpect(status().isOk())
                .andExpect(jsonPath("$.items.length()").value(5))
                .andExpect(jsonPath("$.totalElements").isNumber());
    }

    @Test
    void createCustomerThenOpenAccount() throws Exception {
        String body = "{\"name\":\"Demo Person\",\"email\":\"demo.person@example.com\",\"nationalId\":\"DEMO-NID-9999\"}";
        String created = mvc.perform(post("/api/customers").contentType(MediaType.APPLICATION_JSON).content(body))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.id").isNotEmpty())
                .andReturn().getResponse().getContentAsString();
        String id = created.replaceAll(".*\"id\":\"([^\"]+)\".*", "$1");

        mvc.perform(get("/api/customers/{id}", id)).andExpect(status().isOk());
        mvc.perform(post("/api/customers/{id}/accounts", id))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.currency").value("THB"))
                .andExpect(jsonPath("$.balance").value("0.00"));

        UUID account = jdbc.queryForObject("select id from account where customer_id = ?::uuid", UUID.class, id);
        assertThat(balance(account)).isEqualByComparingTo(BigDecimal.ZERO);
        assertThat(history(account)).isEmpty();
        mvc.perform(get("/api/accounts/{id}/transactions", account))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.items.length()").value(0));
    }

    @Test
    void invalidCustomerAndMissingCustomerAreRejected() throws Exception {
        mvc.perform(post("/api/customers").contentType(MediaType.APPLICATION_JSON).content("{\"name\":\"\"}"))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.code").value("VALIDATION_ERROR"));
        mvc.perform(post("/api/customers").contentType(MediaType.APPLICATION_JSON)
                        .content("{\"name\":\"X\",\"email\":\"not-an-email\"}"))
                .andExpect(status().isBadRequest());
        int before = jdbc.queryForObject("select count(*) from account", Integer.class);
        mvc.perform(post("/api/customers/{id}/accounts", UUID.randomUUID())).andExpect(status().isNotFound());
        assertThat(jdbc.queryForObject("select count(*) from account", Integer.class)).isEqualTo(before);
    }
}
