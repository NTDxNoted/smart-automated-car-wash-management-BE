-- ============================================================
--  AutoWash Pro — Database Schema v7 (PostgreSQL / Supabase)
--  SWP391 — FPT University
--  10 Tables | Car only | Pending → Completed / Failed
-- ============================================================

DROP TABLE IF EXISTS Tier CASCADE;
DROP TABLE IF EXISTS Customer CASCADE;
DROP TABLE IF EXISTS Vehicle CASCADE;
DROP TABLE IF EXISTS Service CASCADE;
DROP TABLE IF EXISTS Rewards_Catalog CASCADE;
DROP TABLE IF EXISTS Promotion CASCADE;
DROP TABLE IF EXISTS Booking CASCADE;
DROP TABLE IF EXISTS Transaction CASCADE;
DROP TABLE IF EXISTS LoyaltyAccount CASCADE;
DROP TABLE IF EXISTS PointTransaction CASCADE;
DROP TABLE IF EXISTS CustomerPromotion CASCADE;
-- ── 1. Tier ─────────────────────────────────────────────
CREATE TABLE Tier (
    TierID            SERIAL         PRIMARY KEY,
    TierName          VARCHAR(20)    NOT NULL,
    MinSpending       DECIMAL(15,2)  NOT NULL DEFAULT 0,
    MinWashes         INT            NOT NULL DEFAULT 0,
    BookingWindowDays INT            NOT NULL DEFAULT 7,
    PriorityScore     INT            NOT NULL DEFAULT 1,
    PointMultiplier   FLOAT          NOT NULL DEFAULT 1.0,
    DiscountRate      DECIMAL(5,2)   NOT NULL DEFAULT 0.00,
    PointRate         FLOAT          NOT NULL DEFAULT 1.0
);

INSERT INTO Tier (TierName, MinSpending, MinWashes, BookingWindowDays, PriorityScore, PointMultiplier, DiscountRate, PointRate) VALUES
('Member',   0,       0,  7,  1, 1.0, 0.00, 1.0),
('Silver',   500000,  8,  10, 2, 1.1, 5.00, 1.1),
('Gold',     1500000, 20, 12, 3, 1.3, 10.00,1.3),
('Platinum', 3000000, 40, 14, 4, 1.5, 15.00,1.5);

-- ── 2. Customer ─────────────────────────────────────────
CREATE TABLE Customer (
    CustomerID    SERIAL         PRIMARY KEY,
    FullName      VARCHAR(100)   NOT NULL,
    Phone         VARCHAR(15)    NOT NULL UNIQUE,
    Password      VARCHAR(255)   NOT NULL,
    Role          VARCHAR(20)    NOT NULL DEFAULT 'MEMBER' CHECK (Role IN ('MEMBER', 'ADMIN')),
    TierID        INT            NOT NULL DEFAULT 1,
    TotalSpending DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    LastVisit     TIMESTAMP      NULL DEFAULT NULL,
    CreatedAt     TIMESTAMP      NOT NULL DEFAULT NOW(),
    IsLocked      BOOLEAN        NOT NULL DEFAULT FALSE,
    NoShowCount   INT            NOT NULL DEFAULT 0,
    SuspendedUntil TIMESTAMP     NULL DEFAULT NULL,
    CONSTRAINT fk_customer_tier FOREIGN KEY (TierID) REFERENCES Tier(TierID)
        ON UPDATE CASCADE ON DELETE RESTRICT
);

-- ── 3. Vehicle ──────────────────────────────────────────
CREATE TABLE Vehicle (
    VehicleID    SERIAL        PRIMARY KEY,
    CustomerID   INT           NOT NULL,
    LicensePlate VARCHAR(20)   NOT NULL UNIQUE,
    IsActive     BOOLEAN       NOT NULL DEFAULT TRUE,
    CreatedAt    TIMESTAMP     NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_vehicle_customer FOREIGN KEY (CustomerID) REFERENCES Customer(CustomerID)
        ON UPDATE CASCADE ON DELETE CASCADE
);

-- ── 4. Service ──────────────────────────────────────────
CREATE TABLE Service (
    ServiceID       SERIAL         PRIMARY KEY,
    ServiceName     VARCHAR(100)   NOT NULL,
    ServiceCategory VARCHAR(20)    NOT NULL CHECK (ServiceCategory IN ('Basic','Premium','Detail','AddOn')),
    Description     TEXT           NULL,
    Price           DECIMAL(10,2)  NOT NULL,
    Duration        INT            NOT NULL,
    Status          VARCHAR(10)    NOT NULL DEFAULT 'Active' CHECK (Status IN ('Active','Inactive'))
);

-- ── 5. Rewards_Catalog ──────────────────────────────────
CREATE TABLE Rewards_Catalog (
    RewardID       SERIAL         PRIMARY KEY,
    RewardName     VARCHAR(100)   NOT NULL,
    PointsRequired INT            NOT NULL,
    DiscountValue  DECIMAL(10,2)  NOT NULL,
    DiscountType   VARCHAR(20)    NOT NULL CHECK (DiscountType IN ('Fixed_Amount','Percentage')),
    IsActive       BOOLEAN        NOT NULL DEFAULT TRUE
);

-- ── 6. Promotion ────────────────────────────────────────
CREATE TABLE Promotion (
    PromotionID   SERIAL         PRIMARY KEY,
    Title         VARCHAR(100)   NOT NULL,
    PromoCode     VARCHAR(20)    NOT NULL UNIQUE,
    MinTierID     INT            NULL DEFAULT NULL,
    DiscountType  VARCHAR(20)    NOT NULL CHECK (DiscountType IN ('Percentage','Fixed_Amount')),
    DiscountValue DECIMAL(10,2)  NOT NULL,
    MaxUsage      INT            NULL DEFAULT NULL,
    StartDate     DATE           NOT NULL,
    EndDate       DATE           NOT NULL,
    IsActive      BOOLEAN        NOT NULL DEFAULT TRUE,
    MinOrderValue DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    MaxDiscountAmount DECIMAL(15,2) NULL DEFAULT NULL,
    CONSTRAINT fk_promo_tier FOREIGN KEY (MinTierID) REFERENCES Tier(TierID)
        ON UPDATE CASCADE ON DELETE SET NULL
);

-- ── 7. Booking ──────────────────────────────────────────
CREATE TABLE Booking (
    BookingID       SERIAL         PRIMARY KEY,
    CustomerID      INT            NOT NULL,
    Phone           VARCHAR(15)    NOT NULL,
    VehicleID       INT            NOT NULL,
    LicensePlate    VARCHAR(20)    NOT NULL,
    ServiceID       INT            NOT NULL,
    RewardID        INT            NULL DEFAULT NULL,
    PromotionID     INT            NULL DEFAULT NULL,
    ScheduledTime   TIMESTAMP      NOT NULL,
    CheckInTime     TIMESTAMP      NULL DEFAULT NULL,
    Status          VARCHAR(20)    NOT NULL DEFAULT 'Pending' CHECK (Status IN ('Pending','Completed','Failed','Cancelled','Noshow')),
    BaseAmount      DECIMAL(10,2)  NOT NULL,
    DiscountApplied DECIMAL(10,2)  NOT NULL DEFAULT 0.00,
    FinalAmount     DECIMAL(10,2)  NOT NULL,
    PointsEarned    INT            NOT NULL DEFAULT 0,
    PointsRedeemed  INT            NOT NULL DEFAULT 0,
    CreatedAt       TIMESTAMP      NOT NULL DEFAULT NOW(),
    CompletedAt     TIMESTAMP      NULL DEFAULT NULL,
    CONSTRAINT fk_booking_customer  FOREIGN KEY (CustomerID)  REFERENCES Customer(CustomerID)      ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_booking_vehicle   FOREIGN KEY (VehicleID)   REFERENCES Vehicle(VehicleID)        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_booking_service   FOREIGN KEY (ServiceID)   REFERENCES Service(ServiceID)        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_booking_reward    FOREIGN KEY (RewardID)    REFERENCES Rewards_Catalog(RewardID) ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_booking_promo     FOREIGN KEY (PromotionID) REFERENCES Promotion(PromotionID)    ON UPDATE CASCADE ON DELETE SET NULL
);

CREATE INDEX idx_booking_phone  ON Booking(Phone);
CREATE INDEX idx_booking_plate  ON Booking(LicensePlate);
CREATE INDEX idx_booking_status ON Booking(Status);
CREATE INDEX idx_booking_time   ON Booking(ScheduledTime);

-- ── 8. Transaction ──────────────────────────────────────
CREATE TABLE Transaction (
    TransactionID  SERIAL         PRIMARY KEY,
    BookingID      INT            NOT NULL,
    Amount         DECIMAL(10,2)  NOT NULL,
    PaymentMethod  VARCHAR(20)    NOT NULL CHECK (PaymentMethod IN ('Cash','Transfer')),
    PaidAt         TIMESTAMP      NULL DEFAULT NULL,
    Status         VARCHAR(10)    NOT NULL DEFAULT 'Pending' CHECK (Status IN ('Paid','Pending','Failed')),
    CONSTRAINT fk_txn_booking FOREIGN KEY (BookingID) REFERENCES Booking(BookingID)
        ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE UNIQUE INDEX idx_txn_booking_paid ON Transaction(BookingID) WHERE Status = 'Paid';


-- ── 9. LoyaltyAccount ───────────────────────────────────
CREATE TABLE LoyaltyAccount (
    LoyaltyID    SERIAL     PRIMARY KEY,
    CustomerID   INT        NOT NULL UNIQUE,
    TotalPoints  INT        NOT NULL DEFAULT 0,
    LastUpdated  TIMESTAMP  NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_loyalty_customer FOREIGN KEY (CustomerID) REFERENCES Customer(CustomerID)
        ON UPDATE CASCADE ON DELETE CASCADE
);

-- ── 10. PointTransaction ────────────────────────────────
CREATE TABLE PointTransaction (
    PointTxnID   SERIAL     PRIMARY KEY,
    LoyaltyID    INT        NOT NULL,
    Points       INT        NOT NULL,
    Type         VARCHAR(10) NOT NULL CHECK (Type IN ('Earn','Redeem','Expire')),
    RefBookingID INT        NULL DEFAULT NULL,
    ExpiredAt    TIMESTAMP  NULL DEFAULT NULL,
    CreatedAt    TIMESTAMP  NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_ptxn_loyalty FOREIGN KEY (LoyaltyID)    REFERENCES LoyaltyAccount(LoyaltyID) ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_ptxn_booking FOREIGN KEY (RefBookingID) REFERENCES Booking(BookingID)         ON UPDATE CASCADE ON DELETE SET NULL
);

CREATE INDEX idx_ptxn_loyalty ON PointTransaction(LoyaltyID);
CREATE INDEX idx_ptxn_date    ON PointTransaction(CreatedAt);

-- ── 11. CustomerPromotion ───────────────────────────────
CREATE TABLE CustomerPromotion (
    ID                   SERIAL         PRIMARY KEY,
    CustomerID           INT            NOT NULL,
    PromotionID          INT            NOT NULL,
    BookingID            INT            NOT NULL,
    UsedAt               TIMESTAMP      NOT NULL DEFAULT NOW(),
    DiscountAmountActual DECIMAL(10,2)  NOT NULL,
    CONSTRAINT fk_cp_customer FOREIGN KEY (CustomerID)  REFERENCES Customer(CustomerID)   ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_cp_promo    FOREIGN KEY (PromotionID) REFERENCES Promotion(PromotionID) ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_cp_booking  FOREIGN KEY (BookingID)   REFERENCES Booking(BookingID)     ON UPDATE CASCADE ON DELETE RESTRICT
);



-- ── View: RFM for ML ────────────────────────────────────
CREATE OR REPLACE VIEW vw_customer_rfm AS
SELECT
    c.CustomerID,
    c.FullName,
    c.Phone,
    t.TierName                                        AS CurrentTier,
    EXTRACT(DAY FROM NOW() - c.LastVisit)::INT        AS Recency_Days,
    COUNT(b.BookingID)                                AS Frequency,
    COALESCE(SUM(b.FinalAmount), 0)                   AS Monetary_Total,
    la.TotalPoints,
    c.TotalSpending,
    c.CreatedAt                                       AS MemberSince
FROM       Customer       c
LEFT JOIN  Tier           t  ON c.TierID     = t.TierID
LEFT JOIN  LoyaltyAccount la ON c.CustomerID = la.CustomerID
LEFT JOIN  Booking        b  ON c.CustomerID = b.CustomerID AND b.Status = 'Completed'
GROUP BY   c.CustomerID, c.FullName, c.Phone,
           t.TierName, c.LastVisit,
           la.TotalPoints, c.TotalSpending, c.CreatedAt;

-- ============================================================
--  END — 11 Tables + 1 View
--  Tier, Customer, Vehicle, Service, Rewards_Catalog,
--  Promotion, Booking, Transaction,
--  LoyaltyAccount, PointTransaction, CustomerPromotion
-- ============================================================

-- ============================================================
--  DEMO DATA — AutoWash Pro
--  Vietnamese names | Realistic data
-- ============================================================

-- ── Service ─────────────────────────────────────────────
INSERT INTO Service (ServiceName, ServiceCategory, Description, Price, Duration, Status) VALUES
('Rửa xe cơ bản',            'Basic',   'Rửa ngoài, sấy khô, lau kính',                  80000,  20, 'Active'),
('Rửa xe cao cấp',           'Premium', 'Rửa toàn diện + xịt bóng ngoại thất',           150000, 35, 'Active'),
('Rửa + Hút bụi nội thất',   'Premium', 'Rửa ngoài và hút bụi toàn bộ nội thất',         220000, 50, 'Active'),
('Rửa chi tiết toàn bộ',     'Detail',  'Dịch vụ cao cấp nhất — trong và ngoài hoàn hảo',350000, 90, 'Active'),
('Đánh bóng & bảo vệ sơn',   'AddOn',   'Đánh bóng lớp sơn + phủ ceramic nano',          200000, 45, 'Active');

-- ── Rewards_Catalog ─────────────────────────────────────
INSERT INTO Rewards_Catalog (RewardName, PointsRequired, DiscountValue, DiscountType, IsActive) VALUES
('Giảm 50,000đ',             500,  50000, 'Fixed_Amount', TRUE),
('Giảm 100,000đ',            900,  100000,'Fixed_Amount', TRUE),
('Giảm 10%',                 300,  10,    'Percentage',   TRUE),
('Giảm 20%',                 700,  20,    'Percentage',   TRUE),
('Rửa xe cơ bản miễn phí',   800,  80000, 'Fixed_Amount', TRUE);

-- ── Promotion ───────────────────────────────────────────
INSERT INTO Promotion (Title, PromoCode, MinTierID, DiscountType, DiscountValue, MaxUsage, StartDate, EndDate, IsActive) VALUES
('Chào mừng thành viên mới',  'WELCOME2025', NULL, 'Fixed_Amount', 30000,  500, '2025-01-01', '2025-12-31', TRUE),
('Ưu đãi Silver tháng 5',     'SILVER05',    2,    'Percentage',   10,     200, '2025-05-01', '2025-05-31', TRUE),
('Gold & Platinum Deal',       'GOLDDEAL',    3,    'Percentage',   15,     100, '2025-05-01', '2025-06-30', TRUE),
('Cuối tuần giảm giá',         'WEEKEND50',   NULL, 'Fixed_Amount', 50000,  NULL,'2025-01-01', '2025-12-31', TRUE);

-- ── Customer ────────────────────────────────────────────
-- Password: 'password123' (bcrypt placeholder)
INSERT INTO Customer (FullName, Phone, Password, Role, TierID, TotalSpending, LastVisit, CreatedAt, IsLocked) VALUES
('Nguyễn Văn An',      '0901111001', '$2a$11$Kw4MZIqYu0UNFSfpZarTLOeEAy5x9UbDzrblb2mQx/.yrZ/DuUSDC', 'MEMBER', 1, 0,        NULL,                        '2025-03-01 08:00:00', FALSE),
('Trần Thị Bích',      '0901111002', '$2a$11$Kw4MZIqYu0UNFSfpZarTLOeEAy5x9UbDzrblb2mQx/.yrZ/DuUSDC', 'MEMBER', 2, 650000,  '2025-05-10 14:30:00',       '2025-01-15 09:00:00', FALSE),
('Lê Hoàng Cường',     '0901111003', '$2a$11$Kw4MZIqYu0UNFSfpZarTLOeEAy5x9UbDzrblb2mQx/.yrZ/DuUSDC', 'MEMBER', 3, 1800000, '2025-05-08 10:00:00',       '2024-12-20 11:00:00', FALSE),
('Phạm Thị Dung',      '0901111004', '$2a$11$Kw4MZIqYu0UNFSfpZarTLOeEAy5x9UbDzrblb2mQx/.yrZ/DuUSDC', 'MEMBER', 4, 3500000, '2025-05-12 16:00:00',       '2024-11-05 08:30:00', FALSE),
('Hoàng Minh Đức',     '0901111005', '$2a$11$Kw4MZIqYu0UNFSfpZarTLOeEAy5x9UbDzrblb2mQx/.yrZ/DuUSDC', 'MEMBER', 2, 720000,  '2025-05-05 09:15:00',       '2025-02-01 10:00:00', FALSE),
('Vũ Thị Hoa',         '0901111006', '$2a$11$Kw4MZIqYu0UNFSfpZarTLOeEAy5x9UbDzrblb2mQx/.yrZ/DuUSDC', 'MEMBER', 1, 120000,  '2025-04-20 13:00:00',       '2025-04-01 08:00:00', FALSE),
('Đặng Quốc Hùng',     '0901111007', '$2a$11$Kw4MZIqYu0UNFSfpZarTLOeEAy5x9UbDzrblb2mQx/.yrZ/DuUSDC', 'MEMBER', 3, 2100000, '2025-05-11 11:00:00',       '2024-10-10 09:00:00', FALSE),
('Bùi Thị Lan',        '0901111008', '$2a$11$Kw4MZIqYu0UNFSfpZarTLOeEAy5x9UbDzrblb2mQx/.yrZ/DuUSDC', 'MEMBER', 1, 80000,   '2025-05-01 15:00:00',       '2025-05-01 08:00:00', FALSE),
('Ngô Thanh Long',     '0901111009', '$2a$11$Kw4MZIqYu0UNFSfpZarTLOeEAy5x9UbDzrblb2mQx/.yrZ/DuUSDC', 'MEMBER', 4, 4200000, '2025-05-13 10:30:00',       '2024-09-15 07:00:00', FALSE),
('Đinh Thị Mai',       '0901111010', '$2a$11$Kw4MZIqYu0UNFSfpZarTLOeEAy5x9UbDzrblb2mQx/.yrZ/DuUSDC', 'MEMBER', 2, 580000,  '2025-05-07 14:00:00',       '2025-01-20 09:00:00', FALSE),
('Super Admin',        '0999999991', '$2a$11$Kw4MZIqYu0UNFSfpZarTLOeEAy5x9UbDzrblb2mQx/.yrZ/DuUSDC', 'ADMIN',  1, 0,       NULL,                        '2025-01-01 00:00:00', FALSE),
('Manager Admin',      '0999999992', '$2a$11$Kw4MZIqYu0UNFSfpZarTLOeEAy5x9UbDzrblb2mQx/.yrZ/DuUSDC', 'ADMIN',  1, 0,       NULL,                        '2025-01-01 00:00:00', FALSE);

-- ── LoyaltyAccount ──────────────────────────────────────
INSERT INTO LoyaltyAccount (CustomerID, TotalPoints, LastUpdated) VALUES
(1, 80,   NOW()),
(2, 650,  NOW()),
(3, 1200, NOW()),
(4, 2800, NOW()),
(5, 720,  NOW()),
(6, 120,  NOW()),
(7, 1500, NOW()),
(8, 80,   NOW()),
(9, 3100, NOW()),
(10,580,  NOW());

-- ── Vehicle ─────────────────────────────────────────────
INSERT INTO Vehicle (CustomerID, LicensePlate, IsActive) VALUES
(1,  '51A-001.11', TRUE),
(2,  '51B-002.22', TRUE),
(2,  '51C-002.33', TRUE),
(3,  '51D-003.44', TRUE),
(4,  '51E-004.55', TRUE),
(4,  '51F-004.66', TRUE),
(4,  '51G-004.77', TRUE),
(5,  '51H-005.88', TRUE),
(6,  '51K-006.99', TRUE),
(7,  '51L-007.10', TRUE),
(7,  '51M-007.20', TRUE),
(8,  '51N-008.30', TRUE),
(9,  '51P-009.40', TRUE),
(9,  '51Q-009.50', TRUE),
(10, '51R-010.60', TRUE);

-- ── Booking ─────────────────────────────────────────────
INSERT INTO Booking (CustomerID, Phone, VehicleID, LicensePlate, ServiceID, RewardID, PromotionID, ScheduledTime, CheckInTime, Status, BaseAmount, DiscountApplied, FinalAmount, PointsEarned, PointsRedeemed, CreatedAt, CompletedAt) VALUES
-- Completed bookings
(1,  '0901111001', 1,  '51A-001.11', 1, NULL, 1,    '2025-03-10 09:00:00', '2025-03-10 09:05:00', 'Completed', 80000,  30000, 50000,  50,  0,   '2025-03-09 20:00:00', '2025-03-10 09:25:00'),
(2,  '0901111002', 2,  '51B-002.22', 2, NULL, NULL, '2025-04-05 10:00:00', '2025-04-05 10:02:00', 'Completed', 150000, 0,     150000, 165, 0,   '2025-04-04 18:00:00', '2025-04-05 10:40:00'),
(3,  '0901111003', 4,  '51D-003.44', 3, 1,    NULL, '2025-04-12 14:00:00', '2025-04-12 14:10:00', 'Completed', 220000, 50000, 170000, 221, 500, '2025-04-11 21:00:00', '2025-04-12 15:00:00'),
(4,  '0901111004', 5,  '51E-004.55', 4, NULL, 3,    '2025-04-20 11:00:00', '2025-04-20 11:05:00', 'Completed', 350000, 52500, 297500, 446, 0,   '2025-04-19 10:00:00', '2025-04-20 12:35:00'),
(5,  '0901111005', 8,  '51H-005.88', 1, 3,    NULL, '2025-04-25 08:00:00', '2025-04-25 08:03:00', 'Completed', 80000,  8000,  72000,  79,  300, '2025-04-24 20:00:00', '2025-04-25 08:22:00'),
(6,  '0901111006', 9,  '51K-006.99', 2, NULL, 1,    '2025-04-28 15:00:00', '2025-04-28 15:08:00', 'Completed', 150000, 30000, 120000, 132, 0,   '2025-04-27 19:00:00', '2025-04-28 15:40:00'),
(7,  '0901111007', 10, '51L-007.10', 4, 2,    3,    '2025-05-02 09:00:00', '2025-05-02 09:02:00', 'Completed', 350000, 152500,197500, 257, 900, '2025-05-01 21:00:00', '2025-05-02 10:35:00'),
(9,  '0901111009', 13, '51P-009.40', 3, NULL, 3,    '2025-05-08 10:00:00', '2025-05-08 10:01:00', 'Completed', 220000, 33000, 187000, 281, 0,   '2025-05-07 22:00:00', '2025-05-08 10:55:00'),
(10, '0901111010', 15, '51R-010.60', 2, 1,    2,    '2025-05-10 13:00:00', '2025-05-10 13:04:00', 'Completed', 150000, 65000, 85000,  94,  500, '2025-05-09 20:00:00', '2025-05-10 13:40:00'),
(3,  '0901111003', 4,  '51D-003.44', 5, NULL, NULL, '2025-05-11 16:00:00', '2025-05-11 16:05:00', 'Completed', 200000, 20000, 180000, 234, 0,   '2025-05-10 09:00:00', '2025-05-11 16:50:00'),
-- Pending bookings
(1,  '0901111001', 1,  '51A-001.11', 2, NULL, 1,    '2025-05-20 09:00:00', NULL,                  'Pending',   150000, 30000, 120000, 0,   0,   '2025-05-14 10:00:00', NULL),
(2,  '0901111002', 3,  '51C-002.33', 1, 3,    NULL, '2025-05-21 10:30:00', NULL,                  'Pending',   80000,  8000,  72000,  0,   300, '2025-05-14 11:00:00', NULL),
(4,  '0901111004', 6,  '51F-004.66', 4, NULL, 3,    '2025-05-22 14:00:00', NULL,                  'Pending',   350000, 52500, 297500, 0,   0,   '2025-05-14 12:00:00', NULL),
(7,  '0901111007', 11, '51M-007.20', 3, 4,    NULL, '2025-05-23 08:00:00', NULL,                  'Pending',   220000, 44000, 176000, 0,   700, '2025-05-14 13:00:00', NULL),
(9,  '0901111009', 14, '51Q-009.50', 5, NULL, NULL, '2025-05-24 11:00:00', NULL,                  'Pending',   200000, 30000, 170000, 0,   0,   '2025-05-14 14:00:00', NULL),
-- Failed booking
(8,  '0901111008', 12, '51N-008.30', 1, NULL, NULL, '2025-05-01 09:00:00', NULL,                  'Failed',    80000,  0,     80000,  0,   0,   '2025-04-30 20:00:00', NULL);

-- ── PointTransaction ────────────────────────────────────
INSERT INTO PointTransaction (LoyaltyID, Points, Type, RefBookingID, ExpiredAt, CreatedAt) VALUES
(1,   50,   'Earn',   1,  '2026-03-10 09:25:00', '2025-03-10 09:25:00'),
(2,   165,  'Earn',   2,  '2026-04-05 10:40:00', '2025-04-05 10:40:00'),
(3,   -500, 'Redeem', 3,  NULL,                  '2025-04-12 14:00:00'),
(3,   221,  'Earn',   3,  '2026-04-12 15:00:00', '2025-04-12 15:00:00'),
(4,   446,  'Earn',   4,  '2026-04-20 12:35:00', '2025-04-20 12:35:00'),
(5,   -300, 'Redeem', 5,  NULL,                  '2025-04-25 08:00:00'),
(5,   79,   'Earn',   5,  '2026-04-25 08:22:00', '2025-04-25 08:22:00'),
(6,   132,  'Earn',   6,  '2026-04-28 15:40:00', '2025-04-28 15:40:00'),
(7,   -900, 'Redeem', 7,  NULL,                  '2025-05-02 09:00:00'),
(7,   257,  'Earn',   7,  '2026-05-02 10:35:00', '2025-05-02 10:35:00'),
(9,   281,  'Earn',   8,  '2026-05-08 10:55:00', '2025-05-08 10:55:00'),
(10,  -500, 'Redeem', 9,  NULL,                  '2025-05-10 13:00:00'),
(10,  94,   'Earn',   9,  '2026-05-10 13:40:00', '2025-05-10 13:40:00'),
(3,   234,  'Earn',   10, '2026-05-11 16:50:00', '2025-05-11 16:50:00');

-- ── CustomerPromotion ───────────────────────────────────
INSERT INTO CustomerPromotion (CustomerID, PromotionID, BookingID, UsedAt, DiscountAmountActual) VALUES
(1,  1, 1,  '2025-03-10 09:00:00', 30000),
(6,  1, 6,  '2025-04-28 15:00:00', 30000),
(4,  3, 4,  '2025-04-20 11:00:00', 52500),
(7,  3, 7,  '2025-05-02 09:00:00', 52500),
(9,  3, 8,  '2025-05-08 10:00:00', 33000),
(2,  2, 10, '2025-05-10 13:00:00', 15000),
(1,  1, 11, '2025-05-14 10:00:00', 30000),
(4,  3, 13, '2025-05-14 12:00:00', 52500);


--  END DEMO DATA
--  10 Customers | 15 Vehicles | 16 Bookings
--  (10 Completed, 5 Pending, 1 Failed)
-- ============================================================
