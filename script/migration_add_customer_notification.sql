-- ============================================================
--  Migration: CustomerNotification table
--  Run manually in the Supabase SQL editor against the existing DB.
--  Safe to re-run: every statement uses IF NOT EXISTS.
--
--  Fixes: Npgsql.PostgresException 42P01 "relation customernotification
--  does not exist" on GET /api/promotions/my-notifications and
--  POST /api/admin/rfm/send-action. The entity/DbSet already exist in
--  code (see PromotionService.cs) and the table is already defined in
--  script/autowash_supabase_3.sql — this migration just brings an
--  existing database that predates that table up to date.
-- ============================================================

CREATE TABLE IF NOT EXISTS CustomerNotification (
    ID            SERIAL         PRIMARY KEY,
    CustomerID    INT            NOT NULL,
    PromotionID   INT            NULL DEFAULT NULL,
    Title         VARCHAR(200)   NOT NULL,
    Message       VARCHAR(500)   NOT NULL,
    PromoCode     VARCHAR(20)    NOT NULL,
    DiscountValue DECIMAL(10,2)  NULL DEFAULT NULL,
    DiscountType  VARCHAR(20)    NULL DEFAULT NULL,
    IsRead        BOOLEAN        NOT NULL DEFAULT FALSE,
    CreatedAt     TIMESTAMP      NOT NULL DEFAULT NOW(),
    ExpiresAt     TIMESTAMP      NULL DEFAULT NULL,
    CONSTRAINT fk_cn_customer  FOREIGN KEY (CustomerID)  REFERENCES Customer(CustomerID)   ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT fk_cn_promo     FOREIGN KEY (PromotionID) REFERENCES Promotion(PromotionID) ON UPDATE CASCADE ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_cn_customer ON CustomerNotification(CustomerID);
