package com.workshop.banking.repository;

import com.workshop.banking.domain.TransactionEntry;
import java.util.UUID;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

public interface TransactionEntryRepository extends JpaRepository<TransactionEntry, UUID> {

    @Query(value = "select e from TransactionEntry e where e.accountId = :accountId "
            + "order by e.completedAt desc, e.sequence desc",
            countQuery = "select count(e) from TransactionEntry e where e.accountId = :accountId")
    Page<TransactionEntry> findHistory(@Param("accountId") UUID accountId, Pageable pageable);
}
