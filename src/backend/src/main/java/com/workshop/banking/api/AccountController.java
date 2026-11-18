package com.workshop.banking.api;

import com.workshop.banking.api.dto.ApiDtos.AccountResponse;
import com.workshop.banking.api.dto.ApiDtos.AccountUpdate;
import com.workshop.banking.api.dto.ApiDtos.AmountRequest;
import com.workshop.banking.api.dto.ApiDtos.BalanceResponse;
import com.workshop.banking.api.dto.ApiDtos.PageResponse;
import com.workshop.banking.api.dto.ApiDtos.TransactionEntryResponse;
import com.workshop.banking.service.BankingService;
import jakarta.validation.Valid;
import java.util.UUID;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/accounts")
public class AccountController {

    private final BankingService service;

    public AccountController(BankingService service) {
        this.service = service;
    }

    @GetMapping
    public PageResponse<AccountResponse> list(@RequestParam(defaultValue = "0") int page,
                                              @RequestParam(defaultValue = "20") int size) {
        return PageResponse.of(service.listAccounts(page, size), AccountResponse::from);
    }

    @GetMapping("/{accountId}")
    public AccountResponse get(@PathVariable UUID accountId) {
        return AccountResponse.from(service.getAccount(accountId));
    }

    @GetMapping("/{accountId}/balance")
    public BalanceResponse balance(@PathVariable UUID accountId) {
        return BalanceResponse.from(service.getAccount(accountId));
    }

    @PostMapping("/{accountId}/deposits")
    @ResponseStatus(HttpStatus.CREATED)
    public TransactionEntryResponse deposit(@PathVariable UUID accountId, @Valid @RequestBody AmountRequest body) {
        return TransactionEntryResponse.from(service.deposit(accountId, body.amount()));
    }

    @PostMapping("/{accountId}/withdrawals")
    @ResponseStatus(HttpStatus.CREATED)
    public TransactionEntryResponse withdraw(@PathVariable UUID accountId, @Valid @RequestBody AmountRequest body) {
        return TransactionEntryResponse.from(service.withdraw(accountId, body.amount()));
    }

    @PatchMapping("/{accountId}")
    public AccountResponse update(@PathVariable UUID accountId, @RequestBody AccountUpdate body) {
        return AccountResponse.from(service.updateAccount(accountId, body.label()));
    }

    @PostMapping("/{accountId}/close")
    public AccountResponse close(@PathVariable UUID accountId) {
        return AccountResponse.from(service.closeAccount(accountId));
    }

    @GetMapping("/{accountId}/transactions")
    public PageResponse<TransactionEntryResponse> history(@PathVariable UUID accountId,
                                                          @RequestParam(defaultValue = "0") int page,
                                                          @RequestParam(defaultValue = "20") int size) {
        return PageResponse.of(service.getHistory(accountId, page, size), TransactionEntryResponse::from);
    }
}
