-- ============================================================
-- FEMVED PLATFORM — COMPLETE POSTGRESQL SCHEMA
-- Version: 1.0 | February 2026
-- Run this file in order. All sections are labelled.
-- ============================================================

-- ─── 0. SETUP ────────────────────────────────────────────────────────────────
-- Create the database (run this as superuser ONCE, then connect to femved_db)
CREATE DATABASE femved_db
    WITH ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8'
    TEMPLATE = template0;

-- Connect to femved_db before running everything below
-- \c femved_db

-- Enable UUID generation
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ─── 1. ROLES ────────────────────────────────────────────────────────────────
CREATE TABLE roles (
    id   SMALLINT     NOT NULL,
    name VARCHAR(50)  NOT NULL,
    CONSTRAINT pk_roles PRIMARY KEY (id)
);

-- ─── 2. USERS ────────────────────────────────────────────────────────────────
CREATE TABLE users (
    id                  UUID          NOT NULL DEFAULT gen_random_uuid(),
    email               VARCHAR(255)  NOT NULL,
    password_hash       TEXT          NOT NULL,
    role_id             SMALLINT      NOT NULL,
    first_name          VARCHAR(100)  NOT NULL,
    last_name           VARCHAR(100)  NOT NULL,

    -- Country stored two ways: dial code (+91) and ISO code (IN)
    -- country_dial_code is what user types at registration e.g. +91, +44, +1
    -- country_iso_code  is derived from dial code for payment/currency logic
    country_dial_code   VARCHAR(10)   NULL,       -- e.g. +91, +44, +1
    country_iso_code    VARCHAR(5)    NULL,        -- e.g. IN, GB, US

    mobile_number       VARCHAR(20)   NULL,        -- number without dial code
    full_mobile         VARCHAR(30)   NULL,        -- dial code + number combined e.g. +917890001234
    is_mobile_verified  BOOLEAN       NOT NULL DEFAULT FALSE,
    is_email_verified   BOOLEAN       NOT NULL DEFAULT FALSE,  -- not enforced at purchase (MVP)
    whatsapp_opt_in     BOOLEAN       NOT NULL DEFAULT FALSE,
    is_active           BOOLEAN       NOT NULL DEFAULT TRUE,
    is_deleted          BOOLEAN       NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ   NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_users PRIMARY KEY (id),
    CONSTRAINT uq_users_email UNIQUE (email),
    CONSTRAINT fk_users_role FOREIGN KEY (role_id) REFERENCES roles (id)
);

CREATE INDEX idx_users_email      ON users (email);
CREATE INDEX idx_users_role_id    ON users (role_id);
CREATE INDEX idx_users_iso_code   ON users (country_iso_code);

-- ─── 3. REFRESH TOKENS ───────────────────────────────────────────────────────
CREATE TABLE refresh_tokens (
    id          UUID        NOT NULL DEFAULT gen_random_uuid(),
    user_id     UUID        NOT NULL,
    token_hash  TEXT        NOT NULL,
    expires_at  TIMESTAMPTZ NOT NULL,
    is_revoked  BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_refresh_tokens PRIMARY KEY (id),
    CONSTRAINT fk_refresh_tokens_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens (user_id);

-- ─── 4. PASSWORD RESET TOKENS ────────────────────────────────────────────────
CREATE TABLE password_reset_tokens (
    id          UUID        NOT NULL DEFAULT gen_random_uuid(),
    user_id     UUID        NOT NULL,
    token_hash  TEXT        NOT NULL,
    expires_at  TIMESTAMPTZ NOT NULL,
    is_used     BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_password_reset_tokens PRIMARY KEY (id),
    CONSTRAINT fk_password_reset_tokens_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE INDEX idx_password_reset_tokens_user_id ON password_reset_tokens (user_id);

-- ─── 5. GUIDED DOMAINS ───────────────────────────────────────────────────────
CREATE TABLE guided_domains (
    id          UUID         NOT NULL DEFAULT gen_random_uuid(),
    name        VARCHAR(200) NOT NULL,
    slug        VARCHAR(200) NOT NULL,
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
    sort_order  INT          NOT NULL DEFAULT 0,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_guided_domains PRIMARY KEY (id),
    CONSTRAINT uq_guided_domains_slug UNIQUE (slug)
);

-- ─── 6. GUIDED CATEGORIES ────────────────────────────────────────────────────
CREATE TABLE guided_categories (
    id              UUID         NOT NULL DEFAULT gen_random_uuid(),
    domain_id       UUID         NOT NULL,
    parent_id       UUID         NULL,               -- NULL = top-level category
    name            VARCHAR(200) NOT NULL,
    slug            VARCHAR(200) NOT NULL,
    category_type   VARCHAR(100) NOT NULL,           -- Display label e.g. "Hormonal Health Support"
    hero_title      VARCHAR(300) NOT NULL,
    hero_subtext    TEXT         NOT NULL,
    cta_label       VARCHAR(100) NULL,
    cta_link        VARCHAR(300) NULL,
    page_header     VARCHAR(300) NULL,
    image_url       TEXT         NULL,
    sort_order      INT          NOT NULL DEFAULT 0,
    is_active       BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_guided_categories PRIMARY KEY (id),
    CONSTRAINT uq_guided_categories_slug UNIQUE (slug),
    CONSTRAINT fk_guided_categories_domain FOREIGN KEY (domain_id) REFERENCES guided_domains (id),
    CONSTRAINT fk_guided_categories_parent FOREIGN KEY (parent_id) REFERENCES guided_categories (id)
);

CREATE INDEX idx_guided_categories_domain_id ON guided_categories (domain_id);
CREATE INDEX idx_guided_categories_parent_id ON guided_categories (parent_id);

-- ─── 7. CATEGORY WHAT'S INCLUDED ─────────────────────────────────────────────
CREATE TABLE category_whats_included (
    id           UUID  NOT NULL DEFAULT gen_random_uuid(),
    category_id  UUID  NOT NULL,
    item_text    TEXT  NOT NULL,
    sort_order   INT   NOT NULL DEFAULT 0,

    CONSTRAINT pk_category_whats_included PRIMARY KEY (id),
    CONSTRAINT fk_category_whats_included_category FOREIGN KEY (category_id) REFERENCES guided_categories (id) ON DELETE CASCADE
);

CREATE INDEX idx_category_whats_included_category_id ON category_whats_included (category_id);

-- ─── 8. CATEGORY KEY AREAS ───────────────────────────────────────────────────
CREATE TABLE category_key_areas (
    id           UUID         NOT NULL DEFAULT gen_random_uuid(),
    category_id  UUID         NOT NULL,
    area_text    VARCHAR(300) NOT NULL,
    sort_order   INT          NOT NULL DEFAULT 0,

    CONSTRAINT pk_category_key_areas PRIMARY KEY (id),
    CONSTRAINT fk_category_key_areas_category FOREIGN KEY (category_id) REFERENCES guided_categories (id) ON DELETE CASCADE
);

CREATE INDEX idx_category_key_areas_category_id ON category_key_areas (category_id);

-- ─── 9. EXPERTS ──────────────────────────────────────────────────────────────
CREATE TABLE experts (
    id                   UUID         NOT NULL DEFAULT gen_random_uuid(),
    user_id              UUID         NOT NULL,
    display_name         VARCHAR(200) NOT NULL,
    title                VARCHAR(200) NOT NULL,     -- e.g. "Naturopath & Herbalist"
    bio                  TEXT         NOT NULL,
    short_bio            VARCHAR(500) NULL,
    profile_image_url    TEXT         NULL,
    specialisations      TEXT[]       NULL,          -- PostgreSQL array
    years_experience     SMALLINT     NULL,
    credentials          TEXT[]       NULL,          -- Degrees, certifications
    location_country     VARCHAR(100) NULL,
    is_active            BOOLEAN      NOT NULL DEFAULT TRUE,
    is_deleted           BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at           TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at           TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_experts PRIMARY KEY (id),
    CONSTRAINT uq_experts_user_id UNIQUE (user_id),
    CONSTRAINT fk_experts_user FOREIGN KEY (user_id) REFERENCES users (id)
);

CREATE INDEX idx_experts_user_id ON experts (user_id);

-- ─── 10. PROGRAMS ────────────────────────────────────────────────────────────
-- Status flow: DRAFT → PENDING_REVIEW → PUBLISHED → ARCHIVED
-- Expert creates as DRAFT, submits for review, Admin publishes.
CREATE TABLE programs (
    id                  UUID         NOT NULL DEFAULT gen_random_uuid(),
    category_id         UUID         NOT NULL,
    expert_id           UUID         NOT NULL,
    name                VARCHAR(300) NOT NULL,
    slug                VARCHAR(300) NOT NULL,
    grid_description    VARCHAR(500) NOT NULL,
    grid_image_url      TEXT         NULL,
    overview            TEXT         NOT NULL,
    status              VARCHAR(30)  NOT NULL DEFAULT 'DRAFT',
        -- DRAFT | PENDING_REVIEW | PUBLISHED | ARCHIVED
    start_date          DATE         NULL,
    end_date            DATE         NULL,
    is_active           BOOLEAN      NOT NULL DEFAULT TRUE,
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,
    sort_order          INT          NOT NULL DEFAULT 0,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_programs PRIMARY KEY (id),
    CONSTRAINT uq_programs_slug UNIQUE (slug),
    CONSTRAINT fk_programs_category FOREIGN KEY (category_id) REFERENCES guided_categories (id),
    CONSTRAINT fk_programs_expert   FOREIGN KEY (expert_id)   REFERENCES experts (id),
    CONSTRAINT chk_programs_status CHECK (status IN ('DRAFT','PENDING_REVIEW','PUBLISHED','ARCHIVED'))
);

CREATE INDEX idx_programs_category_id ON programs (category_id);
CREATE INDEX idx_programs_expert_id   ON programs (expert_id);
CREATE INDEX idx_programs_status      ON programs (status);

-- ─── 11. PROGRAM WHAT YOU GET ────────────────────────────────────────────────
CREATE TABLE program_what_you_get (
    id          UUID  NOT NULL DEFAULT gen_random_uuid(),
    program_id  UUID  NOT NULL,
    item_text   TEXT  NOT NULL,
    sort_order  INT   NOT NULL DEFAULT 0,

    CONSTRAINT pk_program_what_you_get PRIMARY KEY (id),
    CONSTRAINT fk_program_what_you_get_program FOREIGN KEY (program_id) REFERENCES programs (id) ON DELETE CASCADE
);

CREATE INDEX idx_program_what_you_get_program_id ON program_what_you_get (program_id);

-- ─── 12. PROGRAM WHO IS THIS FOR ─────────────────────────────────────────────
CREATE TABLE program_who_is_this_for (
    id          UUID  NOT NULL DEFAULT gen_random_uuid(),
    program_id  UUID  NOT NULL,
    item_text   TEXT  NOT NULL,
    sort_order  INT   NOT NULL DEFAULT 0,

    CONSTRAINT pk_program_who_is_this_for PRIMARY KEY (id),
    CONSTRAINT fk_program_who_is_this_for_program FOREIGN KEY (program_id) REFERENCES programs (id) ON DELETE CASCADE
);

CREATE INDEX idx_program_who_is_this_for_program_id ON program_who_is_this_for (program_id);

-- ─── 13. PROGRAM TAGS ────────────────────────────────────────────────────────
-- Supports FE filter bar (stress, hormones, gut health, etc.)
CREATE TABLE program_tags (
    id          UUID         NOT NULL DEFAULT gen_random_uuid(),
    program_id  UUID         NOT NULL,
    tag         VARCHAR(100) NOT NULL,

    CONSTRAINT pk_program_tags PRIMARY KEY (id),
    CONSTRAINT fk_program_tags_program FOREIGN KEY (program_id) REFERENCES programs (id) ON DELETE CASCADE,
    CONSTRAINT uq_program_tags_program_tag UNIQUE (program_id, tag)
);

CREATE INDEX idx_program_tags_program_id ON program_tags (program_id);
CREATE INDEX idx_program_tags_tag        ON program_tags (tag);

-- ─── 14. PROGRAM TESTIMONIALS ────────────────────────────────────────────────
CREATE TABLE program_testimonials (
    id              UUID         NOT NULL DEFAULT gen_random_uuid(),
    program_id      UUID         NOT NULL,
    reviewer_name   VARCHAR(200) NOT NULL,
    reviewer_title  VARCHAR(200) NULL,     -- e.g. "Mother of two, London"
    review_text     TEXT         NOT NULL,
    rating          SMALLINT     NULL,     -- 1-5 stars, nullable if not using stars
    is_active       BOOLEAN      NOT NULL DEFAULT TRUE,
    sort_order      INT          NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_program_testimonials PRIMARY KEY (id),
    CONSTRAINT fk_program_testimonials_program FOREIGN KEY (program_id) REFERENCES programs (id) ON DELETE CASCADE,
    CONSTRAINT chk_program_testimonials_rating CHECK (rating IS NULL OR (rating >= 1 AND rating <= 5))
);

CREATE INDEX idx_program_testimonials_program_id ON program_testimonials (program_id);

-- ─── 15. PROGRAM DURATIONS ───────────────────────────────────────────────────
CREATE TABLE program_durations (
    id          UUID        NOT NULL DEFAULT gen_random_uuid(),
    program_id  UUID        NOT NULL,
    label       VARCHAR(50) NOT NULL,    -- e.g. "4 weeks", "6 weeks"
    weeks       SMALLINT    NOT NULL,
    is_active   BOOLEAN     NOT NULL DEFAULT TRUE,
    sort_order  INT         NOT NULL DEFAULT 0,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_program_durations PRIMARY KEY (id),
    CONSTRAINT fk_program_durations_program FOREIGN KEY (program_id) REFERENCES programs (id) ON DELETE CASCADE
);

CREATE INDEX idx_program_durations_program_id ON program_durations (program_id);

-- ─── 16. DURATION PRICES (LOCATION-SPECIFIC) ─────────────────────────────────
CREATE TABLE duration_prices (
    id               UUID          NOT NULL DEFAULT gen_random_uuid(),
    duration_id      UUID          NOT NULL,
    location_code    VARCHAR(5)    NOT NULL,    -- IN | GB | US
    amount           DECIMAL(12,2) NOT NULL,
    currency_code    VARCHAR(3)    NOT NULL,    -- INR | GBP | USD
    currency_symbol  VARCHAR(5)    NOT NULL,    -- ₹  | £   | $
    is_active        BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_duration_prices PRIMARY KEY (id),
    CONSTRAINT uq_duration_prices_duration_location UNIQUE (duration_id, location_code),
    CONSTRAINT fk_duration_prices_duration FOREIGN KEY (duration_id) REFERENCES program_durations (id) ON DELETE CASCADE,
    CONSTRAINT chk_duration_prices_location CHECK (location_code IN ('IN','GB','US'))
);

CREATE INDEX idx_duration_prices_duration_location ON duration_prices (duration_id, location_code);

-- ─── 17. COUPONS ─────────────────────────────────────────────────────────────
CREATE TABLE coupons (
    id              UUID          NOT NULL DEFAULT gen_random_uuid(),
    code            VARCHAR(50)   NOT NULL,
    discount_type   VARCHAR(20)   NOT NULL,    -- PERCENTAGE | FLAT
    discount_value  DECIMAL(10,2) NOT NULL,
    max_uses        INT           NULL,         -- NULL = unlimited
    used_count      INT           NOT NULL DEFAULT 0,
    valid_from      TIMESTAMPTZ   NULL,
    valid_until     TIMESTAMPTZ   NULL,
    is_active       BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_coupons PRIMARY KEY (id),
    CONSTRAINT uq_coupons_code UNIQUE (code),
    CONSTRAINT chk_coupons_discount_type CHECK (discount_type IN ('PERCENTAGE','FLAT'))
);

-- ─── 18. ORDERS ──────────────────────────────────────────────────────────────
CREATE TABLE orders (
    id                   UUID          NOT NULL DEFAULT gen_random_uuid(),
    user_id              UUID          NOT NULL,
    duration_id          UUID          NOT NULL,
    duration_price_id    UUID          NOT NULL,
    amount_paid          DECIMAL(12,2) NOT NULL,
    currency_code        VARCHAR(3)    NOT NULL,
    location_code        VARCHAR(5)    NOT NULL,
    coupon_id            UUID          NULL,
    discount_amount      DECIMAL(12,2) NOT NULL DEFAULT 0,
    status               VARCHAR(30)   NOT NULL DEFAULT 'PENDING',
        -- PENDING | PAID | FAILED | REFUNDED
    payment_gateway      VARCHAR(30)   NOT NULL,    -- CASHFREE | PAYPAL
    idempotency_key      VARCHAR(100)  NOT NULL,    -- Client-provided to prevent duplicate orders
    gateway_order_id     VARCHAR(200)  NULL,
    gateway_payment_id   VARCHAR(200)  NULL,
    gateway_response     JSONB         NULL,        -- Full webhook payload for audit
    failure_reason       TEXT          NULL,
    created_at           TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at           TIMESTAMPTZ   NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_orders PRIMARY KEY (id),
    CONSTRAINT uq_orders_idempotency_key UNIQUE (idempotency_key),
    CONSTRAINT fk_orders_user           FOREIGN KEY (user_id)           REFERENCES users (id),
    CONSTRAINT fk_orders_duration       FOREIGN KEY (duration_id)       REFERENCES program_durations (id),
    CONSTRAINT fk_orders_duration_price FOREIGN KEY (duration_price_id) REFERENCES duration_prices (id),
    CONSTRAINT fk_orders_coupon         FOREIGN KEY (coupon_id)         REFERENCES coupons (id),
    CONSTRAINT chk_orders_status CHECK (status IN ('PENDING','PAID','FAILED','REFUNDED')),
    CONSTRAINT chk_orders_gateway CHECK (payment_gateway IN ('CASHFREE','PAYPAL'))
);

CREATE INDEX idx_orders_user_id     ON orders (user_id);
CREATE INDEX idx_orders_status      ON orders (status);
CREATE INDEX idx_orders_created_at  ON orders (created_at DESC);

-- ─── 19. REFUNDS ─────────────────────────────────────────────────────────────
CREATE TABLE refunds (
    id                 UUID          NOT NULL DEFAULT gen_random_uuid(),
    order_id           UUID          NOT NULL,
    refund_amount      DECIMAL(12,2) NOT NULL,
    reason             TEXT          NULL,
    gateway_refund_id  VARCHAR(200)  NULL,
    status             VARCHAR(30)   NOT NULL DEFAULT 'PENDING',
        -- PENDING | COMPLETED | FAILED
    initiated_by       UUID          NOT NULL,    -- Admin user who triggered refund
    created_at         TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    updated_at         TIMESTAMPTZ   NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_refunds PRIMARY KEY (id),
    CONSTRAINT fk_refunds_order        FOREIGN KEY (order_id)      REFERENCES orders (id),
    CONSTRAINT fk_refunds_initiated_by FOREIGN KEY (initiated_by)  REFERENCES users (id),
    CONSTRAINT chk_refunds_status CHECK (status IN ('PENDING','COMPLETED','FAILED'))
);

CREATE INDEX idx_refunds_order_id ON refunds (order_id);

-- ─── 20. USER PROGRAM ACCESS ─────────────────────────────────────────────────
CREATE TABLE user_program_access (
    id            UUID        NOT NULL DEFAULT gen_random_uuid(),
    user_id       UUID        NOT NULL,
    order_id      UUID        NOT NULL,
    program_id    UUID        NOT NULL,
    duration_id   UUID        NOT NULL,
    expert_id     UUID        NOT NULL,
    status        VARCHAR(30) NOT NULL DEFAULT 'ACTIVE',
        -- ACTIVE | COMPLETED | CANCELLED
    reminder_sent BOOLEAN     NOT NULL DEFAULT FALSE,
    started_at    TIMESTAMPTZ NULL,
    completed_at  TIMESTAMPTZ NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_user_program_access PRIMARY KEY (id),
    CONSTRAINT fk_upa_user      FOREIGN KEY (user_id)     REFERENCES users (id),
    CONSTRAINT fk_upa_order     FOREIGN KEY (order_id)    REFERENCES orders (id),
    CONSTRAINT fk_upa_program   FOREIGN KEY (program_id)  REFERENCES programs (id),
    CONSTRAINT fk_upa_duration  FOREIGN KEY (duration_id) REFERENCES program_durations (id),
    CONSTRAINT fk_upa_expert    FOREIGN KEY (expert_id)   REFERENCES experts (id),
    CONSTRAINT chk_upa_status CHECK (status IN ('ACTIVE','COMPLETED','CANCELLED'))
);

CREATE INDEX idx_upa_user_id    ON user_program_access (user_id);
CREATE INDEX idx_upa_program_id ON user_program_access (program_id);
CREATE INDEX idx_upa_expert_id  ON user_program_access (expert_id);
CREATE INDEX idx_upa_status     ON user_program_access (status);

-- ─── 21. EXPERT PROGRESS UPDATES ─────────────────────────────────────────────
CREATE TABLE expert_progress_updates (
    id           UUID    NOT NULL DEFAULT gen_random_uuid(),
    access_id    UUID    NOT NULL,
    expert_id    UUID    NOT NULL,
    update_note  TEXT    NOT NULL,
    send_email   BOOLEAN NOT NULL DEFAULT FALSE,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_expert_progress_updates PRIMARY KEY (id),
    CONSTRAINT fk_epu_access  FOREIGN KEY (access_id)  REFERENCES user_program_access (id),
    CONSTRAINT fk_epu_expert  FOREIGN KEY (expert_id)  REFERENCES experts (id)
);

CREATE INDEX idx_epu_access_id ON expert_progress_updates (access_id);

-- ─── 22. NOTIFICATION LOG ────────────────────────────────────────────────────
CREATE TABLE notification_log (
    id            UUID         NOT NULL DEFAULT gen_random_uuid(),
    user_id       UUID         NULL,
    type          VARCHAR(30)  NOT NULL,       -- EMAIL | WHATSAPP | SMS
    template_key  VARCHAR(100) NOT NULL,       -- e.g. purchase_success
    recipient     VARCHAR(300) NOT NULL,       -- email address or phone number
    status        VARCHAR(20)  NOT NULL DEFAULT 'SENT',   -- SENT | FAILED
    error_message TEXT         NULL,
    payload       JSONB        NULL,           -- template variables (no PII)
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_notification_log PRIMARY KEY (id),
    CONSTRAINT fk_notification_log_user FOREIGN KEY (user_id) REFERENCES users (id),
    CONSTRAINT chk_notification_log_type   CHECK (type   IN ('EMAIL','WHATSAPP','SMS')),
    CONSTRAINT chk_notification_log_status CHECK (status IN ('SENT','FAILED'))
);

CREATE INDEX idx_notification_log_user_id ON notification_log (user_id);
CREATE INDEX idx_notification_log_type    ON notification_log (type);

-- ─── 23. ADMIN AUDIT LOG ─────────────────────────────────────────────────────
CREATE TABLE admin_audit_log (
    id             UUID         NOT NULL DEFAULT gen_random_uuid(),
    admin_user_id  UUID         NOT NULL,
    action         VARCHAR(100) NOT NULL,       -- e.g. UPDATE_PRICE, PUBLISH_PROGRAM
    entity_type    VARCHAR(100) NOT NULL,       -- e.g. duration_prices, programs
    entity_id      UUID         NULL,
    before_value   JSONB        NULL,
    after_value    JSONB        NULL,
    ip_address     VARCHAR(50)  NULL,
    created_at     TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_admin_audit_log PRIMARY KEY (id),
    CONSTRAINT fk_admin_audit_log_user FOREIGN KEY (admin_user_id) REFERENCES users (id)
);

CREATE INDEX idx_admin_audit_log_admin_user_id         ON admin_audit_log (admin_user_id);
CREATE INDEX idx_admin_audit_log_entity_type_entity_id ON admin_audit_log (entity_type, entity_id);
CREATE INDEX idx_admin_audit_log_created_at            ON admin_audit_log (created_at DESC);

-- ─── 24. GDPR DATA DELETION REQUESTS ─────────────────────────────────────────
-- For UK/EU users exercising right to erasure.
-- Actual deletion is handled by a background job that processes PENDING requests.
CREATE TABLE gdpr_deletion_requests (
    id            UUID        NOT NULL DEFAULT gen_random_uuid(),
    user_id       UUID        NOT NULL,
    requested_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    status        VARCHAR(30) NOT NULL DEFAULT 'PENDING',
        -- PENDING | PROCESSING | COMPLETED | REJECTED
    completed_at  TIMESTAMPTZ NULL,
    rejection_reason TEXT      NULL,
    processed_by  UUID        NULL,     -- Admin who handled it

    CONSTRAINT pk_gdpr_deletion_requests PRIMARY KEY (id),
    CONSTRAINT fk_gdpr_user FOREIGN KEY (user_id) REFERENCES users (id)
);

CREATE INDEX idx_gdpr_deletion_requests_user_id ON gdpr_deletion_requests (user_id);
CREATE INDEX idx_gdpr_deletion_requests_status  ON gdpr_deletion_requests (status);


-- ============================================================
-- SEED DATA
-- ============================================================

-- ─── SEED: ROLES ─────────────────────────────────────────────────────────────
INSERT INTO roles (id, name) VALUES
    (1, 'Admin'),
    (2, 'Expert'),
    (3, 'User');

-- ─── SEED: GUIDED DOMAIN ─────────────────────────────────────────────────────
INSERT INTO guided_domains (id, name, slug, is_active, sort_order) VALUES
    ('11111111-0000-0000-0000-000000000001', 'Guided 1:1 Care', 'guided-1-1-care', TRUE, 1);

-- ─── SEED: GUIDED CATEGORIES ─────────────────────────────────────────────────
-- Category 1: Hormonal Health Support
INSERT INTO guided_categories (
    id, domain_id, parent_id, name, slug, category_type,
    hero_title, hero_subtext, cta_label, cta_link, page_header, sort_order
) VALUES (
    '22222222-0000-0000-0000-000000000001',
    '11111111-0000-0000-0000-000000000001',
    NULL,
    'Hormonal Health Support',
    'hormonal-health-support',
    'Hormonal Health Support',
    'Get Guided Hormonal Care',
    'When hormonal changes feel overwhelming and online advice leaves you confused, you deserve guidance you can trust. Get one-to-one support from experienced practitioners and create a personalised wellness plan that fits your life, accessible from anywhere.',
    'Book Your Program',
    '/guided/hormonal-health-support',
    'Choose and book the guided journey that best fits your needs, goals, and life right now.',
    1
);

-- Category 2: Mental and Spiritual Wellbeing
INSERT INTO guided_categories (
    id, domain_id, parent_id, name, slug, category_type,
    hero_title, hero_subtext, cta_label, cta_link, page_header, sort_order
) VALUES (
    '22222222-0000-0000-0000-000000000002',
    '11111111-0000-0000-0000-000000000001',
    NULL,
    'Mental and Spiritual Wellbeing',
    'mental-spiritual-wellbeing',
    'Mind and Spirituality',
    'Begin Your Personal Mind Support',
    'When constant advice and quick fixes leave you feeling overwhelmed, the right guidance helps you slow down. Get one-to-one support from experienced counsellors and spiritual practitioners to find emotional clarity and inner balance, from the comfort of your home.',
    'Book Your Program',
    '/guided/mental-spiritual-wellbeing',
    'Choose and book the guided journey that best fits your needs, goals, and life right now.',
    2
);

-- Category 3: Longevity and Healthy Ageing
INSERT INTO guided_categories (
    id, domain_id, parent_id, name, slug, category_type,
    hero_title, hero_subtext, cta_label, cta_link, page_header, sort_order
) VALUES (
    '22222222-0000-0000-0000-000000000003',
    '11111111-0000-0000-0000-000000000001',
    NULL,
    'Longevity and Healthy Ageing',
    'longevity-healthy-ageing',
    'Longevity',
    'Plan Your Long-Term Health',
    'When longevity trends and conflicting wellness advice leave you confused, the right guidance brings clarity. Work one-to-one with experienced experts to create a personalised longevity plan rooted in science, lifestyle, and prevention, accessible from home.',
    'Book Your Program',
    '/guided/longevity-healthy-ageing',
    'Choose and book the guided journey that best fits your needs, goals, and life right now.',
    3
);

-- Category 4: Fitness and Personal Care Support
INSERT INTO guided_categories (
    id, domain_id, parent_id, name, slug, category_type,
    hero_title, hero_subtext, cta_label, cta_link, page_header, sort_order
) VALUES (
    '22222222-0000-0000-0000-000000000004',
    '11111111-0000-0000-0000-000000000001',
    NULL,
    'Fitness and Personal Care Support',
    'fitness-personal-care',
    'Fitness and Body Care',
    'Book Your Personal Wellness Program',
    'When online fitness advice leaves you unsure what your body truly needs, personalised guidance makes the difference. Get one-to-one support to build a fitness plan that respects your strength, recovery, and rhythm, from the comfort of your home.',
    'Book Your Program',
    '/guided/fitness-personal-care',
    'Choose and book the guided journey that best fits your needs, goals, and life right now.',
    4
);

-- ─── SEED: CATEGORY WHAT'S INCLUDED — Hormonal ───────────────────────────────
INSERT INTO category_whats_included (category_id, item_text, sort_order) VALUES
    ('22222222-0000-0000-0000-000000000001', 'In-depth health assessment with a hormonal wellness expert', 1),
    ('22222222-0000-0000-0000-000000000001', 'One-to-one guidance and ongoing support', 2),
    ('22222222-0000-0000-0000-000000000001', 'Personalised 4–12 week wellness plan', 3),
    ('22222222-0000-0000-0000-000000000001', 'Care tailored to your hormones, life stage, and goals', 4),
    ('22222222-0000-0000-0000-000000000001', 'Customised diet and lifestyle plan with shopping guidance', 5),
    ('22222222-0000-0000-0000-000000000001', 'Support for concerns like PCOS, endometriosis, fertility, and cycle health', 6);

-- ─── SEED: CATEGORY KEY AREAS — Hormonal ─────────────────────────────────────
INSERT INTO category_key_areas (category_id, area_text, sort_order) VALUES
    ('22222222-0000-0000-0000-000000000001', 'Preconception and fertility nutrition', 1),
    ('22222222-0000-0000-0000-000000000001', 'Pregnancy and postpartum support', 2),
    ('22222222-0000-0000-0000-000000000001', 'PCOS, endometriosis, and cycle health', 3),
    ('22222222-0000-0000-0000-000000000001', 'Hormone and stress balance', 4),
    ('22222222-0000-0000-0000-000000000001', 'Intuitive eating and metabolic health', 5),
    ('22222222-0000-0000-0000-000000000001', 'Menopause and perimenopause care', 6),
    ('22222222-0000-0000-0000-000000000001', 'Diabetes and weight management', 7),
    ('22222222-0000-0000-0000-000000000001', 'Life stage hormonal guidance', 8);

-- ─── SEED: EXPERT USERS ──────────────────────────────────────────────────────
-- Passwords are placeholder hashes — reset via admin panel after deploy
-- BCrypt hash of "FemVed@Expert2026" (work factor 12)
INSERT INTO users (id, email, password_hash, role_id, first_name, last_name, country_iso_code, country_dial_code, is_email_verified, is_active) VALUES
    ('33333333-0000-0000-0000-000000000001',
     'prathima@femved.com',
     '$2a$12$PLACEHOLDER_HASH_PRATHIMA_CHANGE_ON_FIRST_LOGIN',
     2, 'Prathima', 'Nagesh', 'IN', '+91', TRUE, TRUE),
    ('33333333-0000-0000-0000-000000000002',
     'kimberly@femved.com',
     '$2a$12$PLACEHOLDER_HASH_KIMBERLY_CHANGE_ON_FIRST_LOGIN',
     2, 'Kimberly', 'Parsons', 'GB', '+44', TRUE, TRUE);

-- ─── SEED: EXPERT PROFILES ───────────────────────────────────────────────────
INSERT INTO experts (id, user_id, display_name, title, bio, short_bio, specialisations, years_experience, credentials, location_country) VALUES
    (
        '44444444-0000-0000-0000-000000000001',
        '33333333-0000-0000-0000-000000000001',
        'Dr. Prathima Nagesh',
        'Ayurvedic Physician & Women''s Health Specialist',
        'Dr. Prathima is a distinguished BAMS, MD Ayurvedic physician with over 25 years of clinical experience, specialising in women''s health and holistic well-being. A trained Clinical Researcher (GCSRT) from Harvard Medical School, she blends classical Ayurvedic wisdom with evidence-informed clinical practice. Over the years, she has successfully supported women through complex health challenges including menstrual disorders, hormonal imbalances, fertility concerns, and chronic lifestyle-related conditions.',
        'BAMS, MD Ayurvedic physician with 25+ years of experience in women''s hormonal health and Ayurvedic medicine.',
        ARRAY['Hormonal Health','Ayurveda','Women''s Wellness','Perimenopause','PCOS','Fertility'],
        25,
        ARRAY['BAMS','MD Ayurveda','GCSRT - Harvard Medical School'],
        'India'
    ),
    (
        '44444444-0000-0000-0000-000000000002',
        '33333333-0000-0000-0000-000000000002',
        'Kimberly Parsons',
        'Naturopath, Herbalist & Author',
        'Kimberly Parsons is the founder of Naturalli.me, an all-natural clinic focused on women''s hormonal health. Australian-born and trained, she holds a Bachelor of Health Science in Naturopathy and brings over 20 years of experience supporting women through herbal medicine, nutrition, and lifestyle care. She is the internationally best-selling author of The Yoga Kitchen series and creator of the Naturalli 28Days app. Kimberly has led wellness retreats around the world, sharing her philosophy of healing through food, herbs, and rhythm-based living.',
        'Naturopath and herbalist with 20+ years experience. Best-selling author of The Yoga Kitchen series. Founder of Naturalli.me.',
        ARRAY['Naturopathy','Herbal Medicine','PCOS','Hormonal Health','Metabolism','Menopause'],
        20,
        ARRAY['Bachelor of Health Science in Naturopathy'],
        'United Kingdom'
    );

-- ─── SEED: PROGRAMS ──────────────────────────────────────────────────────────

-- Program 1: Break the Stress–Hormone–Health Triangle (Dr. Prathima, 6 weeks)
INSERT INTO programs (id, category_id, expert_id, name, slug, grid_description, overview, status, sort_order) VALUES (
    '55555555-0000-0000-0000-000000000001',
    '22222222-0000-0000-0000-000000000001',
    '44444444-0000-0000-0000-000000000001',
    'Break the Stress–Hormone–Health Triangle',
    'break-stress-hormone-health-triangle',
    'A 6-week Ayurvedic program to reset stress patterns, restore hormonal balance, and rebuild daily rhythm.',
    'Did you know that chronic stress can quietly disrupt hormonal balance, digestion, sleep, and long-term vitality? In this 6-week guided program, you will move through structured, personalised phases designed to regulate the stress response, improve digestion and hormone metabolism, nourish reproductive and adrenal health, and restore circadian rhythm. Each phase introduces practical Ayurvedic lifestyle tools, self-care rituals, dietary guidance, and stress-regulation techniques that fit into everyday life.',
    'PUBLISHED',
    1
);

-- Program 2: Balancing Perimenopause with Ayurveda (Dr. Prathima, 4 weeks)
INSERT INTO programs (id, category_id, expert_id, name, slug, grid_description, overview, status, sort_order) VALUES (
    '55555555-0000-0000-0000-000000000002',
    '22222222-0000-0000-0000-000000000001',
    '44444444-0000-0000-0000-000000000001',
    'Balancing & Managing Perimenopause with Ayurveda',
    'balancing-perimenopause-ayurveda',
    'A 4-week Ayurvedic program to stabilise hormonal transitions, support emotional balance, and strengthen long-term vitality.',
    'In Ayurveda, perimenopause reflects natural changes in reproductive tissues and dosha balance, often marked by fluctuations in Vata and Pitta. In this 4-week guided program, you will move through personalised Ayurvedic phases designed to understand perimenopausal changes, regulate hormonal fluctuations, support digestion and tissue nourishment, and establish lifestyle rhythms that ease this transition.',
    'PUBLISHED',
    2
);

-- Program 3: The Metabolic PCOS Reset (Kimberly, 8 weeks)
INSERT INTO programs (id, category_id, expert_id, name, slug, grid_description, overview, status, sort_order) VALUES (
    '55555555-0000-0000-0000-000000000003',
    '22222222-0000-0000-0000-000000000001',
    '44444444-0000-0000-0000-000000000002',
    'The Metabolic PCOS Reset',
    'metabolic-pcos-reset',
    'An 8-week naturopath-led program to restore metabolic balance, regulate hormones, and support fertility.',
    'PCOS is not just a reproductive condition but a metabolic and hormonal imbalance often driven by insulin resistance, androgen excess, inflammation, and chronic stress. In this 8-week program, you will work through a structured, personalised approach designed to identify the metabolic drivers behind your PCOS and reverse them using herbal medicine, nutrition therapy, and hormone-supportive lifestyle strategies.',
    'PUBLISHED',
    3
);

-- Program 4: 28-Day Mastering Midlife Metabolism Method (Kimberly, 4 weeks)
INSERT INTO programs (id, category_id, expert_id, name, slug, grid_description, overview, status, sort_order) VALUES (
    '55555555-0000-0000-0000-000000000004',
    '22222222-0000-0000-0000-000000000001',
    '44444444-0000-0000-0000-000000000002',
    '28-Day Mastering Midlife Metabolism Method',
    '28-day-midlife-metabolism-method',
    'A 28-day food-based protocol to balance hormones, reset metabolism, and restore energy stability during midlife.',
    'Weight gain, fatigue, and metabolic slowdown during midlife are rarely just about calories or exercise. As women transition through perimenopause and menopause, natural shifts in oestrogen, progesterone, insulin, and cortisol significantly impact metabolism. This method integrates cycle-synced nutrition, strategic fasting windows, seed cycling, and lifestyle rhythm correction to support hormonal balance without extreme dieting.',
    'PUBLISHED',
    4
);

-- Program 5: The Happy Hormone Method (Kimberly, 8 weeks)
INSERT INTO programs (id, category_id, expert_id, name, slug, grid_description, overview, status, sort_order) VALUES (
    '55555555-0000-0000-0000-000000000005',
    '22222222-0000-0000-0000-000000000001',
    '44444444-0000-0000-0000-000000000002',
    'The Happy Hormone Method',
    'happy-hormone-method',
    'An 8-week root-cause naturopath program to rebalance hormones, restore energy, and reclaim your natural flow.',
    'Hormonal symptoms like PMS, bloating, mood swings, fatigue, and sleep disruption are often signals of deeper imbalances involving inflammation, gut health, stress hormones, and nutrient deficiencies. In this 8-week 1:1 program, you will follow a structured, personalised treatment pathway to identify root causes and support healing through targeted herbal medicine, nutrition therapy, and lifestyle rhythm correction.',
    'PUBLISHED',
    5
);

-- ─── SEED: WHAT YOU GET ───────────────────────────────────────────────────────
-- Program 1
INSERT INTO program_what_you_get (program_id, item_text, sort_order) VALUES
    ('55555555-0000-0000-0000-000000000001', 'Personalised Dosha, lifestyle, and hormonal pattern assessment', 1),
    ('55555555-0000-0000-0000-000000000001', 'Step-by-step weekly structure to regulate stress and support hormone balance', 2),
    ('55555555-0000-0000-0000-000000000001', 'Ayurvedic self-care rituals including daily rhythm and nervous system calming practices', 3),
    ('55555555-0000-0000-0000-000000000001', 'Practical breathwork and relaxation techniques to stabilise stress hormones', 4),
    ('55555555-0000-0000-0000-000000000001', 'Hormone-supportive dietary and digestion strengthening guidelines', 5),
    ('55555555-0000-0000-0000-000000000001', 'Long-term maintenance plan to sustain hormonal stability', 6);

-- Program 3
INSERT INTO program_what_you_get (program_id, item_text, sort_order) VALUES
    ('55555555-0000-0000-0000-000000000003', 'Personalised herbal tincture formulas designed to address your PCOS root cause', 1),
    ('55555555-0000-0000-0000-000000000003', 'Custom supplement protocol to support metabolic and hormonal balance', 2),
    ('55555555-0000-0000-0000-000000000003', 'Tailored 5-day metabolic cleanse to reset blood sugar and inflammation pathways', 3),
    ('55555555-0000-0000-0000-000000000003', '28-day hormone-supportive metabolic meal plan', 4),
    ('55555555-0000-0000-0000-000000000003', 'Identification of your PCOS subtype: insulin-driven, inflammatory, or androgen-dominant', 5);

-- ─── SEED: WHO IS THIS FOR ────────────────────────────────────────────────────
INSERT INTO program_who_is_this_for (program_id, item_text, sort_order) VALUES
    ('55555555-0000-0000-0000-000000000001', 'Women experiencing hormonal imbalance, irregular cycles, PMS, or fatigue', 1),
    ('55555555-0000-0000-0000-000000000001', 'Individuals dealing with chronic stress, burnout, or sleep disturbances', 2),
    ('55555555-0000-0000-0000-000000000001', 'Women navigating thyroid, metabolic, or adrenal health concerns', 3),
    ('55555555-0000-0000-0000-000000000001', 'Anyone wanting structured, sustainable lifestyle tools rooted in Ayurveda', 4);

INSERT INTO program_who_is_this_for (program_id, item_text, sort_order) VALUES
    ('55555555-0000-0000-0000-000000000003', 'Women aged 25–40 diagnosed with PCOS or strongly suspecting PCOS', 1),
    ('55555555-0000-0000-0000-000000000003', 'Women experiencing irregular or absent cycles, fertility challenges, or hormonal acne', 2),
    ('55555555-0000-0000-0000-000000000003', 'Individuals struggling with stubborn weight gain, sugar cravings, or insulin resistance', 3),
    ('55555555-0000-0000-0000-000000000003', 'Women preparing for conception and seeking to optimise reproductive health', 4);

-- ─── SEED: PROGRAM TAGS ───────────────────────────────────────────────────────
INSERT INTO program_tags (program_id, tag) VALUES
    ('55555555-0000-0000-0000-000000000001', 'stress'),
    ('55555555-0000-0000-0000-000000000001', 'hormones'),
    ('55555555-0000-0000-0000-000000000001', 'ayurveda'),
    ('55555555-0000-0000-0000-000000000001', 'sleep'),
    ('55555555-0000-0000-0000-000000000002', 'perimenopause'),
    ('55555555-0000-0000-0000-000000000002', 'hormones'),
    ('55555555-0000-0000-0000-000000000002', 'ayurveda'),
    ('55555555-0000-0000-0000-000000000002', 'ageing'),
    ('55555555-0000-0000-0000-000000000003', 'pcos'),
    ('55555555-0000-0000-0000-000000000003', 'hormones'),
    ('55555555-0000-0000-0000-000000000003', 'fertility'),
    ('55555555-0000-0000-0000-000000000003', 'metabolism'),
    ('55555555-0000-0000-0000-000000000004', 'menopause'),
    ('55555555-0000-0000-0000-000000000004', 'metabolism'),
    ('55555555-0000-0000-0000-000000000004', 'weight'),
    ('55555555-0000-0000-0000-000000000004', 'hormones'),
    ('55555555-0000-0000-0000-000000000005', 'hormones'),
    ('55555555-0000-0000-0000-000000000005', 'gut-health'),
    ('55555555-0000-0000-0000-000000000005', 'pms'),
    ('55555555-0000-0000-0000-000000000005', 'energy');

-- ─── SEED: PROGRAM DURATIONS ──────────────────────────────────────────────────
-- Program 1: 6 weeks
INSERT INTO program_durations (id, program_id, label, weeks, sort_order) VALUES
    ('66666666-0000-0000-0000-000000000001', '55555555-0000-0000-0000-000000000001', '6 weeks', 6, 1);

-- Program 2: 4 weeks
INSERT INTO program_durations (id, program_id, label, weeks, sort_order) VALUES
    ('66666666-0000-0000-0000-000000000002', '55555555-0000-0000-0000-000000000002', '4 weeks', 4, 1);

-- Program 3: 8 weeks
INSERT INTO program_durations (id, program_id, label, weeks, sort_order) VALUES
    ('66666666-0000-0000-0000-000000000003', '55555555-0000-0000-0000-000000000003', '8 weeks', 8, 1);

-- Program 4: 4 weeks
INSERT INTO program_durations (id, program_id, label, weeks, sort_order) VALUES
    ('66666666-0000-0000-0000-000000000004', '55555555-0000-0000-0000-000000000004', '4 weeks', 4, 1);

-- Program 5: 8 weeks
INSERT INTO program_durations (id, program_id, label, weeks, sort_order) VALUES
    ('66666666-0000-0000-0000-000000000005', '55555555-0000-0000-0000-000000000005', '8 weeks', 8, 1);

-- ─── SEED: DURATION PRICES ────────────────────────────────────────────────────
-- Program 1 (6 weeks): $400 USD, £320 GBP, ₹33000 INR
INSERT INTO duration_prices (duration_id, location_code, amount, currency_code, currency_symbol) VALUES
    ('66666666-0000-0000-0000-000000000001', 'US', 400.00,   'USD', '$'),
    ('66666666-0000-0000-0000-000000000001', 'GB', 320.00,   'GBP', '£'),
    ('66666666-0000-0000-0000-000000000001', 'IN', 33000.00, 'INR', '₹');

-- Program 2 (4 weeks): $350 USD, £280 GBP, ₹29000 INR
INSERT INTO duration_prices (duration_id, location_code, amount, currency_code, currency_symbol) VALUES
    ('66666666-0000-0000-0000-000000000002', 'US', 350.00,   'USD', '$'),
    ('66666666-0000-0000-0000-000000000002', 'GB', 280.00,   'GBP', '£'),
    ('66666666-0000-0000-0000-000000000002', 'IN', 29000.00, 'INR', '₹');

-- Program 3 (8 weeks): £879 GBP, $1099 USD, ₹90000 INR
INSERT INTO duration_prices (duration_id, location_code, amount, currency_code, currency_symbol) VALUES
    ('66666666-0000-0000-0000-000000000003', 'GB', 879.00,   'GBP', '£'),
    ('66666666-0000-0000-0000-000000000003', 'US', 1099.00,  'USD', '$'),
    ('66666666-0000-0000-0000-000000000003', 'IN', 90000.00, 'INR', '₹');

-- Program 4 (4 weeks): £499 GBP, $625 USD, ₹51000 INR
INSERT INTO duration_prices (duration_id, location_code, amount, currency_code, currency_symbol) VALUES
    ('66666666-0000-0000-0000-000000000004', 'GB', 499.00,   'GBP', '£'),
    ('66666666-0000-0000-0000-000000000004', 'US', 625.00,   'USD', '$'),
    ('66666666-0000-0000-0000-000000000004', 'IN', 51000.00, 'INR', '₹');

-- Program 5 (8 weeks): £899 GBP, $1125 USD, ₹92000 INR
INSERT INTO duration_prices (duration_id, location_code, amount, currency_code, currency_symbol) VALUES
    ('66666666-0000-0000-0000-000000000005', 'GB', 899.00,   'GBP', '£'),
    ('66666666-0000-0000-0000-000000000005', 'US', 1125.00,  'USD', '$'),
    ('66666666-0000-0000-0000-000000000005', 'IN', 92000.00, 'INR', '₹');

-- ─── SEED: SAMPLE TESTIMONIALS ────────────────────────────────────────────────
INSERT INTO program_testimonials (program_id, reviewer_name, reviewer_title, review_text, rating, sort_order) VALUES
    ('55555555-0000-0000-0000-000000000001',
     'Riya S.',
     'Marketing Professional, Mumbai',
     'After years of irregular cycles and unexplained fatigue, Dr. Prathima''s program completely changed how I understand my own body. The Ayurvedic approach felt deeply personal and actually worked.',
     5, 1),
    ('55555555-0000-0000-0000-000000000003',
     'Meera T.',
     'Software Engineer, Bangalore',
     'The PCOS Reset program was the first time someone treated my PCOS as a metabolic condition and not just a hormonal one. Six months on and my cycles are regular for the first time in three years.',
     5, 1);

-- ============================================================
-- VERIFY: Quick row counts to confirm seeding
-- ============================================================
SELECT 'roles'                    AS table_name, COUNT(*) AS rows FROM roles
UNION ALL SELECT 'guided_domains',              COUNT(*) FROM guided_domains
UNION ALL SELECT 'guided_categories',           COUNT(*) FROM guided_categories
UNION ALL SELECT 'category_whats_included',     COUNT(*) FROM category_whats_included
UNION ALL SELECT 'category_key_areas',          COUNT(*) FROM category_key_areas
UNION ALL SELECT 'users (experts)',             COUNT(*) FROM users
UNION ALL SELECT 'experts',                    COUNT(*) FROM experts
UNION ALL SELECT 'programs',                   COUNT(*) FROM programs
UNION ALL SELECT 'program_durations',          COUNT(*) FROM program_durations
UNION ALL SELECT 'duration_prices',            COUNT(*) FROM duration_prices
UNION ALL SELECT 'program_tags',              COUNT(*) FROM program_tags
UNION ALL SELECT 'program_testimonials',      COUNT(*) FROM program_testimonials;
