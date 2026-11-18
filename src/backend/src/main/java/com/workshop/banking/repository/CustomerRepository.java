package com.workshop.banking.repository;

import com.workshop.banking.domain.Customer;
import java.util.UUID;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

public interface CustomerRepository extends JpaRepository<Customer, UUID> {

    @Query("select c from Customer c where lower(c.name) like :pattern or lower(c.englishName) like :pattern "
            + "or lower(c.email) like :pattern or lower(c.phone) like :pattern")
    Page<Customer> search(@Param("pattern") String pattern, Pageable pageable);
}
