package com.workshop.banking.domain;

import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.Instant;
import java.time.LocalDate;
import java.util.UUID;

@Entity
@Table(name = "customer")
public class Customer {

    @Id
    private UUID id;
    private String name;
    private Instant createdAt;
    private String englishName;
    private String email;
    private String phone;
    private String addressLine;
    private String subdistrict;
    private String district;
    private String province;
    private String postcode;
    private LocalDate dateOfBirth;
    private String occupation;
    private String nationalId;

    protected Customer() {
    }

    public Customer(UUID id, String name, Instant createdAt) {
        this.id = id;
        this.name = name;
        this.createdAt = createdAt;
    }

    public UUID getId() { return id; }
    public String getName() { return name; }
    public Instant getCreatedAt() { return createdAt; }
    public String getEnglishName() { return englishName; }
    public void setEnglishName(String v) { this.englishName = v; }
    public String getEmail() { return email; }
    public void setEmail(String v) { this.email = v; }
    public String getPhone() { return phone; }
    public void setPhone(String v) { this.phone = v; }
    public String getAddressLine() { return addressLine; }
    public void setAddressLine(String v) { this.addressLine = v; }
    public String getSubdistrict() { return subdistrict; }
    public void setSubdistrict(String v) { this.subdistrict = v; }
    public String getDistrict() { return district; }
    public void setDistrict(String v) { this.district = v; }
    public String getProvince() { return province; }
    public void setProvince(String v) { this.province = v; }
    public String getPostcode() { return postcode; }
    public void setPostcode(String v) { this.postcode = v; }
    public LocalDate getDateOfBirth() { return dateOfBirth; }
    public void setDateOfBirth(LocalDate v) { this.dateOfBirth = v; }
    public String getOccupation() { return occupation; }
    public void setOccupation(String v) { this.occupation = v; }
    public String getNationalId() { return nationalId; }
    public void setNationalId(String v) { this.nationalId = v; }
}
