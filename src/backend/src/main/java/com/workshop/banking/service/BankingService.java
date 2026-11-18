package com.workshop.banking.service;

import com.workshop.banking.api.dto.ApiDtos.CustomerInput;
import com.workshop.banking.domain.Account;
import com.workshop.banking.domain.AccountStatus;
import com.workshop.banking.domain.Customer;
import com.workshop.banking.domain.EntryType;
import com.workshop.banking.domain.Transfer;
import com.workshop.banking.domain.TransactionEntry;
import com.workshop.banking.repository.AccountRepository;
import com.workshop.banking.repository.CustomerRepository;
import com.workshop.banking.repository.TransactionEntryRepository;
import com.workshop.banking.repository.TransferRepository;
import java.math.BigDecimal;
import java.time.Instant;
import java.time.LocalDate;
import java.util.List;
import java.util.UUID;
import java.util.regex.Pattern;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

/** All banking rules live here; controllers only delegate. */
@Service
public class BankingService {

    public static final String CURRENCY = "THB";
    private static final int MAX_PAGE_SIZE = 100;
    private static final int MAX_LABEL = 100;
    private static final Pattern EMAIL = Pattern.compile("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$");
    private static final Pattern AMOUNT = Pattern.compile("^\\d{1,17}(\\.\\d{1,2})?$");

    private final CustomerRepository customers;
    private final AccountRepository accounts;
    private final TransferRepository transfers;
    private final TransactionEntryRepository entries;
    private final JdbcTemplate jdbc;

    public BankingService(CustomerRepository customers, AccountRepository accounts, TransferRepository transfers,
                          TransactionEntryRepository entries, JdbcTemplate jdbc) {
        this.customers = customers;
        this.accounts = accounts;
        this.transfers = transfers;
        this.entries = entries;
        this.jdbc = jdbc;
    }

    // ---- reads -------------------------------------------------------------

    @Transactional(readOnly = true)
    public Page<Customer> listCustomers(int page, int size) {
        return customers.findAll(pageRequest(page, size, Sort.by("createdAt", "id")));
    }

    @Transactional(readOnly = true)
    public Customer getCustomer(UUID id) {
        return customers.findById(id).orElseThrow(() -> BankingException.notFound("Customer"));
    }

    @Transactional(readOnly = true)
    public List<Account> listCustomerAccounts(UUID customerId) {
        getCustomer(customerId);
        return accounts.findByCustomerIdOrderByCreatedAtAscIdAsc(customerId);
    }

    @Transactional(readOnly = true)
    public Page<Account> listAccounts(int page, int size) {
        return accounts.findAll(pageRequest(page, size, Sort.by("createdAt", "id")));
    }

    @Transactional(readOnly = true)
    public Account getAccount(UUID id) {
        return accounts.findById(id).orElseThrow(() -> BankingException.notFound("Account"));
    }

    @Transactional(readOnly = true)
    public Page<TransactionEntry> getHistory(UUID accountId, int page, int size) {
        getAccount(accountId);
        return entries.findHistory(accountId, pageRequest(page, size, Sort.unsorted()));
    }

    // ---- creates -----------------------------------------------------------

    @Transactional
    public Customer createCustomer(CustomerInput in) {
        validateCustomer(in);
        Customer c = new Customer(UUID.randomUUID(), in.name().trim(), Instant.now());
        applyCustomer(c, in);
        return customers.save(c);
    }

    @Transactional(readOnly = true)
    public Page<Customer> searchCustomers(String query, int page, int size) {
        if (query == null || query.isBlank()) {
            throw BankingException.invalid("query is required");
        }
        PageRequest pr = pageRequest(page, size, Sort.by("createdAt", "id"));
        String escaped = query.trim().toLowerCase().replace("\\", "\\\\").replace("%", "\\%").replace("_", "\\_");
        return customers.search("%" + escaped + "%", pr);
    }

    @Transactional
    public Customer updateCustomer(UUID id, CustomerInput in) {
        validateCustomer(in);
        Customer c = getCustomer(id);
        c.setName(in.name().trim());
        applyCustomer(c, in);
        return customers.save(c);
    }

    @Transactional
    public void deleteCustomer(UUID id) {
        Customer c = getCustomer(id);
        if (accounts.existsByCustomerId(id)) {
            throw BankingException.customerHasAccounts();
        }
        customers.delete(c);
    }

    @Transactional
    public Account openAccount(UUID customerId) {
        return openAccount(customerId, null);
    }

    @Transactional
    public Account openAccount(UUID customerId, String label) {
        getCustomer(customerId);
        Account account = new Account(UUID.randomUUID(), customerId, CURRENCY,
                BigDecimal.ZERO.setScale(2), Instant.now());
        account.setLabel(normalizeLabel(label));
        return accounts.save(account);
    }

    @Transactional
    public Account updateAccount(UUID accountId, String label) {
        Account account = getAccount(accountId);
        account.setLabel(normalizeLabel(label));
        return accounts.save(account);
    }

    @Transactional
    public Account closeAccount(UUID accountId) {
        Account account = lock(accountId);
        if (account.getStatus() == AccountStatus.CLOSED) {
            throw BankingException.accountClosed();
        }
        if (account.getBalance().signum() != 0) {
            throw BankingException.nonZeroBalance();
        }
        account.setStatus(AccountStatus.CLOSED);
        return accounts.save(account);
    }

    // ---- money movements ---------------------------------------------------

    @Transactional
    public TransactionEntry deposit(UUID accountId, String rawAmount) {
        BigDecimal amount = parseAmount(rawAmount);
        Account account = lockOpen(accountId);
        account.setBalance(account.getBalance().add(amount));
        return record(account, EntryType.DEPOSIT, amount, Instant.now(), null, null);
    }

    @Transactional
    public TransactionEntry withdraw(UUID accountId, String rawAmount) {
        BigDecimal amount = parseAmount(rawAmount);
        Account account = lockOpen(accountId);
        if (account.getBalance().compareTo(amount) < 0) {
            throw BankingException.insufficientFunds();
        }
        account.setBalance(account.getBalance().subtract(amount));
        return record(account, EntryType.WITHDRAWAL, amount, Instant.now(), null, null);
    }

    @Transactional
    public Transfer transfer(UUID sourceId, UUID destinationId, String rawAmount) {
        BigDecimal amount = parseAmount(rawAmount);
        if (sourceId.equals(destinationId)) {
            throw BankingException.sameAccount();
        }
        // Lock in a fixed order so opposing concurrent transfers cannot deadlock.
        boolean sourceFirst = sourceId.compareTo(destinationId) < 0;
        Account first = lock(sourceFirst ? sourceId : destinationId);
        Account second = lock(sourceFirst ? destinationId : sourceId);
        Account source = sourceFirst ? first : second;
        Account destination = sourceFirst ? second : first;

        if (source.getStatus() == AccountStatus.CLOSED || destination.getStatus() == AccountStatus.CLOSED) {
            throw BankingException.accountClosed();
        }
        if (!source.getCurrency().equals(destination.getCurrency())) {
            throw BankingException.currencyMismatch();
        }
        if (source.getBalance().compareTo(amount) < 0) {
            throw BankingException.insufficientFunds();
        }
        Instant now = Instant.now();
        source.setBalance(source.getBalance().subtract(amount));
        destination.setBalance(destination.getBalance().add(amount));
        Transfer transfer = transfers.saveAndFlush(new Transfer(UUID.randomUUID(), source.getId(),
                destination.getId(), amount, source.getCurrency(), now));
        record(source, EntryType.TRANSFER_OUT, amount, now, transfer.getId(), destination.getId());
        record(destination, EntryType.TRANSFER_IN, amount, now, transfer.getId(), source.getId());
        return transfer;
    }

    // ---- helpers -----------------------------------------------------------

    private Account lock(UUID id) {
        return accounts.findByIdForUpdate(id).orElseThrow(() -> BankingException.notFound("Account"));
    }

    private Account lockOpen(UUID id) {
        Account account = lock(id);
        if (account.getStatus() == AccountStatus.CLOSED) {
            throw BankingException.accountClosed();
        }
        return account;
    }

    private static void validateCustomer(CustomerInput in) {
        if (in.name() == null || in.name().isBlank()) {
            throw BankingException.invalid("name is required");
        }
        if (in.email() != null && !in.email().isBlank() && !EMAIL.matcher(in.email().trim()).matches()) {
            throw BankingException.invalid("email must be a valid email address");
        }
        if (in.dateOfBirth() != null && !in.dateOfBirth().isBefore(LocalDate.now())) {
            throw BankingException.invalid("dateOfBirth must be in the past");
        }
    }

    private static void applyCustomer(Customer c, CustomerInput in) {
        c.setEnglishName(in.englishName());
        c.setEmail(in.email());
        c.setPhone(in.phone());
        c.setAddressLine(in.addressLine());
        c.setSubdistrict(in.subdistrict());
        c.setDistrict(in.district());
        c.setProvince(in.province());
        c.setPostcode(in.postcode());
        c.setDateOfBirth(in.dateOfBirth());
        c.setOccupation(in.occupation());
        c.setNationalId(in.nationalId());
    }

    private static String normalizeLabel(String label) {
        if (label == null || label.isBlank()) {
            return null;
        }
        String trimmed = label.trim();
        if (trimmed.length() > MAX_LABEL) {
            throw BankingException.invalid("label must be at most " + MAX_LABEL + " characters");
        }
        return trimmed;
    }

    private TransactionEntry record(Account account, EntryType type, BigDecimal amount, Instant at,
                                    UUID transferId, UUID counterparty) {
        long seq = jdbc.queryForObject("select nextval('transaction_entry_seq')", Long.class);
        return entries.save(new TransactionEntry(UUID.randomUUID(), seq, account.getId(), type, amount,
                account.getCurrency(), at, account.getBalance(), transferId, counterparty));
    }

    static BigDecimal parseAmount(String raw) {
        if (raw == null || !AMOUNT.matcher(raw.trim()).matches()) {
            throw BankingException.invalid("amount must be a positive decimal with at most 2 decimal places");
        }
        BigDecimal value = new BigDecimal(raw.trim()).setScale(2);
        if (value.signum() <= 0) {
            throw BankingException.invalid("amount must be greater than zero");
        }
        return value;
    }

    private static PageRequest pageRequest(int page, int size, Sort sort) {
        if (page < 0) {
            throw BankingException.invalid("page must be 0 or greater");
        }
        if (size < 1 || size > MAX_PAGE_SIZE) {
            throw BankingException.invalid("size must be between 1 and " + MAX_PAGE_SIZE);
        }
        return PageRequest.of(page, size, sort);
    }
}
