package com.workshop.banking.service;

public class BankingException extends RuntimeException {

    private final String code;

    public BankingException(String code, String message) {
        super(message);
        this.code = code;
    }

    public String getCode() {
        return code;
    }

    public static BankingException notFound(String what) {
        return new BankingException("NOT_FOUND", what + " not found");
    }

    public static BankingException invalid(String message) {
        return new BankingException("VALIDATION_ERROR", message);
    }

    public static BankingException insufficientFunds() {
        return new BankingException("INSUFFICIENT_FUNDS", "Insufficient funds");
    }

    public static BankingException sameAccount() {
        return new BankingException("SAME_ACCOUNT", "Source and destination accounts must differ");
    }

    public static BankingException currencyMismatch() {
        return new BankingException("CURRENCY_MISMATCH", "Accounts must have the same currency");
    }
}
