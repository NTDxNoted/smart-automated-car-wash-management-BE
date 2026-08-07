-- ============================================================
--  Migration: Email OTP (registration verify / forgot password / 2FA)
--  Run manually in the Supabase SQL editor against the existing DB.
--  Safe to re-run: every statement uses IF NOT EXISTS / guards.
-- ============================================================

-- 1. New columns on Customer.
--    Email starts NULLABLE because existing rows have no value yet — Postgres
--    allows multiple NULLs under a UNIQUE constraint, so this won't fail on
--    old data. Existing accounts (including ADMIN) will need an email backfilled
--    before they can register/verify/login through the new email-gated flow;
--    decide with the team whether to backfill manually or add a "set email"
--    step for legacy accounts before flipping this to NOT NULL.
ALTER TABLE Customer ADD COLUMN IF NOT EXISTS Email VARCHAR(255) NULL;
ALTER TABLE Customer ADD COLUMN IF NOT EXISTS IsEmailVerified BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE Customer ADD COLUMN IF NOT EXISTS Is2FAEnabled BOOLEAN NOT NULL DEFAULT FALSE;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'uq_customer_email'
    ) THEN
        ALTER TABLE Customer ADD CONSTRAINT uq_customer_email UNIQUE (Email);
    END IF;
END $$;

-- 2. New Email_Otp table.
CREATE TABLE IF NOT EXISTS Email_Otp (
    OtpID       SERIAL         PRIMARY KEY,
    CustomerID  INT            NOT NULL,
    Email       VARCHAR(255)   NOT NULL,
    Purpose     VARCHAR(30)    NOT NULL CHECK (Purpose IN ('RegisterVerify','ResetPassword','Login2Fa','SensitiveAction')),
    CodeHash    VARCHAR(255)   NOT NULL,
    Attempts    INT            NOT NULL DEFAULT 0,
    IsUsed      BOOLEAN        NOT NULL DEFAULT FALSE,
    ExpiresAt   TIMESTAMP      NOT NULL,
    CreatedAt   TIMESTAMP      NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_otp_customer FOREIGN KEY (CustomerID) REFERENCES Customer(CustomerID)
        ON UPDATE CASCADE ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_otp_customer_purpose ON Email_Otp(CustomerID, Purpose);
