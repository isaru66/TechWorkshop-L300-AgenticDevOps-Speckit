package com.workshop.banking.api;

import com.workshop.banking.api.dto.ApiDtos.AccountResponse;
import com.workshop.banking.api.dto.ApiDtos.CustomerInput;
import com.workshop.banking.api.dto.ApiDtos.CustomerResponse;
import com.workshop.banking.api.dto.ApiDtos.OpenAccountRequest;
import com.workshop.banking.api.dto.ApiDtos.PageResponse;
import com.workshop.banking.service.BankingService;
import jakarta.validation.Valid;
import java.util.List;
import java.util.UUID;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/customers")
public class CustomerController {

    private final BankingService service;

    public CustomerController(BankingService service) {
        this.service = service;
    }

    @GetMapping
    public PageResponse<CustomerResponse> list(@RequestParam(defaultValue = "0") int page,
                                               @RequestParam(defaultValue = "20") int size) {
        return PageResponse.of(service.listCustomers(page, size), CustomerResponse::from);
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public CustomerResponse create(@Valid @RequestBody CustomerInput input) {
        return CustomerResponse.from(service.createCustomer(input));
    }

    @GetMapping("/{customerId}")
    public CustomerResponse get(@PathVariable UUID customerId) {
        return CustomerResponse.from(service.getCustomer(customerId));
    }

    @GetMapping("/{customerId}/accounts")
    public List<AccountResponse> accounts(@PathVariable UUID customerId) {
        return service.listCustomerAccounts(customerId).stream().map(AccountResponse::from).toList();
    }

    @PostMapping("/{customerId}/accounts")
    @ResponseStatus(HttpStatus.CREATED)
    public AccountResponse openAccount(@PathVariable UUID customerId,
                                       @RequestBody(required = false) OpenAccountRequest body) {
        return AccountResponse.from(service.openAccount(customerId, body == null ? null : body.label()));
    }

    @PutMapping("/{customerId}")
    public CustomerResponse update(@PathVariable UUID customerId, @Valid @RequestBody CustomerInput input) {
        return CustomerResponse.from(service.updateCustomer(customerId, input));
    }

    @DeleteMapping("/{customerId}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable UUID customerId) {
        service.deleteCustomer(customerId);
    }
}
