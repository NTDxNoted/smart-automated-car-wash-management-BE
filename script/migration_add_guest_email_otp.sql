-- ============================================================
--  Migration: Guest booking email OTP (Full Name + Email step in Guest booking wizard)
--  Safe to re-run: every statement uses IF NOT EXISTS / guards.
-- ============================================================

-- 1. New account-less OTP table for guest bookings (no FK — guests have no real Customer row).
CREATE TABLE IF NOT EXISTS Guest_Email_Otp (
    OtpID       SERIAL         PRIMARY KEY,
    Email       VARCHAR(255)   NOT NULL,
    Purpose     VARCHAR(30)    NOT NULL CHECK (Purpose IN ('RegisterVerify','ResetPassword','Login2Fa','SensitiveAction','GuestBookingVerify')),
    CodeHash    VARCHAR(255)   NOT NULL,
    Attempts    INT            NOT NULL DEFAULT 0,
    IsUsed      BOOLEAN        NOT NULL DEFAULT FALSE,
    VerifiedAt  TIMESTAMP      NULL,
    ExpiresAt   TIMESTAMP      NOT NULL,
    CreatedAt   TIMESTAMP      NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_guest_otp_email_purpose ON Guest_Email_Otp(Email, Purpose);

-- 2. New columns on Booking to persist the verified guest's identity.
ALTER TABLE Booking ADD COLUMN IF NOT EXISTS Email VARCHAR(255) NULL;
ALTER TABLE Booking ADD COLUMN IF NOT EXISTS GuestFullName VARCHAR(100) NULL;
