# CLAUDE.md — FemVed Backend API
# READ THIS FULLY AT THE START OF EVERY SESSION BEFORE TOUCHING ANY CODE.

---

## 1. WHAT THIS PROJECT IS

FemVed is an enterprise-grade women's wellness platform. This repo is the **.NET 10 backend API only**.
A separate React frontend (different repo, different engineer) consumes these APIs.

There are 3 modules — build in this order:
1. **Guided Programs** (Live 1:1 expert-led programs) ← We are building this first
2. Recorded Content (video library, Udemy-style) ← Phase 2 later
3. Events / Retreats ← Phase 3 later

---

## 2. TECH STACK

| Layer | Choice |
|---|---|
| Framework | .NET 10, ASP.NET Core Web API |
| Database | PostgreSQL (Railway hosted) |
| ORM | Entity Framework Core 9, code-first, Fluent API |
| Validation | FluentValidation |
| Mediator / CQRS | MediatR 12 |
| Auth | JWT Bearer tokens + Refresh tokens |
| Password hashing | BCrypt.Net-Next (work factor 12) |
| Email | SendGrid (dynamic templates) |
| WhatsApp + SMS | Twilio (WhatsApp Business API + SMS) |
| Payments — India | CashFree Payments API |
| Payments — UK/US | PayPal Orders API v2 |
| File storage | Cloudflare R2 (S3-compatible, use AWSSDK.S3) |
| Logging | Serilog + structured JSON output |
| API docs | Swagger / Swashbuckle |
| Rate limiting | ASP.NET Core built-in rate limiting middleware |
| Hosting | Railway (Linux container) |
| CI/CD | GitHub Actions |

---

## 3. SOLUTION STRUCTURE

```
FemVed.sln
├── src/
│   ├── FemVed.API/              ← ASP.NET Core Web API (thin layer only)
│   │   ├── Controllers/
│   │   ├── Middleware/          ← ExceptionHandling, CorrelationId, RequestLogging
│   │   ├── Filters/
│   │   ├── Extensions/          ← ServiceCollection registration extensions
│   │   └── appsettings.json
│   ├── FemVed.Application/      ← All business logic lives here
│   │   ├── Auth/
│   │   │   ├── Commands/        ← Register, Login, Refresh, Logout, ForgotPassword, ResetPassword
│   │   │   └── Queries/
│   │   ├── Guided/
│   │   │   ├── Commands/        ← CreateProgram, UpdateProgram, PublishProgram, etc.
│   │   │   └── Queries/         ← GetGuidedTree, GetCategoryBySlug, GetProgramBySlug
│   │   ├── Payments/
│   │   │   ├── Commands/        ← InitiateOrder, ProcessWebhook, InitiateRefund
│   │   │   └── Queries/         ← GetOrder, GetMyOrders
│   │   ├── Notifications/
│   │   │   └── Commands/        ← SendEmail, SendWhatsApp, SendSms
│   │   ├── Common/
│   │   │   └── Behaviours/      ← ValidationBehaviour, LoggingBehaviour, PerformanceBehaviour
│   │   ├── Interfaces/          ← IRepository<T>, IUnitOfWork, IPaymentGateway, IEmailService, etc.
│   │   └── DTOs/
│   ├── FemVed.Domain/           ← Pure domain — zero external dependencies
│   │   ├── Entities/            ← User, Expert, Program, Order, etc.
│   │   ├── Enums/               ← UserRole, OrderStatus, ProgramStatus, LocationCode, etc.
│   │   ├── Exceptions/          ← DomainException, NotFoundException, ValidationException
│   │   ├── Events/              ← OrderPaidEvent, ProgramPublishedEvent
│   │   └── ValueObjects/        ← Money
│   ├── FemVed.Infrastructure/   ← All external concerns — implements Application interfaces
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Migrations/
│   │   │   ├── Configurations/  ← EF Fluent API per entity
│   │   │   └── Repositories/
│   │   ├── Payment/             ← CashFreePaymentGateway, PayPalPaymentGateway, PaymentGatewayFactory
│   │   ├── Notifications/       ← SendGridEmailService, TwilioWhatsAppService, TwilioSmsService
│   │   └── Storage/             ← R2StorageService
└── tests/
    └── FemVed.Tests/
        ├── Unit/
        └── Integration/
```

---

## 4. ARCHITECTURE RULES — NEVER VIOLATE

- Controllers are THIN. They only: receive HTTP input → call MediatR → return response. No logic.
- All business logic in Application layer (Command/Query Handlers).
- Domain entities NEVER reference EF Core, MediatR, or any infrastructure concern.
- All interfaces defined in Application layer. All implementations in Infrastructure.
- FluentValidation validators always in a separate file next to their Command class.
- MediatR Pipeline Behaviours: ValidationBehaviour runs first, then LoggingBehaviour, then PerformanceBehaviour (warn if > 500ms).
- Every public method and every controller action must have XML doc comments.
- Every controller action must have `[ProducesResponseType]` Swagger annotations.

---

## 5. DESIGN PATTERNS IN USE

| Pattern | Where Used |
|---|---|
| Repository + Unit of Work | Infrastructure/Persistence |
| CQRS via MediatR | Application layer |
| Factory | PaymentGatewayFactory (selects CashFree or PayPal by country_iso_code) |
| Strategy | NotificationService (selects Email / WhatsApp / SMS at runtime) |
| Observer / Domain Events | Post-payment triggers (MediatR notifications) |
| Options Pattern | Typed config: JwtOptions, SendGridOptions, PayPalOptions, CashFreeOptions, R2Options |
| Pipeline Decorator | MediatR behaviours for validation, logging, performance |

---

## 6. USER ROLES

| role_id | Name | Can Do |
|---|---|---|
| 1 | Admin | Everything — CRUD all entities, reports, audit log, publish programs |
| 2 | Expert | Manage own programs, see enrolled users, send progress updates, update own profile |
| 3 | User | Browse catalog, purchase programs, view own purchases |

Use policy-based auth:
- `[Authorize(Policy = "AdminOnly")]`
- `[Authorize(Policy = "ExpertOrAdmin")]`
- `[Authorize]` for any authenticated user

---

## 7. BUSINESS RULES — ALWAYS ENFORCE

1. **Email NOT enforced at purchase** (MVP decision) — `is_email_verified` stored but not checked for purchase.
2. **Mobile OTP** — NOT implemented at MVP. Collect number, no verification.
3. **Coupon discount** is capped so final price is never below 1 unit of currency (e.g. min £1, $1, ₹1).
4. **Program status flow**: `DRAFT → PENDING_REVIEW → PUBLISHED → ARCHIVED`. Expert creates as DRAFT, Admin publishes.
5. **Admin mutations**: Every change logged to `admin_audit_log` with before/after JSON snapshot.
6. **Expert price changes**: Also logged to `admin_audit_log`.
7. **Payment webhooks**: Signature verified BEFORE any DB update. Reject unverified webhooks with 401.
8. **Refresh token rotation**: Old token revoked immediately when new one is issued.
9. **Soft deletes**: Never hard-delete users, experts, or programs. Set `is_deleted = true`.
10. **Idempotency**: Orders have `idempotency_key` (client-generated UUID). Duplicate key = return existing order.
11. **GDPR**: Users in GB/EU can request data erasure via `POST /api/v1/users/me/gdpr-deletion-request`. Stored in `gdpr_deletion_requests` table, processed manually by Admin.

---

## 8. COUNTRY CODE — DUAL STORAGE

Users register with both:
- `country_dial_code` — what they type: `+91`, `+44`, `+1`
- `country_iso_code` — derived ISO code: `IN`, `GB`, `US`
- `mobile_number` — digits only, no dial code
- `full_mobile` — concatenated: `+917890001234`

**Dial code to ISO mapping** (implement as a utility in Domain layer):
```
+91  → IN
+44  → GB
+1   → US
+61  → AU
+971 → AE
(add more as needed)
```

Payment gateway selection is based on `country_iso_code`:
- `IN` → CashFree
- Anything else → PayPal

---

## 9. STANDARD API RESPONSE FORMAT

Base URL: `/api/v1`

**Success**: HTTP 200/201 with typed response body.

**All errors (4xx / 5xx)**:
```json
{
  "type": "https://femved.com/errors/[error-type]",
  "title": "Human readable title",
  "status": 400,
  "detail": "Specific detail message",
  "instance": "/api/v1/path/that/failed",
  "errors": {
    "fieldName": ["Validation message"]
  }
}
```

Use `ProblemDetails` (built into ASP.NET Core). Create a global `ExceptionHandlingMiddleware` that catches:
- `NotFoundException` → 404
- `ValidationException` (FluentValidation) → 400 with field errors
- `UnauthorizedException` → 401
- `ForbiddenException` → 403
- `DomainException` → 422
- All others → 500 (log full exception, return generic message)

---

## 10. ENVIRONMENT VARIABLES (NEVER HARDCODE ANY OF THESE)

```
# Database
DB_CONNECTION_STRING=Host=...;Database=femved_db;Username=...;Password=...

# JWT
JWT_SECRET=<256-bit-secret>
JWT_ISSUER=https://api.femved.com
JWT_AUDIENCE=https://femved.com
JWT_ACCESS_EXPIRY_MINUTES=15
JWT_REFRESH_EXPIRY_DAYS=7

# SendGrid
SENDGRID_API_KEY=SG.xxxxx
SENDGRID_FROM_EMAIL=hello@femved.com
SENDGRID_FROM_NAME=FemVed

# Twilio
TWILIO_ACCOUNT_SID=ACxxxx
TWILIO_AUTH_TOKEN=xxxx
TWILIO_WHATSAPP_FROM=whatsapp:+14155238886

# CashFree (India)
CASHFREE_CLIENT_ID=xxxx
CASHFREE_CLIENT_SECRET=xxxx
CASHFREE_BASE_URL=https://api.cashfree.com/pg   # prod: https://api.cashfree.com/pg

# PayPal (UK/US)
PAYPAL_CLIENT_ID=xxxx
PAYPAL_SECRET=xxxx
PAYPAL_BASE_URL=https://api-m.sandbox.paypal.com  # prod: https://api-m.paypal.com

# Cloudflare R2
R2_ENDPOINT=https://<account-id>.r2.cloudflarestorage.com
R2_ACCESS_KEY=xxxx
R2_SECRET_KEY=xxxx
R2_BUCKET_NAME=femved-assets

# App
APP_BASE_URL=https://femved.com
ASPNETCORE_ENVIRONMENT=Production
```

---

## 11. NOTIFICATION TEMPLATES (SendGrid)

| template_key | Trigger | Recipient |
|---|---|---|
| `purchase_success` | Order status → PAID | User |
| `purchase_failed` | Order status → FAILED | User |
| `program_reminder` | 24h before start_date | User |
| `expert_new_enrollment` | Order status → PAID | Expert |
| `password_reset` | Forgot password | User |
| `email_verify` | Post-registration | User |
| `expert_progress_update` | Expert sends update from dashboard | User |

WhatsApp templates (Twilio, pre-approved by Meta before launch):
- `purchase_confirmation_wa`
- `program_reminder_wa`

---

## 12. COMPLETE DATABASE SCHEMA REFERENCE

> The authoritative schema is in `/database/femved_schema.sql`.
> EF Core migrations must match this exactly.
> When creating migrations, always tell me the migration name and what it adds.

### Tables and their purpose:
- `roles` — seed data: Admin(1), Expert(2), User(3)
- `users` — all platform users, dual country code storage
- `refresh_tokens` — JWT refresh token rotation
- `password_reset_tokens` — time-limited email reset links
- `guided_domains` — top-level domain: "Guided 1:1 Care"
- `guided_categories` — category pages (supports subcategories via parent_id)
- `category_whats_included` — bullet list on category hero page
- `category_key_areas` — key support areas listed on category page
- `experts` — expert profiles linked to user accounts. Key columns: `expert_grid_description` (short bio for grid cards, max 500, was `short_bio`), `expert_detailed_description` (detailed bio for program detail page, nullable TEXT)
- `programs` — individual programs per category per expert
- `program_what_you_get` — what's included bullet list on program page
- `program_who_is_this_for` — target audience on program page
- `program_tags` — filter tags (stress, hormones, pcos, gut-health, etc.)
- `program_detail_sections` — heading + description pairs shown on the program detail page (e.g. "Reset Stress Patterns")
- `program_testimonials` — reviews shown on program pages and homepage
- `program_durations` — duration options per program (4 weeks, 6 weeks, 8 weeks)
- `duration_prices` — location-specific pricing per duration (IN/GB/US)
- `coupons` — discount codes (PERCENTAGE or FLAT)
- `orders` — purchase records with idempotency key
- `refunds` — refund records linked to orders
- `user_program_access` — post-purchase access record (what user can access)
- `expert_progress_updates` — expert messages to enrolled users
- `notification_log` — audit of all emails/WhatsApp/SMS sent
- `admin_audit_log` — every admin or expert mutation logged with before/after
- `gdpr_deletion_requests` — right to erasure requests from UK/EU users

---

## 13. GUIDED TREE API — EXACT JSON CONTRACT

`GET /api/v1/guided/tree` — public, cached 10 minutes.
The React frontend binds directly to this shape. **Field names must match exactly (camelCase).**

```json
{
  "domains": [
    {
      "domainId": "uuid",
      "domainName": "Guided 1:1 Care",
      "categories": [
        {
          "categoryId": "uuid",
          "categoryName": "hormonal-health-support",
          "categoryPageData": {
            "categoryPageDataImage": "string url",
            "categoryType": "Hormonal Health Support",
            "categoryHeroTitle": "Get Guided Hormonal Care",
            "categoryHeroSubtext": "string",
            "categoryCtaLabel": "Book Your Program",
            "categoryCtaTo": "/guided/hormonal-health-support",
            "whatsIncludedInCategory": ["string item 1", "string item 2"],
            "categoryPageHeader": "string",
            "categoryPageKeyAreas": ["string area 1", "string area 2"]
          },
          "programsInCategory": [
            {
              "programId": "uuid",
              "programName": "Break the Stress–Hormone–Health Triangle",
              "programGridDescription": "string",
              "programGridImage": "string url",
              "expertId": "uuid",
              "expertName": "Dr. Prathima Nagesh",
              "expertTitle": "Ayurvedic Physician & Women's Health Specialist",
              "expertGridDescription": "string short bio for grid card (max 500 chars)",
              "expertDetailedDescription": "string detailed bio for program detail page (nullable)",
              "programDurations": [
                {
                  "durationId": "uuid",
                  "durationLabel": "6 weeks",
                  "durationPrice": "£320"
                }
              ],
              "programPageDisplayDetails": {
                "overview": "string",
                "whatYouGet": ["string item 1"],
                "whoIsThisFor": ["string item 1"],
                "detailSections": [
                  {
                    "heading": "Reset Stress Patterns",
                    "description": "In this 6-week guided program..."
                  }
                ]
              }
            }
          ]
        }
      ]
    }
  ]
}
```

**Note on durationPrice**: Return the price for the user's detected location.
Detect location from: (1) authenticated user's `country_iso_code`, or (2) `Accept-Language` header, or (3) default to `GB`.
Format: symbol + amount string, e.g. `"£320"`, `"$400"`, `"₹33,000"`.

---

## 14. PAYMENT FLOW

### Initiate Order: `POST /api/v1/orders/initiate` [Auth: User]
```json
Request:  { "durationId": "uuid", "couponCode": "optional string", "idempotencyKey": "client-uuid" }
Response (CashFree - India): {
  "orderId": "uuid",
  "gatewayOrderId": "cf_xxx",
  "paymentSessionId": "cashfree_session_token",
  "amount": 33000.00,
  "currency": "INR",
  "symbol": "₹",
  "gateway": "CASHFREE"
}
Response (PayPal - GB/US): {
  "orderId": "uuid",
  "approvalUrl": "https://paypal.com/checkoutnow?token=...",
  "amount": 320.00,
  "currency": "GBP",
  "symbol": "£",
  "gateway": "PAYPAL"
}
```

### Webhook Endpoints [Public — validate signature only]:
- `POST /api/v1/payments/cashfree/webhook`
- `POST /api/v1/payments/paypal/webhook`

### Post-payment domain event flow (OrderPaidEvent):
1. Create `user_program_access` record
2. Send `purchase_success` email via SendGrid
3. Send WhatsApp message via Twilio (if user opted in)
4. Send `expert_new_enrollment` email to expert
5. Log all notifications to `notification_log`

---

## 15. BUILD PHASES — FOLLOW THIS ORDER STRICTLY

| Phase | Scope |
|---|---|
| **Phase 1** | Solution setup, all 5 projects, NuGet packages, EF DbContext, all migrations for ALL tables, seed data migration |
| **Phase 2** | Auth — Register, Login, Refresh, Logout, ForgotPassword, ResetPassword, VerifyEmail |
| **Phase 3** | Guided Catalog — Domains/Categories/Programs CRUD + `GET /guided/tree` + individual detail endpoints |
| **Phase 4** | Payments — CashFree + PayPal gateway, `POST /orders/initiate`, webhooks, refunds |
| **Phase 5** | Notifications — SendGrid email + Twilio WhatsApp post-purchase |
| **Phase 6** | User Dashboard — profile, purchase history, program access |
| **Phase 7** | Expert Dashboard — enrollments, progress updates, program management |
| **Phase 8** | Admin — full CRUD, reports, audit log, coupon management, GDPR deletion processing |
| **Phase 9** | Hardening — rate limiting, CORS, health checks, reminder background job |
| **Phase 10** | Final docs — README.md polish, Swagger annotations review, deployment checklist |

---

## 16. HOW I WANT YOU TO WORK (CLAUDE CODE RULES)

1. **One phase at a time.** Never start the next phase until I type "Phase X confirmed, proceed."
2. **Before writing any code** for a phase, list every file you will create or modify. Wait for my "go ahead."
3. **After each phase**, show me a curl test command for every new endpoint created.
4. **Ask before deciding** — if there's an ambiguous choice (naming, pattern, exception type), ask me before coding.
5. **Migration discipline** — before running `dotnet ef migrations add`, tell me:
   - The migration name you plan to use
   - Exactly what the migration will add/change
   - Then wait for confirmation.
6. **Always include**:
   - XML doc comments on all public methods and interfaces
   - `[ProducesResponseType]` on all controller actions
   - A FluentValidation class for every Command that has input
7. **Logging**: Every handler logs at start (Information) and end (Information). Errors are logged as Error with full exception. Never log passwords, tokens, or card data.
8. **At the end of every response**, tell me: what you just built, what files were created/modified, and what the next step is.

---

## 17. PACKAGES TO INSTALL (Phase 1)

```xml
<!-- FemVed.API -->
Swashbuckle.AspNetCore
Serilog.AspNetCore
Serilog.Sinks.Console

<!-- FemVed.Application -->
MediatR
FluentValidation.DependencyInjectionExtensions

<!-- FemVed.Infrastructure -->
Microsoft.EntityFrameworkCore
Npgsql.EntityFrameworkCore.PostgreSQL
Microsoft.EntityFrameworkCore.Design
BCrypt.Net-Next
Microsoft.IdentityModel.Tokens
System.IdentityModel.Tokens.Jwt
SendGrid
Twilio
AWSSDK.S3
```

---

## 18. FIRST MESSAGE TO SEND ME WHEN I START A NEW SESSION

If the user says "start" or "continue" or asks what to do next — respond with:

"I've read CLAUDE.md. Here is the current state: [summarise what phases are done based on what code exists in the repo]. The next step is Phase [N]: [description]. Before I write any code, here are the files I plan to create: [list]. Shall I proceed?"
