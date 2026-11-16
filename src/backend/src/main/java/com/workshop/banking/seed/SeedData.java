package com.workshop.banking.seed;

import java.io.IOException;
import java.math.BigDecimal;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.Instant;
import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.UUID;
import java.util.regex.Pattern;

/** Parsed and validated seed baseline. Loading fails with {@link SeedException} on any inconsistency. */
public record SeedData(List<CustomerRow> customers, List<AccountRow> accounts, List<TransferRow> transfers,
                       List<EntryRow> entries) {

    public record CustomerRow(UUID id, String name, Instant createdAt, String englishName, String email,
                              String phone, String addressLine, String subdistrict, String district,
                              String province, String postcode, LocalDate dateOfBirth, String occupation,
                              String nationalId) {
    }

    public record AccountRow(UUID id, UUID customerId, String currency, BigDecimal balance, Instant createdAt) {
    }

    public record TransferRow(UUID id, UUID source, UUID destination, BigDecimal amount, String currency,
                              Instant completedAt) {
    }

    public record EntryRow(UUID id, long sequence, UUID accountId, String type, BigDecimal amount,
                           String currency, Instant completedAt, BigDecimal resultingBalance, UUID transferId,
                           UUID counterparty) {
    }

    public static class SeedException extends RuntimeException {
        public SeedException(String message) {
            super(message);
        }
    }

    private static final Pattern TS = Pattern.compile("^(\\d{4}-\\d{2}-\\d{2}) (\\S+?)([+-]\\d{2})(?::?(\\d{2}))?$");
    private static final Set<String> TYPES = Set.of("DEPOSIT", "WITHDRAWAL", "TRANSFER_IN", "TRANSFER_OUT");

    public static SeedData load(Path dir) {
        List<CustomerRow> customers = new ArrayList<>();
        for (var r : read(dir, "customers.csv")) {
            customers.add(new CustomerRow(uuid(r, "id"), req(r, "name"), ts(r.get("created_at")),
                    opt(r, "english_name"), opt(r, "email"), opt(r, "phone"), opt(r, "address_line"),
                    opt(r, "subdistrict"), opt(r, "district"), opt(r, "province"), opt(r, "postcode"),
                    opt(r, "date_of_birth") == null ? null : LocalDate.parse(r.get("date_of_birth")),
                    opt(r, "occupation"), opt(r, "national_id")));
        }
        List<AccountRow> accounts = new ArrayList<>();
        for (var r : read(dir, "accounts.csv")) {
            accounts.add(new AccountRow(uuid(r, "id"), uuid(r, "customer_id"), currency(r),
                    money(r, "balance", true), ts(r.get("created_at"))));
        }
        List<TransferRow> transfers = new ArrayList<>();
        for (var r : read(dir, "transfers.csv")) {
            transfers.add(new TransferRow(uuid(r, "id"), uuid(r, "source_account_id"),
                    uuid(r, "destination_account_id"), money(r, "amount", false), currency(r),
                    ts(r.get("completed_at"))));
        }
        List<EntryRow> entries = new ArrayList<>();
        for (var r : read(dir, "transaction_entries.csv")) {
            String type = req(r, "type");
            if (!TYPES.contains(type)) {
                throw new SeedException("transaction_entries.csv: unknown type " + type);
            }
            entries.add(new EntryRow(uuid(r, "id"), (long) Double.parseDouble(req(r, "sequence")),
                    uuid(r, "account_id"), type, money(r, "amount", false), currency(r),
                    ts(r.get("completed_at")), money(r, "resulting_balance", true),
                    optUuid(r, "transfer_id"), optUuid(r, "counterparty_account_id")));
        }
        SeedData data = new SeedData(customers, accounts, transfers, entries);
        data.validate();
        return data;
    }

    private void validate() {
        Set<UUID> customerIds = unique("customers.csv", customers.stream().map(CustomerRow::id).toList());
        Set<UUID> accountIds = unique("accounts.csv", accounts.stream().map(AccountRow::id).toList());
        Set<UUID> transferIds = unique("transfers.csv", transfers.stream().map(TransferRow::id).toList());
        unique("transaction_entries.csv", entries.stream().map(EntryRow::id).toList());
        unique("transaction_entries.csv (sequence)", entries.stream().map(EntryRow::sequence).toList());

        for (AccountRow a : accounts) {
            ref("accounts.csv customer_id", a.customerId(), customerIds);
        }
        for (TransferRow t : transfers) {
            ref("transfers.csv source_account_id", t.source(), accountIds);
            ref("transfers.csv destination_account_id", t.destination(), accountIds);
        }
        for (EntryRow e : entries) {
            ref("transaction_entries.csv account_id", e.accountId(), accountIds);
            if (e.transferId() != null) {
                ref("transaction_entries.csv transfer_id", e.transferId(), transferIds);
            }
            if (e.counterparty() != null) {
                ref("transaction_entries.csv counterparty_account_id", e.counterparty(), accountIds);
            }
        }
    }

    private static <T> Set<T> unique(String file, List<T> ids) {
        Set<T> set = new HashSet<>();
        for (T id : ids) {
            if (!set.add(id)) {
                throw new SeedException(file + ": duplicate id " + id);
            }
        }
        return set;
    }

    private static void ref(String what, UUID id, Set<UUID> known) {
        if (!known.contains(id)) {
            throw new SeedException(what + " does not resolve: " + id);
        }
    }

    // ---- CSV / value parsing ---------------------------------------------

    private static List<java.util.Map<String, String>> read(Path dir, String file) {
        Path path = dir.resolve(file);
        if (!Files.isRegularFile(path)) {
            throw new SeedException("Seed file not found: " + path);
        }
        try {
            List<List<String>> rows = parseCsv(Files.readString(path, StandardCharsets.UTF_8));
            if (rows.isEmpty()) {
                throw new SeedException(file + " is empty");
            }
            List<String> header = rows.get(0);
            List<java.util.Map<String, String>> out = new ArrayList<>();
            for (int i = 1; i < rows.size(); i++) {
                List<String> row = rows.get(i);
                if (row.size() != header.size()) {
                    throw new SeedException(file + " line " + (i + 1) + ": expected " + header.size()
                            + " columns but found " + row.size());
                }
                var map = new java.util.LinkedHashMap<String, String>();
                for (int c = 0; c < header.size(); c++) {
                    map.put(header.get(c).trim(), row.get(c));
                }
                out.add(map);
            }
            return out;
        } catch (IOException e) {
            throw new SeedException("Cannot read " + path + ": " + e.getMessage());
        }
    }

    static List<List<String>> parseCsv(String text) {
        if (text.startsWith("\uFEFF")) {
            text = text.substring(1);
        }
        List<List<String>> rows = new ArrayList<>();
        List<String> row = new ArrayList<>();
        StringBuilder field = new StringBuilder();
        boolean quoted = false;
        for (int i = 0; i < text.length(); i++) {
            char ch = text.charAt(i);
            if (quoted) {
                if (ch == '"') {
                    if (i + 1 < text.length() && text.charAt(i + 1) == '"') {
                        field.append('"');
                        i++;
                    } else {
                        quoted = false;
                    }
                } else {
                    field.append(ch);
                }
            } else if (ch == '"') {
                quoted = true;
            } else if (ch == ',') {
                row.add(field.toString());
                field.setLength(0);
            } else if (ch == '\n' || ch == '\r') {
                if (ch == '\r' && i + 1 < text.length() && text.charAt(i + 1) == '\n') {
                    i++;
                }
                row.add(field.toString());
                field.setLength(0);
                if (!(row.size() == 1 && row.get(0).isEmpty())) {
                    rows.add(row);
                }
                row = new ArrayList<>();
            } else {
                field.append(ch);
            }
        }
        if (field.length() > 0 || !row.isEmpty()) {
            row.add(field.toString());
            rows.add(row);
        }
        return rows;
    }

    private static String req(java.util.Map<String, String> r, String key) {
        String v = r.get(key);
        if (v == null || v.isBlank()) {
            throw new SeedException("Missing required value: " + key);
        }
        return v;
    }

    private static String opt(java.util.Map<String, String> r, String key) {
        String v = r.get(key);
        return v == null || v.isBlank() ? null : v;
    }

    private static UUID uuid(java.util.Map<String, String> r, String key) {
        try {
            return UUID.fromString(req(r, key).trim());
        } catch (IllegalArgumentException e) {
            throw new SeedException("Invalid UUID in " + key + ": " + r.get(key));
        }
    }

    private static UUID optUuid(java.util.Map<String, String> r, String key) {
        return opt(r, key) == null ? null : uuid(r, key);
    }

    private static String currency(java.util.Map<String, String> r) {
        String c = req(r, "currency");
        if (!"THB".equals(c)) {
            throw new SeedException("Unsupported currency " + c + " (only THB is allowed)");
        }
        return c;
    }

    private static BigDecimal money(java.util.Map<String, String> r, String key, boolean allowZero) {
        String raw = req(r, key).trim();
        BigDecimal v;
        try {
            v = new BigDecimal(raw);
        } catch (NumberFormatException e) {
            throw new SeedException("Invalid amount in " + key + ": " + raw);
        }
        if (v.scale() > 2 || v.signum() < 0 || (!allowZero && v.signum() == 0)) {
            throw new SeedException("Invalid amount in " + key + ": " + raw);
        }
        return v.setScale(2);
    }

    static Instant ts(String raw) {
        if (raw == null || raw.isBlank()) {
            throw new SeedException("Missing timestamp");
        }
        var m = TS.matcher(raw.trim());
        if (!m.matches()) {
            throw new SeedException("Invalid timestamp: " + raw);
        }
        String offset = m.group(3) + ":" + (m.group(4) == null ? "00" : m.group(4));
        return OffsetDateTime.parse(m.group(1) + "T" + m.group(2) + offset).toInstant();
    }
}
