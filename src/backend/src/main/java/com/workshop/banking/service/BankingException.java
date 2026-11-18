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

    public static BankingException accountClosed() {
        return new BankingException("ACCOUNT_CLOSED", "Account is closed");
    }

    public static BankingException nonZeroBalance() {
        return new BankingException("NON_ZERO_BALANCE", "Account balance must be zero to close it");
    }

    public static BankingException customerHasAccounts() {
        return new BankingException("CUSTOMER_HAS_ACCOUNTS", "Customer still has accounts and cannot be deleted");
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
