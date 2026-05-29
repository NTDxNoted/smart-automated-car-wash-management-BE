# AutoWash Pro — Project Context
**SWP391 — FPT University | 8-week scope**
**Stack: React + Vite + Node.js | DB: PostgreSQL (Supabase)**

---

## 1. What is this?
Smart car wash management web app for **Car (ô tô) only**.
Frontend + Backend web only — NOT touching physical wash machines.
Two actors: **Customer** and **Admin**.

---

## 2. Actors & Workflow

### Customer
1. Register: `FullName` + `Phone` (username) + `Password` + `ConfirmPassword` (FE validation only, NOT stored)
2. Login: `Phone` + `Password`
3. After login: sees **TotalPoints** + **CurrentTier** on header
4. Add vehicles: license plate only — **max 3 cars** (BR-05)
5. Create booking:
   - Select vehicle (from registered plates)
   - Select service
   - Select date/time (limited by tier booking window)
   - Optionally apply a Reward (only shown if `PointsRequired ≤ TotalPoints` AND `TotalPoints ≥ 50`)
   - Submit → Status = **Pending**
6. Cancel booking: only **Pending**, at least **2 hours** before scheduled time (BR-57)
7. View booking history, points balance, current tier

### Admin
1. Login (separate admin account)
2. View booking dashboard — list of all bookings, filter by status
3. Search bookings by **Phone** or **LicensePlate**
4. Checkout when wash done:
   - **Completed** → points auto-added to customer
   - **Failed** → no points added
5. Configure: Tier rules, Services, Rewards, Promotions
6. Create targeted promotions by tier (e.g. Silver+ only)

---

## 3. Booking Status Flow
```
Customer submit → Pending
Admin Checkout  → Completed (points earned) | Failed (no points)
Customer cancel → Cancelled (only Pending, ≥ 2h before)
Auto            → No-show (if not checked in 15 min after scheduled time)
```
> BR-54 (Washing → Drying → Finished) is OUT OF SCOPE — no real-time machine status.

---

## 4. Business Rules

### I. Account Management
| BR | Rule |
|----|------|
| BR-01 | Customer must register before booking |
| BR-02 | Phone is unique identifier (username) |
| BR-03 | Required at registration: FullName + Phone + Password |
| BR-04 | Duplicate phone must be warned and rejected |
| BR-05 | Max **3 vehicles** per account; must delete old to add 6th |
| BR-06 | One license plate → one account only |
| BR-08 | Customer can only view their own profile, points, history |
| BR-09 | Admin can view all history but **cannot edit** completed transactions |
| BR-10 | Locked account cannot book |

> BR-07 (OTP for adding vehicle) — OUT OF SCOPE

---

### II. Membership Tiers
| BR | Rule |
|----|------|
| BR-11 | New customer defaults to **Member** tier |
| BR-12 | Member: book up to **7 days** ahead |
| BR-13 | Silver: book up to **10 days** ahead |
| BR-14 | Gold: book up to **12 days** ahead |
| BR-15 | Platinum: book up to **14 days** ahead |
| BR-16 | Same time slot: **Platinum > Gold > Silver > Member** priority queue |
| BR-17 | Auto **upgrade** immediately when spending threshold reached |
| BR-18 | Auto **downgrade** at 00:00 on 1st of each month based on last 12 months spending |
| BR-19 | Tier perks auto-applied at checkout — no manual action needed |

---

### III. Booking
| BR | Rule |
|----|------|
| BR-20 | Each booking applies to **one vehicle only** |
| BR-21 | Max **2 unfinished bookings** per account per day |
| BR-22 | Same vehicle cannot have 2 bookings within **120 minutes** of each other |
| BR-23 | Max **1 Pending** booking per account at a time |
| BR-24 | Booking must be made at least **60 minutes** before service time |
| BR-57 | Customer can cancel free of charge at least **2 hours** before scheduled time |
| BR-58 | Only **Pending** bookings can be cancelled |
| BR-59 | **Completed** or **Cancelled** bookings cannot be edited |

---

### IV. Service & Pricing
| BR | Rule |
|----|------|
| BR-29 | Car only in this system |
| BR-30 | Each service must have fixed price, description, and duration |
| BR-32 | Inactive services must not be shown for booking |
| BR-33 | Admin can update price but NOT affect already-confirmed bookings |
| BR-34 | Total = service price - discounts |

---

### V. Payment
| BR | Rule |
|----|------|
| BR-35 | Supported: **Cash** and **Transfer** only |
| BR-39 | System must record payment timestamp |
| BR-41 | No automatic refund — manual, Admin must approve |

---

### VI. Loyalty & Rewards
| BR | Rule |
|----|------|
| BR-42 | Default rate: **10,000 VNĐ = 1 point** (Admin configurable) |
| BR-43 | Points added only after booking → **Completed** and paid |
| BR-44 | Max **500 points** per transaction |
| BR-45 | No points for cancelled bookings |
| BR-46 | Points expire **12 months** after transaction date |
| BR-47 | Deduct oldest points first (**FIFO**) |
| BR-48 | Expired points cannot be restored |
| BR-49 | **1 point = 1,000 VNĐ** discount |
| BR-50 | Minimum **50 points** to start redeeming |
| BR-51 | Points cover max **50%** of invoice |
| BR-52 | Points cannot be converted to cash |
| BR-53 | Redeemable for: **Discount**, **Free Wash**, or **Add-on** only |

---

### VII. No-show & Violations
| BR | Rule |
|----|------|
| BR-60 | No check-in after **15 minutes** → auto **No-show** |
| BR-61 | **3 No-shows in 30 days** → booking locked **15 days** |
| BR-62 | Lock auto-lifted after penalty period |

---

### VIII. Promotions
| BR | Rule |
|----|------|
| BR-63 | Promotions restricted by tier — lower tiers cannot see or use |
| BR-64 | Only Admin can configure tier rules, point rates, perks, promotions |

---

## 5. Entities (12 total)

| ID | Entity | Purpose |
|----|--------|---------|
| E-01 | Customer | Account, tier, spending |
| E-02 | Vehicle | License plate, max 5/customer, Car only |
| E-03 | Booking | Core transaction log |
| E-04 | Service | Car wash service catalog |
| E-05 | Tier | Member/Silver/Gold/Platinum rules |
| E-06 | LoyaltyAccount | Points balance per customer |
| E-07 | PointTransaction | Points earn/redeem/expire FIFO log |
| E-08 | Rewards_Catalog | Redeemable rewards |
| E-09 | Promotion | Targeted promos by tier |
| E-10 | CustomerPromotion | Promo usage log (N-M resolution) |
| E-11 | Transaction | Payment record |
| E-12 | NoShowLog | No-show tracking → account lock |

---

## 6. Key Fields

**Customer:** CustomerID, FullName, Phone (UNIQUE), Password (bcrypt), TierID (FK), TotalSpending, LastVisit, CreatedAt, IsLocked

**Vehicle:** VehicleID, CustomerID (FK), LicensePlate (UNIQUE), IsActive, CreatedAt

**Booking:** BookingID, CustomerID (FK), Phone (denorm), VehicleID (FK), LicensePlate (denorm), ServiceID (FK), RewardID (FK nullable), PromotionID (FK nullable), ScheduledTime, CheckInTime, Status ENUM(Pending/Completed/Failed/Cancelled/No-show), BaseAmount, DiscountApplied, FinalAmount, PointsEarned, PointsRedeemed, CreatedAt, CompletedAt

**Tier:** TierID, TierName, MinSpending, MinWashes, BookingWindowDays, PriorityScore, PointMultiplier, DiscountRate, PointRate

**Rewards_Catalog:** RewardID, RewardName, PointsRequired, DiscountValue, DiscountType (Fixed_Amount/Percentage), RewardType (Discount/FreeWash/AddOn), IsActive

**NoShowLog:** LogID, CustomerID (FK), BookingID (FK), OccurredAt

---

## 7. Out of Scope (8-week sprint)
| Feature | BR | Reason |
|---------|----|--------|
| OTP verification when adding vehicle | BR-07 | No SMS service |
| Real-time wash status (Washing→Drying→Finished) | BR-54/55 | No machine integration |
| Online payment gateway | BR-35 partial | Cash/Transfer only |
| Automatic refund | BR-41 | Manual process only |
| VAT calculation | BR-31 | Simplified pricing |
| SMS/App push notification | BR-28 | No notification service |
| 5-min gap between washes | BR-25 | No machine scheduling |
| Station slot management | BR-26/27 | Simplified — no multi-station |
| Motorbike services | BR-29 | Car only by design |

---

## 8. Current Progress
- Database schema: done (`autowash_supabase.sql`)
- Entity Context: done (`Context_v7.xlsx`)
- Frontend: React + Vite started (Navbar, Hero, Services, Booking, Membership)
- Backend: not started
- DB: Supabase (PostgreSQL)
