# Claude Code Prompt — Wellness Library Domain Implementation

> **How to use in Claude Code:**
> 1. Place this file + `wellness_platform.html` + `Wellness_Library_User_Flow.docx` in your project root or a `/docs` folder
> 2. In Claude Code run: `Read WELLNESS_LIBRARY_PROMPT.md, then read wellness_platform.html and Wellness_Library_User_Flow.docx. Then read the existing CLAUDE.md. Enter plan mode — ask me all clarifying questions before implementing anything.`
> 3. Let Claude Code ask questions, answer them, then approve the plan before any code is written.

---

## START OF PROMPT

I need you to plan and implement a new domain module called **"Wellness Library"** (recorded video content, Udemy/Masterclass-style) for the FemVed platform. This is the second major domain after "Guided Programs" which is already fully built.

**CRITICAL: Before implementing anything:**
1. Read the existing `CLAUDE.md` in the project root to understand the full tech stack, architecture, conventions, and existing schema.
2. Read this entire prompt.
3. Read the attached `wellness_platform.html` — this is the complete working HTML/CSS/JS template showing exactly how the UI should look and behave.
4. Read the attached `Wellness_Library_User_Flow.docx` — this describes the exact user journey from browsing → viewing → purchasing → post-purchase access.
5. Enter plan mode and ask me ALL your clarifying questions before writing a single line of code. I want to review and approve the full plan before any implementation begins.

---

## 1. WHAT THIS MODULE IS

Wellness Library is a recorded video content marketplace (like Udemy/Masterclass). Users browse a catalog of wellness videos organized by categories, watch YouTube trailers for free, purchase individual videos (one-time payment), and get lifetime access to stream the full content.

### Video Hosting Strategy:
- **All videos (trailers, full content, individual episodes) are uploaded to YouTube as UNLISTED videos.**
- The backend only stores the YouTube **embed URL** for each video — specifically just the `src` value from the YouTube embed iframe. Example: `https://www.youtube.com/embed/ZhnA6836TN8`
- **NOT** the full iframe tag, **NOT** a regular youtube.com/watch URL — only the embed URL.
- The content team (Aditi) will upload videos to YouTube, right-click → copy embed code → extract the `src` field value → enter it into the admin panel.
- The architecture must support swapping YouTube for other video providers in the future (Cloudflare Stream, Mux, Vimeo OTT) without rewriting business logic. Use an `IVideoStreamProvider` interface pattern.

### Key Differences from Guided Programs:
- **No durations** — a video is a single purchasable unit (no 4/6/8 week variants)
- **Price tiers** — videos are assigned a pricing tier (e.g., movie/standard/medium/large), each tier has fixed prices per region (IN/GB/US). Admins can optionally override price for a specific video.
- **Trailers** — every video has a public YouTube embed URL shown to ALL visitors (no login needed)
- **Stream URLs** — the actual video embed URL, only returned to users who have purchased. The API must verify ownership server-side before returning any stream URL.
- **Episodes** — some videos are "Series" type with multiple episodes (each episode has its own YouTube embed URL, title, duration, description). Others are "Masterclass" type (single video).
- **Featured row** — admin-curated featured section at the top of the catalog (3 highlighted cards in a grid)
- **Video type badge** — "Series" or "Masterclass" badge on each card
- **Same payment flow** — CashFree for India, PayPal for UK/US, same webhook handling, same coupon system
- **Same admin patterns** — CRUD, status lifecycle (DRAFT → PENDING_REVIEW → PUBLISHED → ARCHIVED), audit logging

---

## 2. USER FLOW (from the Wellness_Library_User_Flow.docx)

### Browsing the Library (PUBLIC — no account required):
- User lands on the Wellness Library page
- Full program catalog displayed in a card grid
- Each card shows: cover image, title, expert name, type badge (Masterclass/Series), duration or episode count, and price
- Filter tabs at top allow filtering by `programType` — these filter categories are DYNAMIC (stored in DB, managed by admin, content team will provide the list)
- Filtering updates the grid immediately and collapses the featured row
- Featured row at top shows 3 curated highlights (only visible when "All" filter is active)

### Viewing a Program (PUBLIC — no account required):
- Clicking any card opens the detail page
- Detail page hero shows: full title, expert credentials, descriptive tags, rich description
- Trailer player is available — YouTube embed, viewable by anyone without purchase
- For Series: episode list shown with title, description, duration — ALL episodes appear LOCKED (🔒) before purchase
- Purchase card on the right shows: price, one-time payment notice, included features (lifetime access, all devices, downloadable resources, certificate of completion)
- Back button always available to return to the library grid

### Purchasing a Program (REQUIRES AUTH):
- User clicks "Purchase" button (available in hero section AND in purchase card)
- **If NOT logged in → prompt to sign in or create account FIRST, then return to purchase flow**
- Once authenticated → same payment flow as guided programs (CashFree for IN, PayPal for UK/US)
- After successful payment → confirmation shown, video added to user's personal library in dashboard
- For Series → episode list unlocks, user can begin watching any episode

### Post-Purchase Access:
- User returns to purchased content anytime via their dashboard library section
- All content accessible permanently — no time restriction, tied to account
- Masterclass: full video playable
- Series: all individual episodes playable, user can pick any episode
- Stream URLs (YouTube embed) only revealed after purchase verification

---

## 3. DATABASE DESIGN

### Approach: New dedicated tables for Wellness Library content, reuse shared infrastructure

The Guided Programs domain and Wellness Library domain are fundamentally different data shapes (think Books vs Movies — same store, different shelves). Using a shared `programs` table with discriminator columns would create a "God table" anti-pattern with nullable fields everywhere. Separate tables keep each domain clean and independently evolvable.

### Tables to REUSE as-is (no changes):
`roles`, `users`, `refresh_tokens`, `password_reset_tokens`, `experts`, `refunds`, `notification_log`, `admin_audit_log`, `gdpr_deletion_requests`

**Experts table is shared** — the same expert can teach guided programs AND create library content.

### Tables to MODIFY:

**`orders` table — add columns:**
```sql
ALTER TABLE orders ADD COLUMN library_video_id UUID NULL REFERENCES library_videos(id);
ALTER TABLE orders ADD COLUMN order_source VARCHAR(20) NOT NULL DEFAULT 'GUIDED';
-- order_source values: 'GUIDED' | 'LIBRARY'
-- Constraint: exactly one of (duration_price_id, library_video_id) must be NOT NULL
ALTER TABLE orders ADD CONSTRAINT chk_order_source CHECK (
  (order_source = 'GUIDED' AND duration_price_id IS NOT NULL AND library_video_id IS NULL)
  OR (order_source = 'LIBRARY' AND library_video_id IS NOT NULL AND duration_price_id IS NULL)
);
```

**`coupons` table — add scope column:**
```sql
ALTER TABLE coupons ADD COLUMN scope VARCHAR(20) NOT NULL DEFAULT 'ALL';
-- values: 'ALL' | 'GUIDED' | 'LIBRARY'
```

### NEW Tables:

**`library_domain`** — top-level domain container (like `guided_domains`)
```
id                    UUID PK DEFAULT gen_random_uuid()
name                  VARCHAR(200) NOT NULL        -- "Wellness Library"
slug                  VARCHAR(200) NOT NULL UNIQUE  -- "wellness-library"
description           TEXT
hero_image_desktop    TEXT           -- hero banner image for desktop
hero_image_mobile     TEXT           -- hero banner image for mobile
hero_image_portrait   TEXT           -- hero banner image portrait
sort_order            INT NOT NULL DEFAULT 0
is_active             BOOLEAN NOT NULL DEFAULT TRUE
created_at            TIMESTAMPTZ NOT NULL DEFAULT NOW()
updated_at            TIMESTAMPTZ NOT NULL DEFAULT NOW()
```

**`library_categories`** — content categories (Hormonal Health Support, Mental & Spiritual Wellbeing, etc.)
```
id              UUID PK DEFAULT gen_random_uuid()
domain_id       UUID NOT NULL FK → library_domain(id)
name            VARCHAR(300) NOT NULL
slug            VARCHAR(300) NOT NULL UNIQUE
description     TEXT
card_image      TEXT
sort_order      INT NOT NULL DEFAULT 0
is_active       BOOLEAN NOT NULL DEFAULT TRUE
created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
```

**`library_filter_types`** — dynamic filter tabs (programType filters that Aditi/content team will define)
```
id              UUID PK DEFAULT gen_random_uuid()
domain_id       UUID NOT NULL FK → library_domain(id)
name            VARCHAR(100) NOT NULL    -- "Masterclasses", "Series", "Mindfulness", "Nutrition", etc.
filter_key      VARCHAR(100) NOT NULL    -- "masterclass", "series", "mindfulness" (used in query param)
filter_target   VARCHAR(20) NOT NULL     -- 'VIDEO_TYPE' or 'CATEGORY' (what this filter matches against)
sort_order      INT NOT NULL DEFAULT 0
is_active       BOOLEAN NOT NULL DEFAULT TRUE
created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
```
Note: filter_target tells the frontend/API whether this filter matches against video_type (masterclass/series) or against a category slug. This makes the filter tabs fully dynamic — the content team adds filters via admin without code changes.

**`library_price_tiers`** — defines pricing tiers
```
id              UUID PK DEFAULT gen_random_uuid()
tier_key        VARCHAR(20) NOT NULL UNIQUE  -- 'MOVIE', 'STANDARD', 'MEDIUM', 'LARGE'
display_name    VARCHAR(100) NOT NULL
sort_order      INT NOT NULL DEFAULT 0
is_active       BOOLEAN NOT NULL DEFAULT TRUE
created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
```

**`library_tier_prices`** — amount per tier per region
```
id              UUID PK DEFAULT gen_random_uuid()
tier_id         UUID NOT NULL FK → library_price_tiers(id)
location_code   VARCHAR(5) NOT NULL   -- 'IN', 'GB', 'US'
amount          DECIMAL(12,2) NOT NULL
currency_code   VARCHAR(3) NOT NULL   -- 'INR', 'GBP', 'USD'
currency_symbol VARCHAR(5) NOT NULL   -- '₹', '£', '$'
created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
UNIQUE(tier_id, location_code)
```

**`library_videos`** — the core content unit (each purchasable video/series)
```
id                      UUID PK DEFAULT gen_random_uuid()
category_id             UUID NOT NULL FK → library_categories(id)
expert_id               UUID NOT NULL FK → experts(id)
price_tier_id           UUID NOT NULL FK → library_price_tiers(id)
title                   VARCHAR(500) NOT NULL
slug                    VARCHAR(500) NOT NULL UNIQUE
synopsis                TEXT                     -- short blurb for grid card
description             TEXT                     -- long rich text for detail page (supports HTML)
card_image              TEXT                     -- grid card cover image URL
hero_image              TEXT                     -- detail page hero image URL
icon_emoji              VARCHAR(10)              -- "🧘" for gradient card fallback when no image
gradient_class          VARCHAR(50)              -- "grad-1" for CSS gradient fallback
trailer_url             TEXT                     -- YouTube EMBED URL (public, e.g. https://www.youtube.com/embed/xxx)
stream_url              TEXT                     -- YouTube EMBED URL for full video (gated behind purchase)
video_type              VARCHAR(20) NOT NULL     -- 'MASTERCLASS' | 'SERIES'
total_duration          VARCHAR(50)              -- "4h 10m" display string
total_duration_seconds  INT                      -- for sorting/filtering
release_year            VARCHAR(4)               -- "2026"
is_featured             BOOLEAN NOT NULL DEFAULT FALSE
featured_label          VARCHAR(200)             -- "Editor's Choice · Series"
featured_position       INT                      -- 1,2,3 for featured row ordering
status                  VARCHAR(20) NOT NULL DEFAULT 'DRAFT'
  -- DRAFT → PENDING_REVIEW → PUBLISHED → ARCHIVED
sort_order              INT NOT NULL DEFAULT 0
is_deleted              BOOLEAN NOT NULL DEFAULT FALSE
created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
```

**`library_video_prices`** — optional per-video price override (falls back to tier if not set)
```
id              UUID PK DEFAULT gen_random_uuid()
video_id        UUID NOT NULL FK → library_videos(id)
location_code   VARCHAR(5) NOT NULL
amount          DECIMAL(12,2) NOT NULL
currency_code   VARCHAR(3) NOT NULL
currency_symbol VARCHAR(5) NOT NULL
original_amount DECIMAL(12,2)         -- for struck-through "was ₹X" display
created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
UNIQUE(video_id, location_code)
```

**`library_video_episodes`** — for Series-type content (each episode has its own YouTube embed URL)
```
id                UUID PK DEFAULT gen_random_uuid()
video_id          UUID NOT NULL FK → library_videos(id)
episode_number    INT NOT NULL
title             VARCHAR(500) NOT NULL
description       TEXT
duration          VARCHAR(50)          -- "18 min" display string
duration_seconds  INT
stream_url        TEXT                 -- YouTube EMBED URL for this episode (gated)
thumbnail_url     TEXT
is_free_preview   BOOLEAN NOT NULL DEFAULT FALSE  -- admin can unlock specific episodes as teasers
sort_order        INT NOT NULL DEFAULT 0
created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
updated_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
UNIQUE(video_id, episode_number)
```

**`library_video_tags`** — tags displayed on detail page + used for search
```
id              UUID PK DEFAULT gen_random_uuid()
video_id        UUID NOT NULL FK → library_videos(id)
tag             VARCHAR(100) NOT NULL
sort_order      INT NOT NULL DEFAULT 0
```

**`library_video_features`** — "what's included" bullets on purchase card
```
id              UUID PK DEFAULT gen_random_uuid()
video_id        UUID NOT NULL FK → library_videos(id)
icon            VARCHAR(10) NOT NULL     -- "▶", "♾", "📱", "📄", "✦"
description     VARCHAR(500) NOT NULL    -- "Lifetime access, watch anytime"
sort_order      INT NOT NULL DEFAULT 0
```

**`library_video_resources`** — downloadable files gated behind purchase (workbooks, PDFs)
```
id              UUID PK DEFAULT gen_random_uuid()
video_id        UUID NOT NULL FK → library_videos(id)
file_name       VARCHAR(300) NOT NULL
file_url        TEXT NOT NULL             -- Cloudflare R2 URL
file_type       VARCHAR(50)              -- "pdf", "xlsx"
file_size_bytes BIGINT
sort_order      INT NOT NULL DEFAULT 0
created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
```

**`user_library_access`** — post-purchase access record
```
id                      UUID PK DEFAULT gen_random_uuid()
user_id                 UUID NOT NULL FK → users(id)
video_id                UUID NOT NULL FK → library_videos(id)
order_id                UUID NOT NULL FK → orders(id)
purchased_at            TIMESTAMPTZ NOT NULL DEFAULT NOW()
expires_at              TIMESTAMPTZ          -- NULL = lifetime (default for now)
is_active               BOOLEAN NOT NULL DEFAULT TRUE
last_watched_at         TIMESTAMPTZ
watch_progress_seconds  INT NOT NULL DEFAULT 0
created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
UNIQUE(user_id, video_id)
```

**`user_episode_progress`** — per-episode watch tracking for Series
```
id                      UUID PK DEFAULT gen_random_uuid()
user_id                 UUID NOT NULL FK → users(id)
episode_id              UUID NOT NULL FK → library_video_episodes(id)
watch_progress_seconds  INT NOT NULL DEFAULT 0
is_completed            BOOLEAN NOT NULL DEFAULT FALSE
last_watched_at         TIMESTAMPTZ
UNIQUE(user_id, episode_id)
```

---

## 4. API ARCHITECTURE

Follow the EXACT same patterns as the existing Guided module: MediatR CQRS, FluentValidation, same error handling middleware, same response format, same auth policies, same repository + UnitOfWork pattern.

### New Endpoints:

**Public Catalog (cached, same pattern as guided/tree):**
```
GET  /api/v1/library/tree                  -- full catalog: domain → categories → videos with pricing + filter types
GET  /api/v1/library/categories/{slug}     -- single category with its videos
GET  /api/v1/library/videos/{slug}         -- video detail (trailer always included, stream URL NEVER here)
GET  /api/v1/library/filters               -- available filter tabs (from library_filter_types)
```

**Authenticated User:**
```
GET  /api/v1/library/videos/{slug}/stream    -- returns stream URL(s) ONLY if user has purchased
                                              -- for Series: returns all episode stream URLs
                                              -- for Masterclass: returns single stream URL
GET  /api/v1/users/me/library                -- purchased videos with watch progress
PUT  /api/v1/users/me/library/{videoId}/progress  -- update watch progress
                                                   -- body: { progressSeconds, episodeId? }
```

**Purchase (extend existing):**
```
POST /api/v1/orders/initiate
-- Current body: { durationId, couponCode?, idempotencyKey }
-- NEW alternative: { videoId, couponCode?, idempotencyKey }
-- When videoId present (instead of durationId) → library purchase flow
-- Price resolved: video-level override ?? tier-level default for user's region
-- Validates user doesn't already own this video
```

**Admin + Expert:**
```
POST   /api/v1/library/domains                          -- create domain [AdminOnly]
PUT    /api/v1/library/domains/{id}                     -- update domain [AdminOnly]
POST   /api/v1/library/categories                       -- create category [AdminOnly]
PUT    /api/v1/library/categories/{id}                  -- update category [AdminOnly]
POST   /api/v1/library/filters                          -- create filter type [AdminOnly]
PUT    /api/v1/library/filters/{id}                     -- update filter type [AdminOnly]
DELETE /api/v1/library/filters/{id}                     -- remove filter type [AdminOnly]
POST   /api/v1/library/videos                           -- create video [ExpertOrAdmin]
PUT    /api/v1/library/videos/{id}                      -- update video [ExpertOrAdmin]
POST   /api/v1/library/videos/{id}/submit               -- DRAFT → PENDING_REVIEW [ExpertOrAdmin]
POST   /api/v1/library/videos/{id}/publish              -- PENDING_REVIEW → PUBLISHED [AdminOnly]
POST   /api/v1/library/videos/{id}/archive              -- PUBLISHED → ARCHIVED [AdminOnly]
POST   /api/v1/library/videos/{id}/episodes             -- add episode [ExpertOrAdmin]
PUT    /api/v1/library/videos/{id}/episodes/{epId}      -- update episode [ExpertOrAdmin]
DELETE /api/v1/library/videos/{id}/episodes/{epId}      -- remove episode [AdminOnly]
POST   /api/v1/library/videos/{id}/tags                 -- set tags (replace all) [ExpertOrAdmin]
POST   /api/v1/library/videos/{id}/features             -- set purchase card features [ExpertOrAdmin]
POST   /api/v1/library/videos/{id}/resources            -- add downloadable resource [ExpertOrAdmin]
DELETE /api/v1/library/videos/{id}/resources/{resId}    -- remove resource [AdminOnly]
POST   /api/v1/library/price-tiers                      -- create/update tier [AdminOnly]
PUT    /api/v1/library/price-tiers/{id}/prices          -- set tier prices per region [AdminOnly]
PUT    /api/v1/library/videos/{id}/price-override       -- set per-video price override [AdminOnly]
DELETE /api/v1/library/videos/{id}/price-override       -- remove override, revert to tier [AdminOnly]
GET    /api/v1/admin/library/analytics                  -- purchase/revenue stats by video [AdminOnly]
GET    /api/v1/admin/library/purchases                  -- which user bought which video [AdminOnly]
```

### Solution Structure (new folders, same layered pattern as Guided):

```
src/FemVed.Application/Library/
├── Commands/
│   ├── CreateLibraryDomain/
│   ├── UpdateLibraryDomain/
│   ├── CreateLibraryCategory/
│   ├── UpdateLibraryCategory/
│   ├── ManageFilterTypes/
│   ├── CreateVideo/
│   ├── UpdateVideo/
│   ├── SubmitVideo/
│   ├── PublishVideo/
│   ├── ArchiveVideo/
│   ├── ManageEpisodes/
│   ├── SetVideoTags/
│   ├── SetVideoFeatures/
│   ├── ManageVideoResources/
│   ├── ManagePriceTiers/
│   ├── SetVideoPrice/
│   └── UpdateWatchProgress/
├── Queries/
│   ├── GetLibraryTree/
│   ├── GetLibraryCategoryBySlug/
│   ├── GetVideoBySlug/
│   ├── GetVideoStreamUrl/
│   ├── GetFilterTypes/
│   ├── GetMyLibrary/
│   ├── GetLibraryAnalytics/
│   └── GetLibraryPurchases/
└── DTOs/
    ├── LibraryTreeResponse.cs
    ├── LibraryDomainDto.cs
    ├── LibraryCategoryDto.cs
    ├── LibraryVideoDto.cs
    ├── LibraryVideoDetailDto.cs
    ├── LibraryEpisodeDto.cs
    ├── LibraryStreamResponse.cs
    ├── LibraryFilterDto.cs
    └── LibraryAnalyticsDto.cs

src/FemVed.Domain/Entities/
├── LibraryDomain.cs
├── LibraryCategory.cs
├── LibraryFilterType.cs
├── LibraryVideo.cs
├── LibraryVideoEpisode.cs
├── LibraryVideoTag.cs
├── LibraryVideoFeature.cs
├── LibraryVideoResource.cs
├── LibraryPriceTier.cs
├── LibraryTierPrice.cs
├── LibraryVideoPrice.cs
├── UserLibraryAccess.cs
└── UserEpisodeProgress.cs

src/FemVed.Domain/Enums/
├── VideoType.cs          -- MASTERCLASS, SERIES
├── VideoStatus.cs        -- DRAFT, PENDING_REVIEW, PUBLISHED, ARCHIVED
├── PriceTierKey.cs       -- MOVIE, STANDARD, MEDIUM, LARGE
├── OrderSource.cs        -- GUIDED, LIBRARY
└── FilterTarget.cs       -- VIDEO_TYPE, CATEGORY

src/FemVed.Infrastructure/Library/
└── LibraryCatalogReadService.cs  -- complex EF Core projections (same pattern as GuidedCatalogReadService)

src/FemVed.API/Controllers/
├── LibraryCatalogController.cs   -- public catalog endpoints
├── LibraryAdminController.cs     -- admin CRUD endpoints
└── (modify) OrdersController.cs  -- extend InitiateOrder to support videoId
└── (modify) UsersController.cs   -- add /users/me/library endpoints
```

### Library Tree API — JSON Contract (React frontend binds to this shape exactly):

```json
{
  "domain": {
    "domainId": "uuid",
    "domainName": "Wellness Library",
    "heroImageDesktop": "url",
    "heroImageMobile": "url",
    "heroImagePortrait": "url"
  },
  "filters": [
    { "filterId": "uuid", "name": "All Programs", "filterKey": "all", "filterTarget": "ALL" },
    { "filterId": "uuid", "name": "Masterclasses", "filterKey": "masterclass", "filterTarget": "VIDEO_TYPE" },
    { "filterId": "uuid", "name": "Series", "filterKey": "series", "filterTarget": "VIDEO_TYPE" },
    { "filterId": "uuid", "name": "Mindfulness", "filterKey": "mindfulness", "filterTarget": "CATEGORY" }
  ],
  "featuredVideos": [
    {
      "videoId": "uuid",
      "position": 1,
      "eyebrowText": "Editor's Choice · Series",
      "title": "...",
      "expertName": "...",
      "totalDuration": "4h 10m",
      "episodeCount": 30,
      "price": "₹2,199",
      "cardImage": "url",
      "iconEmoji": "🧘",
      "gradientClass": "grad-1",
      "videoType": "SERIES"
    }
  ],
  "categories": [
    {
      "categoryId": "uuid",
      "categoryName": "Hormonal Health Support",
      "categorySlug": "hormonal-health-support",
      "videos": [
        {
          "videoId": "uuid",
          "title": "Cycle Reset Method",
          "slug": "cycle-reset-method",
          "synopsis": "A practical reset framework...",
          "cardImage": "url",
          "iconEmoji": "🧘",
          "gradientClass": "grad-1",
          "videoType": "MASTERCLASS",
          "totalDuration": "4h 10m",
          "episodeCount": null,
          "releaseYear": "2026",
          "price": "₹2,199",
          "originalPrice": null,
          "expertName": "Dr. Ananya Mehta",
          "expertTitle": "Ayurvedic Physician",
          "tags": ["Hormones", "Cycle", "Nutrition"]
        }
      ]
    }
  ]
}
```

### Video Detail API — JSON Contract:
```json
{
  "videoId": "uuid",
  "title": "Cycle Reset Method",
  "slug": "cycle-reset-method",
  "synopsis": "...",
  "description": "Long rich text with <strong>HTML</strong> support...",
  "cardImage": "url",
  "heroImage": "url",
  "iconEmoji": "🧘",
  "gradientClass": "grad-1",
  "trailerUrl": "https://www.youtube.com/embed/ZhnA6836TN8",
  "videoType": "SERIES",
  "totalDuration": "4h 10m",
  "releaseYear": "2026",
  "price": "₹2,199",
  "originalPrice": null,
  "priceTier": "LARGE",
  "expertId": "uuid",
  "expertName": "Dr. Ananya Mehta",
  "expertTitle": "Ayurvedic Physician & Women's Health Specialist",
  "expertGridDescription": "...",
  "tags": ["Hormones", "Cycle"],
  "episodes": [
    {
      "episodeId": "uuid",
      "episodeNumber": 1,
      "title": "The Ground of Awareness",
      "description": "What mindfulness really is...",
      "duration": "18 min",
      "isFreePreview": false,
      "isLocked": true
    }
  ],
  "features": [
    { "icon": "▶", "description": "30 episodes, lifetime access" },
    { "icon": "♾", "description": "Lifetime access, watch anytime" },
    { "icon": "📱", "description": "Available on all devices" },
    { "icon": "📄", "description": "Downloadable workbooks & resources" },
    { "icon": "✦", "description": "Certificate of completion" }
  ],
  "resources": [
    { "resourceId": "uuid", "fileName": "Workbook.pdf", "fileType": "pdf" }
  ],
  "isPurchased": false
}
```

### Stream URL Response (only for authenticated + purchased users):
```json
{
  "videoId": "uuid",
  "videoType": "SERIES",
  "streamUrl": "https://www.youtube.com/embed/xxx",
  "episodes": [
    {
      "episodeId": "uuid",
      "episodeNumber": 1,
      "title": "The Ground of Awareness",
      "streamUrl": "https://www.youtube.com/embed/yyy",
      "watchProgressSeconds": 342,
      "isCompleted": false
    }
  ],
  "resources": [
    { "resourceId": "uuid", "fileName": "Workbook.pdf", "fileUrl": "https://r2.../workbook.pdf" }
  ],
  "overallProgressSeconds": 1200,
  "lastWatchedAt": "2026-03-15T10:30:00Z"
}
```

**CRITICAL: The `streamUrl` fields and `fileUrl` for resources are NEVER included in the public detail endpoint. They are ONLY returned by the `/stream` endpoint after verifying the user has a valid `user_library_access` record.**

### Price Resolution Logic:
```
1. Check library_video_prices for (video_id, location_code) → if exists, use it (per-video override)
2. Else, get video.price_tier_id → lookup library_tier_prices for (tier_id, location_code) (tier default)
3. Location detection same as guided: auth user country_iso_code → Accept-Language header → default "GB"
4. Format: symbol + amount string, e.g. "£24", "$29", "₹1,299"
```

---

## 5. MODIFY EXISTING ORDER FLOW

The `InitiateOrderCommand` currently expects `{ durationId, couponCode?, idempotencyKey }`.

Extend it to ALSO accept `{ videoId, couponCode?, idempotencyKey }`:
1. Exactly one of `durationId` or `videoId` must be provided (validate this)
2. If `videoId`: resolve price from video override or tier default, validate user doesn't already own this video (check `user_library_access`), set `order_source = 'LIBRARY'`, set `library_video_id` on the order
3. If `durationId`: existing guided flow, set `order_source = 'GUIDED'`
4. Coupon validation: check coupon scope matches order source (scope 'ALL' works for both)
5. Rest of flow (gateway selection by country, order creation) remains the same

The `OrderPaidEventHandler` must branch on `order.OrderSource`:
- If `LIBRARY`: create `UserLibraryAccess` record (NOT `UserProgramAccess`)
- Send same notification templates (purchase_success to user, expert_new_enrollment to expert)
- Template data adjusted for library content (video title instead of program name, etc.)
- Log to `notification_log` as before

---

## 6. SEED DATA NOTE

I am attaching a `wellnessLibrary.json` file that has preliminary seed data with 4 categories and 20 videos. However, **this JSON may be outdated — the final content will come from a Google Sheet prepared by the content team (Aditi).** Use the JSON structure as a reference for the data shape and relationships, but:
- Do NOT hardcode the JSON data into the migration
- Create a proper seed migration that demonstrates the pattern (maybe 1-2 sample videos)
- The real data will be entered through the admin panel once built
- The admin panel is the primary data entry method — the content team will use it to add all videos, episodes, trailer URLs, stream URLs, images, etc.

---

## 7. REACT FRONTEND PLAN

Reference the attached `wellness_platform.html` for the EXACT UI design. The React implementation should match this template's visual design precisely.

### New Routes:
```
/library                              -- catalog grid page
/library/:categorySlug/:videoSlug     -- video detail page
/dashboard/library                    -- user's purchased videos
/admin/library                        -- admin management
/admin/library/videos/new             -- create new video
/admin/library/videos/:id/edit        -- edit video
/admin/library/analytics              -- purchase analytics
```

### Key Components:
```
LibraryCatalogPage     -- filter tabs + featured row + card grid (from HTML template)
LibraryCard            -- individual video card (3:4 aspect, cover image with gradient fallback)
LibraryDetailPage      -- detail hero + about + episodes + trailer + purchase card + instructor
TrailerEmbed           -- YouTube iframe wrapper using embed URL
VideoPlayer            -- YouTube iframe for purchased content (swappable via interface later)
EpisodeList            -- locked/unlocked episode list with progress indicators
PurchaseCard           -- sidebar purchase widget with price + features
InstructorStrip        -- expert profile card
MyLibraryPage          -- dashboard section showing purchased content with progress
AdminLibraryPage       -- CRUD management for all library content
```

### Frontend Behavior Notes:
- Filter tabs: "All Programs" shows featured row + full grid. Any other filter hides featured row, shows filtered grid.
- Cards use cover image if available, fall back to gradient + emoji icon (see HTML template's gradient classes)
- Detail page: trailer embed uses YouTube embed URL directly in an iframe
- Episode list: shows 🔒 lock icon on all episodes before purchase. After purchase, lock icons replaced with play buttons + progress bars.
- Purchase button: if user not authenticated, redirect to login/register with return URL back to the video detail page
- Stream endpoint: called only after purchase verified, loads actual YouTube embed URLs for player

---

## 8. FEATURES TO CONSIDER (ask me about these)

1. **Watch progress / resume playback** — included in schema. For Series, per-episode progress tracking.
2. **Free preview episodes** — `is_free_preview` on episodes table. Admin can unlock 1-2 episodes as teasers.
3. **Downloadable resources** — gated behind purchase, stored on Cloudflare R2.
4. **Certificate of completion** — mentioned in the template's purchase features. Would need a `user_library_completions` table and threshold logic.
5. **Video reviews/ratings** — similar to existing `program_testimonials`.
6. **Wishlisting** — save videos before purchasing.
7. **Bundle pricing** — "Buy 3 from this category for X% off."
8. **Video storage abstraction** — `IVideoStreamProvider` interface for future cloud migration away from YouTube.
9. **SEO metadata** — `meta_title`, `meta_description` on videos/categories.
10. **Admin analytics** — top-selling videos, revenue by category/tier, conversion rates, watch completion rates.

---

## 9. IMPLEMENTATION PHASES — follow in strict order

**Phase L1:** Domain entities + EF Fluent API configurations + single migration (all new tables + orders/coupons modifications). No controllers yet. Show me the migration plan and entity list before writing code.

**Phase L2:** Library catalog read-only endpoints (tree, category by slug, video by slug, filters) + `ILibraryCatalogReadService` + minimal sample seed data. Verify with curl commands.

**Phase L3:** Purchase flow extension (modify `InitiateOrderCommand` to support `videoId`, modify `OrderPaidEventHandler` to create `UserLibraryAccess`). Test with existing payment infrastructure.

**Phase L4:** Authenticated user endpoints (stream URL access check, my library listing, watch progress tracking, episode progress).

**Phase L5:** Admin CRUD endpoints (full video lifecycle, episode management, tags, features, resources, tier management, filter types, price overrides, analytics queries).

**Phase L6:** Swagger annotations, curl test commands for every endpoint, integration verification.

---

## 10. QUESTIONS TO ASK ME BEFORE STARTING

Before you write any code, ask me about:

1. Should library categories be completely separate from guided_categories? (I'm leaning yes)
2. Do I want per-video price overrides from day one, or just tier-level pricing for MVP simplicity?
3. Should individual episodes each have their own YouTube embed URLs, or does the series have a single URL with timestamps/chapters?
4. Do I want watch progress tracking from day one, or defer to a later phase?
5. Should the featured row be managed via a boolean `is_featured` on the video + `featured_position`, or a separate admin endpoint?
6. For the `library/tree` response — should it return ALL published videos, or should there be pagination for when the catalog grows?
7. Do I want video reviews/ratings in this initial build?
8. Should experts from the Wellness Library be the same expert profiles as Guided Programs, or separate expert rows?
9. For the order modification — should `OrderSource` be a string column or a new enum mapped to the DB?
10. Migration naming convention — what name do I want for this migration?
11. Do I want the downloadable resources feature in this initial build, or defer?
12. Do I want the certificate of completion feature in this initial build, or defer?
13. The `library_filter_types` table — is this the right approach for dynamic filter tabs, or do you prefer deriving filters from existing data (video types + category slugs)?
14. Should the `description` field on videos support raw HTML (like the HTML template uses `<strong>` tags), or should we use Markdown?

Ask me any other questions you think are important based on the existing codebase patterns you see in CLAUDE.md and the source files.

---

## 11. RULES (SAME AS CLAUDE.md — follow these exactly)

1. **One phase at a time.** Never start the next phase until I type "Phase confirmed, proceed."
2. **Before writing any code** for a phase, list every file you will create or modify. Wait for my "go ahead."
3. **After each phase**, show me curl test commands for every new endpoint.
4. **Ask before deciding** on ambiguous choices (naming, pattern, exception type).
5. **Migration discipline** — tell me the migration name and exactly what it adds/changes, then wait for confirmation before running `dotnet ef migrations add`.
6. Follow existing patterns EXACTLY — same Repository<T>, same UnitOfWork, same MediatR pipeline behaviours, same FluentValidation, same ExceptionHandlingMiddleware, same ProblemDetails response format.
7. All entity configurations use Fluent API in separate configuration classes (no data annotations on entities).
8. All table/column names use snake_case (existing PostgreSQL convention).
9. XML doc comments on all public types and members.
10. Never hardcode values that should be environment variables.
11. Soft deletes only — never hard-delete videos, categories, or domains. Set `is_deleted = true`.
12. All admin mutations logged to `admin_audit_log` with before/after JSON snapshots.

## END OF PROMPT
