package com.workshop.banking.seed;

import com.workshop.banking.seed.SeedData.AccountRow;
import com.workshop.banking.seed.SeedData.CustomerRow;
import com.workshop.banking.seed.SeedData.EntryRow;
import com.workshop.banking.seed.SeedData.TransferRow;
import java.nio.file.Path;
import java.sql.Date;
import java.sql.Timestamp;
import java.util.List;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

/**
 * Insert-only seed import. Rows whose UUID already exists are skipped, so repeat runs never duplicate or
 * overwrite later changes. Balances and history are loaded as recorded and never replayed.
 */
@Component
public class SeedImporter {

    private static final Logger log = LoggerFactory.getLogger(SeedImporter.class);

    public record Result(int customers, int accounts, int transfers, int entries) {
        public int total() {
            return customers + accounts + transfers + entries;
        }
    }

    private final JdbcTemplate jdbc;
    private final SeedProperties properties;

    public SeedImporter(JdbcTemplate jdbc, SeedProperties properties) {
        this.jdbc = jdbc;
        this.properties = properties;
    }

    public Result importConfigured() {
        return importFrom(Path.of(properties.path()).toAbsolutePath().normalize());
    }

    @Transactional
    public Result importFrom(Path dir) {
        // Parse and validate everything first so invalid data never reaches the database.
        SeedData data = SeedData.load(dir);

        int c = insert("customer", data.customers().size(), () -> jdbc.batchUpdate(
                "insert into customer (id, name, created_at, english_name, email, phone, address_line, "
                        + "subdistrict, district, province, postcode, date_of_birth, occupation, national_id) "
                        + "values (?,?,?,?,?,?,?,?,?,?,?,?,?,?) on conflict (id) do nothing",
                data.customers(), 500, (ps, r) -> bindCustomer(ps, r)));
        int a = insert("account", data.accounts().size(), () -> jdbc.batchUpdate(
                "insert into account (id, customer_id, currency, balance, created_at) "
                        + "values (?,?,?,?,?) on conflict (id) do nothing",
                data.accounts(), 500, (ps, r) -> {
                    ps.setObject(1, r.id());
                    ps.setObject(2, r.customerId());
                    ps.setString(3, r.currency());
                    ps.setBigDecimal(4, r.balance());
                    ps.setTimestamp(5, Timestamp.from(r.createdAt()));
                }));
        int t = insert("transfer", data.transfers().size(), () -> jdbc.batchUpdate(
                "insert into transfer (id, source_account_id, destination_account_id, amount, currency, "
                        + "completed_at) values (?,?,?,?,?,?) on conflict (id) do nothing",
                data.transfers(), 500, (ps, r) -> {
                    ps.setObject(1, r.id());
                    ps.setObject(2, r.source());
                    ps.setObject(3, r.destination());
                    ps.setBigDecimal(4, r.amount());
                    ps.setString(5, r.currency());
                    ps.setTimestamp(6, Timestamp.from(r.completedAt()));
                }));
        int e = insert("transaction_entry", data.entries().size(), () -> jdbc.batchUpdate(
                "insert into transaction_entry (id, sequence, account_id, type, amount, currency, completed_at, "
                        + "resulting_balance, transfer_id, counterparty_account_id) "
                        + "values (?,?,?,?,?,?,?,?,?,?) on conflict (id) do nothing",
                data.entries(), 500, (ps, r) -> {
                    ps.setObject(1, r.id());
                    ps.setLong(2, r.sequence());
                    ps.setObject(3, r.accountId());
                    ps.setString(4, r.type());
                    ps.setBigDecimal(5, r.amount());
                    ps.setString(6, r.currency());
                    ps.setTimestamp(7, Timestamp.from(r.completedAt()));
                    ps.setBigDecimal(8, r.resultingBalance());
                    ps.setObject(9, r.transferId());
                    ps.setObject(10, r.counterparty());
                }));

        // New entries must sort after every imported one.
        jdbc.queryForObject("select setval('transaction_entry_seq', greatest("
                + "(select coalesce(max(sequence), 1) from transaction_entry), "
                + "(select last_value from transaction_entry_seq)))", Long.class);

        Result result = new Result(c, a, t, e);
        log.info("Seed import finished: inserted customers={}, accounts={}, transfers={}, entries={} "
                        + "(existing rows skipped)",
                c, a, t, e);
        return result;
    }

    private int insert(String table, int fileRows, Runnable batch) {
        int before = jdbc.queryForObject("select count(*) from " + table, Integer.class);
        batch.run();
        int after = jdbc.queryForObject("select count(*) from " + table, Integer.class);
        int inserted = after - before;
        log.info("{}: {} in file, {} inserted, {} skipped", table, fileRows, inserted, fileRows - inserted);
        return inserted;
    }

    private static void bindCustomer(java.sql.PreparedStatement ps, CustomerRow r) throws java.sql.SQLException {
        ps.setObject(1, r.id());
        ps.setString(2, r.name());
        ps.setTimestamp(3, Timestamp.from(r.createdAt()));
        ps.setString(4, r.englishName());
        ps.setString(5, r.email());
        ps.setString(6, r.phone());
        ps.setString(7, r.addressLine());
        ps.setString(8, r.subdistrict());
        ps.setString(9, r.district());
        ps.setString(10, r.province());
        ps.setString(11, r.postcode());
        ps.setDate(12, r.dateOfBirth() == null ? null : Date.valueOf(r.dateOfBirth()));
        ps.setString(13, r.occupation());
        ps.setString(14, r.nationalId());
    }
}
