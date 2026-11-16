package com.workshop.banking.api.dto;

import com.workshop.banking.domain.Account;
import com.workshop.banking.domain.Customer;
import com.workshop.banking.domain.Transfer;
import com.workshop.banking.domain.TransactionEntry;
import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Past;
import java.math.BigDecimal;
import java.time.Instant;
import java.time.LocalDate;
import java.util.List;
import java.util.UUID;
import org.springframework.data.domain.Page;

/** Request and response shapes matching contracts/openapi.yaml. Money is always a 2-decimal string. */
public final class ApiDtos {

    private ApiDtos() {
    }

    public static String money(BigDecimal value) {
        return value.setScale(2).toPlainString();
    }

    public record PageResponse<T>(int page, int size, long totalElements, int totalPages, List<T> items) {
        public static <S, T> PageResponse<T> of(Page<S> page, java.util.function.Function<S, T> mapper) {
            return new PageResponse<>(page.getNumber(), page.getSize(), page.getTotalElements(),
                    page.getTotalPages(), page.getContent().stream().map(mapper).toList());
        }
    }

    public record ApiError(String code, String message, List<FieldError> fieldErrors) {
    }

    public record FieldError(String field, String message) {
    }

    public record CustomerInput(
            @NotBlank String name,
            String englishName,
            @Email String email,
            String phone,
            String addressLine,
            String subdistrict,
            String district,
            String province,
            String postcode,
            @Past LocalDate dateOfBirth,
            String occupation,
            String nationalId) {
    }

    public record CustomerResponse(
            UUID id, String name, Instant createdAt, String englishName, String email, String phone,
            String addressLine, String subdistrict, String district, String province, String postcode,
            LocalDate dateOfBirth, String occupation, String nationalId) {
        public static CustomerResponse from(Customer c) {
            return new CustomerResponse(c.getId(), c.getName(), c.getCreatedAt(), c.getEnglishName(),
                    c.getEmail(), c.getPhone(), c.getAddressLine(), c.getSubdistrict(), c.getDistrict(),
                    c.getProvince(), c.getPostcode(), c.getDateOfBirth(), c.getOccupation(), c.getNationalId());
        }
    }

    public record AccountResponse(UUID id, UUID customerId, String currency, String balance, Instant createdAt) {
        public static AccountResponse from(Account a) {
            return new AccountResponse(a.getId(), a.getCustomerId(), a.getCurrency(), money(a.getBalance()),
                    a.getCreatedAt());
        }
    }

    public record BalanceResponse(UUID accountId, String currency, String balance) {
        public static BalanceResponse from(Account a) {
            return new BalanceResponse(a.getId(), a.getCurrency(), money(a.getBalance()));
        }
    }

    public record AmountRequest(@NotNull String amount) {
    }

    public record TransferRequest(@NotNull UUID sourceAccountId, @NotNull UUID destinationAccountId,
                                  @NotNull String amount) {
    }

    public record TransferResponse(UUID id, UUID sourceAccountId, UUID destinationAccountId, String amount,
                                   String currency, Instant completedAt) {
        public static TransferResponse from(Transfer t) {
            return new TransferResponse(t.getId(), t.getSourceAccountId(), t.getDestinationAccountId(),
                    money(t.getAmount()), t.getCurrency(), t.getCompletedAt());
        }
    }

    public record TransactionEntryResponse(UUID id, long sequence, UUID accountId, String type, String amount,
                                           String currency, Instant completedAt, String resultingBalance,
                                           UUID transferId, UUID counterpartyAccountId) {
        public static TransactionEntryResponse from(TransactionEntry e) {
            return new TransactionEntryResponse(e.getId(), e.getSequence(), e.getAccountId(),
                    e.getType().name(), money(e.getAmount()), e.getCurrency(), e.getCompletedAt(),
                    money(e.getResultingBalance()), e.getTransferId(), e.getCounterpartyAccountId());
        }
    }
}
