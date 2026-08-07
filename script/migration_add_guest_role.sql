-- ============================================================
--  Migration: allow role = 'GUEST' on Customer, backfill walk-ins
--  Run manually in the Supabase SQL editor against the existing DB.
--  Safe to re-run.
--
--  Walk-in customers (Password = 'WALKIN', auto-created by
--  AdminBookingService when an admin books for someone without an
--  account) were stored with Role = 'MEMBER', which read as if they
--  were real registered members in the raw table. They're a distinct
--  kind of row (see AdminCustomerService.ResolveTierLabel, which
--  already displays them as "Guest" everywhere in the admin UI).
--  This migration widens the check constraint so Role can also be
--  'GUEST', and backfills the existing walk-in rows to match.
-- ============================================================

ALTER TABLE Customer DROP CONSTRAINT IF EXISTS customer_role_check;
ALTER TABLE Customer ADD CONSTRAINT customer_role_check
    CHECK (Role IN ('MEMBER', 'ADMIN', 'GUEST'));

UPDATE Customer SET Role = 'GUEST' WHERE Password = 'WALKIN' AND Role != 'GUEST';
