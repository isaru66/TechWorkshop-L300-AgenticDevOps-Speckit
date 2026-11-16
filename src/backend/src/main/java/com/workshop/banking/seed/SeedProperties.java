package com.workshop.banking.seed;

import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "banking.seed")
public record SeedProperties(String path, Boolean enabled) {

    public SeedProperties {
        if (path == null || path.isBlank()) {
            path = "../data/seed";
        }
        if (enabled == null) {
            enabled = true;
        }
    }
}
