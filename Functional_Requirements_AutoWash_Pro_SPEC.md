# Functional Requirements — Backend Specifications (AutoWash Pro)

This document expands FR-001 .. FR-022 into backend-focused specs: DB schema (Postgres) and API contracts (REST). Each section maps to FR codes.

---

## Database Schema (Primary Tables)

Customers
- id UUID PK
- full_name VARCHAR NOT NULL
- phone VARCHAR NOT NULL UNIQUE
- password_hash VARCHAR NOT NULL
- role VARCHAR NOT NULL DEFAULT 'member' -- enum(member,admin)
- is_locked BOOLEAN DEFAULT FALSE
- total_spending BIGINT DEFAULT 0
- created_at TIMESTAMP DEFAULT now()
- updated_at TIMESTAMP

LoyaltyAccounts
- id UUID PK
- customer_id UUID FK -> customers(id) UNIQUE
- points_balance INTEGER DEFAULT 0
- created_at TIMESTAMP

PointTransactions
- id UUID PK
- loyalty_account_id UUID FK -> loyaltyaccounts(id)
- type VARCHAR NOT NULL -- earn/redeem/expire/lock/unlock
- points INTEGER NOT NULL
- reference_id UUID NULL -- e.g., booking_id
- created_at TIMESTAMP NOT NULL
- expires_at TIMESTAMP NULL
- CONSTRAINT no_negative_balance CHECK (points >= 0)

Tiers
- id SERIAL PK
- name VARCHAR UNIQUE -- Member/Silver/Gold/Platinum
- booking_window_days INT
- min_spending BIGINT DEFAULT 0
- priority_score INT DEFAULT 0
- point_multiplier NUMERIC DEFAULT 1.0

Vehicles
- id UUID PK
- customer_id UUID FK NULL (guest vehicles NULL)
- license_plate VARCHAR NOT NULL
- is_active BOOLEAN DEFAULT TRUE
- created_at TIMESTAMP
- UNIQUE(customer_id, license_plate) -- optional if desired per-customer uniqueness

Services
- id UUID PK
- code VARCHAR UNIQUE
- name VARCHAR
- price BIGINT
- duration_minutes INT
- is_active BOOLEAN DEFAULT TRUE

Rewards
- id UUID PK
- name VARCHAR
- points_required INT
- discount_amount BIGINT NULL
- discount_percent NUMERIC NULL
- type VARCHAR -- discount/freewash/addon
- is_active BOOLEAN DEFAULT TRUE

Promotions
- id UUID PK
- name VARCHAR
- target_tier_id INT NULL
- discount_amount BIGINT NULL
- discount_percent NUMERIC NULL
- valid_from TIMESTAMP
- valid_to TIMESTAMP
- usage_limit INT NULL

Bookings
- id UUID PK
- customer_id UUID NULL -- null for guest
- guest_name VARCHAR NULL
- guest_phone VARCHAR NULL
- vehicle_id UUID FK NULL
- license_plate VARCHAR NOT NULL -- denormalized
- service_id UUID FK
- reward_id UUID FK NULL
- promotion_id UUID FK NULL
- scheduled_time TIMESTAMP NOT NULL
- checkin_time TIMESTAMP NULL
- status VARCHAR NOT NULL DEFAULT 'Pending' -- Pending/Washing/Drying/Completed/Failed/Cancelled/No-show
- base_amount BIGINT NOT NULL
- discount_amount BIGINT DEFAULT 0
- final_amount BIGINT NOT NULL
- points_earned INT DEFAULT 0
- points_redeemed INT DEFAULT 0
- payment_method VARCHAR NULL
- payment_timestamp TIMESTAMP NULL
- created_at TIMESTAMP DEFAULT now()
- updated_at TIMESTAMP

NoShowLogs
- id UUID PK
- customer_id UUID NULL
- booking_id UUID FK
- occurred_at TIMESTAMP

AdminAudit
- id UUID PK
- admin_id UUID
- action VARCHAR
- resource_type VARCHAR
- resource_id UUID
- payload JSONB
- created_at TIMESTAMP

Indexes
- bookings(scheduled_time)
- bookings(license_plate)
- customers(phone)
- vehicles(license_plate)

---

## API Contracts (Summary)

Auth
- POST /api/auth/register
  - Auth: public
  - Body: { fullName, phone, password }
  - Validations: phone unique, password min length
  - Actions: create customer, hash password (bcrypt), create loyalty account, assign Member tier
  - Response 201: { accessToken, refreshToken, user }
  - Errors: 400 validation, 409 phone exists

- POST /api/auth/login
  - Body: { phone, password }
  - Actions: verify password, block if is_locked, issue JWT access+refresh
  - Response 200: { accessToken, refreshToken, user }
  - Errors: 401 invalid creds, 423 account locked

Profile
- GET /api/profile
  - Auth: bearer
  - Response: { user, loyalty: { points_balance }, current_tier, vehicles }

Vehicles
- GET /api/vehicles (auth)
- POST /api/vehicles { license_plate }
  - Enforce member-only access; validations
  - Response 201 vehicle
- PUT /api/vehicles/:id
- DELETE /api/vehicles/:id

Bookings
- POST /api/bookings
  - Auth: optional (guest or member). Body: { customer_id? or guestName/guestPhone, vehicleId?, licensePlate, serviceId, scheduledTime, rewardId? }
  - Validations:
    - If member: owner vehicleId must belong to customer
    - Pending quotas: Guest <=1, Member <=3
    - scheduledTime >= now + 60 minutes
    - scheduledTime <= now + booking_window_days(tier)
    - No existing booking for same plate within 120 minutes
    - Service active
    - Reward eligibility: points >= required and points >= 50
  - Behavior: within DB transaction, create booking status=Pending, if reward selected create PointTransaction type=lock
  - Response 201: booking payload
  - Errors: 400 validation, 403 forbidden (outside window), 409 conflict (race)

- PATCH /api/bookings/:id/cancel
  - Auth: owner or admin
  - Validations: status == Pending, scheduledTime >= now + 2 hours
  - Behavior: mark Cancelled, if points locked then unlock (create PointTransaction unlock/refund)
  - Response 200 booking
  - Errors: 400/403

- GET /api/bookings (history)
  - Auth: owner or admin
  - Query: page, limit, filters

Admin Booking Operations
- GET /api/admin/bookings?status=&page=&limit=&search=
  - Auth: admin
  - Features: filter, sorting by scheduled_time, search by phone/license

- PATCH /api/admin/bookings/:id/status
  - Auth: admin
  - Body: { status } allowed transitions only (linear)
  - On Completed: within transaction award points, update loyalty account and create PointTransaction earn, update customer total_spending, evaluate immediate tier upgrade
  - On Failed: release point locks

Promotions & Rewards
- Admin CRUD endpoints for services, tiers, rewards, promotions
- Each change recorded in AdminAudit

Points & Ledger
- PointTransactions endpoint to query ledger (owner or admin)
- FIFO redemption implemented by selecting oldest unexpired positive transactions and deducting accordingly within a transaction
- Point expiry: background job marks transactions expired and adjusts balance

No-show and Background Jobs
- No-show processor runs every 5 minutes:
  - Query bookings with scheduled_time <= now - 15 minutes and status in (Pending, Washing?) and not checked-in -> mark No-show, create NoShowLog
  - Count no-shows per phone in past 30 days -> if >=3 set customers.is_locked and record lock expiry (could be separate table)

Auto-tiering
- Real-time upgrade: check thresholds on Completed
- Monthly downgrade job: run at 00:00 on day 1; compute last 12 months spending and downgrade if below threshold

Payments
- POST /api/bookings/:id/pay (admin/cashier)
  - Body: { paymentMethod, referenceCode?, cashierId }
  - Behavior: record payment_timestamp, payment_method; once booking Completed, payment fields immutable

Errors & HTTP Codes
- 200 OK, 201 Created
- 400 Bad Request (validation)
- 401 Unauthorized
- 403 Forbidden (RBAC / rule violation)
- 404 Not Found
- 409 Conflict (concurrency/race)
- 423 Locked (account locked)

Transactions & Concurrency
- Use DB transactions for booking creation, checkout, reward redemption, and point ledger updates
- Apply SELECT ... FOR UPDATE on loyalty rows and relevant bookings to avoid races

Security
- JWT auth, RBAC middleware for admin
- Input validation, rate limiting for booking endpoints
- Audit logs for admin changes

---

This spec is intended to be a concise, implementable backend blueprint. For expansion: provide one FR id to auto-generate model/controller/service code stubs in C# .NET 8 Web API or SQL migration scripts.
