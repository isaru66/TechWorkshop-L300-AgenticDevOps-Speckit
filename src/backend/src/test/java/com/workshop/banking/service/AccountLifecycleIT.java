package com.workshop.banking.service;

import static org.assertj.core.api.Assertions.assertThat;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.patch;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.put;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

import com.workshop.banking.support.PostgresIntegrationTest;
import org.junit.jupiter.api.Test;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.ResultActions;
import tools.jackson.databind.json.JsonMapper;
import org.springframework.beans.factory.annotation.Autowired;

/** Close/delete rules and the REST endpoints added alongside the MCP server. */
class AccountLifecycleIT extends PostgresIntegrationTest {

    @Autowired
    JsonMapper json;

    private String create(String path, String body) throws Exception {
        String response = mvc.perform(post(path).contentType(MediaType.APPLICATION_JSON).content(body))
                .andExpect(status().isCreated()).andReturn().getResponse().getContentAsString();
        return json.readTree(response).get("id").asString();
    }

    private ResultActions amount(String path, String id, String amount) throws Exception {
        return mvc.perform(post(path, id).contentType(MediaType.APPLICATION_JSON)
                .content("{\"amount\":\"" + amount + "\"}"));
    }

    @Test
    void closeAndDeleteRulesOverRest() throws Exception {
        String customer = create("/api/customers", "{\"name\":\"Rest Lifecycle\"}");
        String account = create("/api/customers/" + customer + "/accounts", "{\"label\":\"Main\"}");

        mvc.perform(get("/api/accounts/{id}", account)).andExpect(status().isOk())
                .andExpect(jsonPath("$.status").value("OPEN")).andExpect(jsonPath("$.label").value("Main"));

        mvc.perform(patch("/api/accounts/{id}", account).contentType(MediaType.APPLICATION_JSON)
                        .content("{\"label\":\"Renamed\"}"))
                .andExpect(status().isOk()).andExpect(jsonPath("$.label").value("Renamed"));

        mvc.perform(put("/api/customers/{id}", customer).contentType(MediaType.APPLICATION_JSON)
                        .content("{\"name\":\"Rest Renamed\"}"))
                .andExpect(status().isOk()).andExpect(jsonPath("$.name").value("Rest Renamed"));
        mvc.perform(put("/api/customers/{id}", customer).contentType(MediaType.APPLICATION_JSON)
                        .content("{\"name\":\" \"}"))
                .andExpect(status().isBadRequest());

        amount("/api/accounts/{id}/deposits", account, "10.00").andExpect(status().isCreated());
        mvc.perform(post("/api/accounts/{id}/close", account)).andExpect(status().isConflict())
                .andExpect(jsonPath("$.code").value("NON_ZERO_BALANCE"));
        amount("/api/accounts/{id}/withdrawals", account, "10.00").andExpect(status().isCreated());

        mvc.perform(post("/api/accounts/{id}/close", account)).andExpect(status().isOk())
                .andExpect(jsonPath("$.status").value("CLOSED"));
        mvc.perform(post("/api/accounts/{id}/close", account)).andExpect(status().isConflict())
                .andExpect(jsonPath("$.code").value("ACCOUNT_CLOSED"));
        amount("/api/accounts/{id}/deposits", account, "1.00").andExpect(status().isConflict());
        amount("/api/accounts/{id}/withdrawals", account, "1.00").andExpect(status().isConflict());
        mvc.perform(get("/api/accounts/{id}/transactions", account)).andExpect(status().isOk())
                .andExpect(jsonPath("$.totalElements").value(2));

        mvc.perform(delete("/api/customers/{id}", customer)).andExpect(status().isConflict())
                .andExpect(jsonPath("$.code").value("CUSTOMER_HAS_ACCOUNTS"));

        String lonely = create("/api/customers", "{\"name\":\"No Accounts\"}");
        mvc.perform(delete("/api/customers/{id}", lonely)).andExpect(status().isNoContent());
        mvc.perform(get("/api/customers/{id}", lonely)).andExpect(status().isNotFound());
        mvc.perform(delete("/api/customers/{id}", lonely)).andExpect(status().isNotFound());
        mvc.perform(post("/api/accounts/{id}/close", java.util.UUID.randomUUID())).andExpect(status().isNotFound());
    }

    @Test
    void existingOpenAccountEndpointStillWorksWithoutBody() throws Exception {
        String customer = create("/api/customers", "{\"name\":\"No Body\"}");
        mvc.perform(post("/api/customers/{id}/accounts", customer)).andExpect(status().isCreated())
                .andExpect(jsonPath("$.balance").value("0.00")).andExpect(jsonPath("$.status").value("OPEN"));
    }
}
