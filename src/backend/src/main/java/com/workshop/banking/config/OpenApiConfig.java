package com.workshop.banking.config;

import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.info.Info;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class OpenApiConfig {

    @Bean
    OpenAPI bankingOpenApi() {
        return new OpenAPI().info(new Info()
                .title("Local Banking API")
                .description("Fictional, private, local-only banking API. No authentication. THB only.")
                .version("1.0.0"));
    }
}
