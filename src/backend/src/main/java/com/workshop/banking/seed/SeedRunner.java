package com.workshop.banking.seed;

import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.stereotype.Component;

/** Runs the insert-only seed import on every backend start. */
@Component
public class SeedRunner implements ApplicationRunner {

    private final SeedImporter importer;
    private final SeedProperties properties;

    public SeedRunner(SeedImporter importer, SeedProperties properties) {
        this.importer = importer;
        this.properties = properties;
    }

    @Override
    public void run(ApplicationArguments args) {
        if (properties.enabled()) {
            importer.importConfigured();
        }
    }
}
