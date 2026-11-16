CREATE TABLE customer (
    id             uuid PRIMARY KEY,
    name           text        NOT NULL,
    created_at     timestamptz NOT NULL,
    english_name   text,
    email          text,
    phone          text,
    address_line   text,
    subdistrict    text,
    district       text,
    province       text,
    postcode       text,
    date_of_birth  date,
    occupation     text,
    national_id    text
);

CREATE TABLE account (
    id          uuid PRIMARY KEY,
    customer_id uuid          NOT NULL REFERENCES customer (id),
    currency    varchar(3)       NOT NULL CHECK (currency = 'THB'),
    balance     numeric(19,2) NOT NULL CHECK (balance >= 0),
    created_at  timestamptz   NOT NULL
);
CREATE INDEX idx_account_customer ON account (customer_id);

CREATE TABLE transfer (
    id                     uuid PRIMARY KEY,
    source_account_id      uuid          NOT NULL REFERENCES account (id),
    destination_account_id uuid          NOT NULL REFERENCES account (id),
    amount                 numeric(19,2) NOT NULL CHECK (amount > 0),
    currency               varchar(3)       NOT NULL,
    completed_at           timestamptz   NOT NULL,
    CHECK (source_account_id <> destination_account_id)
);

CREATE SEQUENCE transaction_entry_seq START WITH 1;

CREATE TABLE transaction_entry (
    id                      uuid PRIMARY KEY,
    sequence                bigint        NOT NULL UNIQUE,
    account_id              uuid          NOT NULL REFERENCES account (id),
    type                    varchar(16)   NOT NULL CHECK (type IN ('DEPOSIT', 'WITHDRAWAL', 'TRANSFER_IN', 'TRANSFER_OUT')),
    amount                  numeric(19,2) NOT NULL CHECK (amount > 0),
    currency                varchar(3)       NOT NULL,
    completed_at            timestamptz   NOT NULL,
    resulting_balance       numeric(19,2) NOT NULL,
    transfer_id             uuid REFERENCES transfer (id),
    counterparty_account_id uuid REFERENCES account (id)
);
CREATE INDEX idx_entry_account_history ON transaction_entry (account_id, completed_at DESC, sequence DESC);
CREATE INDEX idx_entry_transfer ON transaction_entry (transfer_id);
