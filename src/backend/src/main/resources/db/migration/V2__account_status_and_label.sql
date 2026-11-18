ALTER TABLE account
    ADD COLUMN status text NOT NULL DEFAULT 'OPEN' CHECK (status IN ('OPEN', 'CLOSED')),
    ADD COLUMN label  text CHECK (char_length(label) <= 100);
