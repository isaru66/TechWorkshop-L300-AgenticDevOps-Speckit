package com.workshop.banking.domain;

import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.math.BigDecimal;
import java.time.Instant;
import java.util.UUID;

@Entity
@Table(name = "transaction_entry")
public class TransactionEntry {

    @Id
    private UUID id;
    private long sequence;
    private UUID accountId;
    @Enumerated(EnumType.STRING)
    private EntryType type;
    private BigDecimal amount;
    private String currency;
    private Instant completedAt;
    private BigDecimal resultingBalance;
    private UUID transferId;
    private UUID counterpartyAccountId;

    protected TransactionEntry() {
    }

    public TransactionEntry(UUID id, long sequence, UUID accountId, EntryType type, BigDecimal amount,
                            String currency, Instant completedAt, BigDecimal resultingBalance,
                            UUID transferId, UUID counterpartyAccountId) {
        this.id = id;
        this.sequence = sequence;
        this.accountId = accountId;
        this.type = type;
        this.amount = amount;
        this.currency = currency;
        this.completedAt = completedAt;
        this.resultingBalance = resultingBalance;
        this.transferId = transferId;
        this.counterpartyAccountId = counterpartyAccountId;
    }

    public UUID getId() { return id; }
    public long getSequence() { return sequence; }
    public UUID getAccountId() { return accountId; }
    public EntryType getType() { return type; }
    public BigDecimal getAmount() { return amount; }
    public String getCurrency() { return currency; }
    public Instant getCompletedAt() { return completedAt; }
    public BigDecimal getResultingBalance() { return resultingBalance; }
    public UUID getTransferId() { return transferId; }
    public UUID getCounterpartyAccountId() { return counterpartyAccountId; }
}
