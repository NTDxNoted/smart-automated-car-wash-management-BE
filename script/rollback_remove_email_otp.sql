-- ============================================================
--  Rollback: remove email OTP verification (auth + guest booking)
--  Companion to the code revert in PR "Revert: remove email OTP
--  verification (auth + guest booking)". Irreversible — drops data.
-- ============================================================

DROP TABLE IF EXISTS email_otp CASCADE;
DROP TABLE IF EXISTS guest_email_otp CASCADE;

ALTER TABLE customer DROP COLUMN IF EXISTS email;
ALTER TABLE customer DROP COLUMN IF EXISTS isemailverified;
ALTER TABLE customer DROP COLUMN IF EXISTS is2faenabled;

ALTER TABLE booking DROP COLUMN IF EXISTS email;
ALTER TABLE booking DROP COLUMN IF EXISTS guestfullname;
