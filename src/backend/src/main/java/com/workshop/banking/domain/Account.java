package com.workshop.banking.domain;

import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.math.BigDecimal;
import java.time.Instant;
import java.util.UUID;

@Entity
@Table(name = "account")
public class Account {

    @Id
    private UUID id;
    private UUID customerId;
    private String currency;
    private BigDecimal balance;
    private Instant createdAt;

    protected Account() {
    }

    public Account(UUID id, UUID customerId, String currency, BigDecimal balance, Instant createdAt) {
        this.id = id;
        this.customerId = customerId;
        this.currency = currency;
        this.balance = balance;
        this.createdAt = createdAt;
    }

    public UUID getId() { return id; }
    public UUID getCustomerId() { return customerId; }
    public String getCurrency() { return currency; }
    public BigDecimal getBalance() { return balance; }
    public void setBalance(BigDecimal balance) { this.balance = balance; }
    public Instant getCreatedAt() { return createdAt; }
}
