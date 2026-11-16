package com.workshop.banking.api;

import com.workshop.banking.api.dto.ApiDtos.TransferRequest;
import com.workshop.banking.api.dto.ApiDtos.TransferResponse;
import com.workshop.banking.service.BankingService;
import jakarta.validation.Valid;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/transfers")
public class TransferController {

    private final BankingService service;

    public TransferController(BankingService service) {
        this.service = service;
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public TransferResponse transfer(@Valid @RequestBody TransferRequest body) {
        return TransferResponse.from(
                service.transfer(body.sourceAccountId(), body.destinationAccountId(), body.amount()));
    }
}
