# 📝 FUNCTIONAL REQUIREMENTS (FR) — BACKEND UPDATED
# AutoWash Pro — Backend-focused FRs (synced)

This file was updated by Copilot CLI to reflect backend-mapped Functional Requirements.

FR-001 — Register: POST /api/auth/register; unique Phone; bcrypt; create LoyaltyAccount; default Member; return JWTs + profile.
FR-002 — Login: POST /api/auth/login; JWT + refresh; block if IsLocked; RBAC roles Member/Admin.
FR-003 — Vehicles: GET/POST/PUT/DELETE /api/vehicles; Member vehicle count unlimited; LicensePlate not globally unique; Guest disallowed.
FR-004 — Read Profile: GET /api/profile; return user, loyalty points, current tier, saved vehicles.
FR-005 — Create Booking: POST /api/bookings; support Guest (GuestName/Phone) and Member (CustomerID); enforce pending quotas (Guest=1, Member=3); validate time, spacing, tier window.
FR-006 — Tier Window: enforce booking window per tier (Guest/Member 7d, Silver 10d, Gold 12d, Platinum 14d).
FR-007 — Apply Reward: validate min 50 points and sufficient points; lock points on Pending; record Point Lock.
FR-008 — Cancel Booking: PATCH /api/bookings/{id}/cancel; only Pending; >=2 hours prior; unlock/refund locked points.
FR-009 — History: paged endpoints for booking history and point ledger; owner-scoped by JWT.
FR-010 — Admin Auth: RBAC-protected admin endpoints; Members forbidden.
FR-011 — Admin Dashboard: GET /api/admin/bookings with filters, pagination, sorting.
FR-012 — Advanced Search: indexed search by Phone and LicensePlate.
FR-013 — Live Status & Checkout: status flow (Pending→Washing→Drying→Completed/Failed); linear transitions only; admin checkout triggers points awarding, spending update, immediate upgrade evaluation; Failed releases point locks.
FR-014 — Admin Config: CRUD for Services, Tiers, Rewards with audit logs.
FR-015 — PointTransaction FIFO Ledger: transactional earn/redeem/expire records; FIFO deduction; no negative balance.
FR-016 — Auto Tiering: real-time upgrade on Completed when thresholds met; monthly batch downgrade (00:00 day 1) scanning 12-month window.
FR-017 — Point Expiry Scanner: scheduled job to expire points after 12 months; record Expired transactions.
FR-018 — Automatic No-show: background job every 5 minutes; mark No-show if >15m and not checked-in; apply penalties (3 no-shows in 30d → block online bookings for 15 days).
FR-019 — Promotion Evaluation: validate tier, validity, expiry, usage rules at checkout; compute FinalAmount.
FR-020 — Booking Time Constraints: atomic checks for min 60-min lead, 120-min gap per plate, include 5-min station gap; prevent race conditions.
FR-021 — Offline Payment Recording: record PaymentMethod and PaymentTimestamp; ReferenceCode and CashierID; immutable after Completed.
FR-022 — Points Economics: enforce 1 point = 1,000đ, max 500 points/transaction, max 50% invoice coverage; validate caps at checkout.

# End of backend FRs
