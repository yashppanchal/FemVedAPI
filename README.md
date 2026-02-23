# FemVed API

> Enterprise-grade women's wellness platform — .NET 10 backend REST API.

---

## Overview

FemVed is a women's wellness platform connecting users with expert-led guided programs.
This repository is the **backend API only**. A separate React frontend consumes these APIs.

Three product modules (built in phases):
1. **Guided Programs** — Live 1:1 expert-led programs *(this API, fully implemented)*
2. Recorded Content — Video library, Udemy-style *(Phase 2)*
3. Events / Retreats *(Phase 3)*

---

## Tech Stack

| Layer | Choice |
|---|---|
| Framework | .NET 10, ASP.NET Core Web API |
| Database | PostgreSQL (Railway hosted) |
| ORM | Entity Framework Core 10, code-first, Fluent API |
| Validation | FluentValidation |
| Mediator / CQRS | MediatR 12 |
| Auth | JWT Bearer tokens + Refresh token rotation |
| Password hashing | BCrypt.Net-Next (work factor 12) |
| Email | SendGrid (dynamic templates) |
| WhatsApp | Twilio (WhatsApp Business API) |
| Payments — India | CashFree Payments API |
| Payments — UK/US | PayPal Orders API v2 |
| File storage | Cloudflare R2 (S3-compatible) |
| Logging | Serilog + structured JSON |
| API docs | Swagger / Swashbuckle |
| Rate limiting | ASP.NET Core built-in rate limiting |
| Hosting | Railway (Linux container) |
| CI/CD | GitHub Actions |

---

## Solution Structure

```
FemVed.sln
├── src/
│   ├── FemVed.API/              ← ASP.NET Core Web API (thin controllers only)
│   │   ├── Controllers/
│   │   ├── Middleware/          ← ExceptionHandling, CorrelationId, RequestLogging
│   │   ├── Extensions/          ← Swagger, JWT, Authorization registration
│   │   └── Program.cs
│   ├── FemVed.Application/      ← All business logic
│   │   ├── Admin/               ← Admin commands + queries
│   │   ├── Auth/                ← Register, Login, Refresh, Logout, Password reset
│   │   ├── Common/Behaviours/   ← Validation, Logging, Performance pipeline
│   │   ├── Experts/             ← Expert dashboard
│   │   ├── Guided/              ← Catalog queries
│   │   ├── Interfaces/          ← IRepository<T>, IUnitOfWork, IEmailService, etc.
│   │   ├── Payments/            ← Order initiation, webhooks, refunds
│   │   └── Users/               ← User dashboard
│   ├── FemVed.Domain/           ← Pure domain — zero external dependencies
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── Events/
│   │   ├── Exceptions/
│   │   └── ValueObjects/
│   └── FemVed.Infrastructure/   ← All external concerns
│       ├── BackgroundJobs/      ← ProgramReminderJob (24h reminder emails)
│       ├── Guided/              ← EF Core catalog read service
│       ├── Notifications/       ← SendGrid, Twilio
│       ├── Payment/             ← CashFree, PayPal gateways + factory
│       ├── Persistence/         ← AppDbContext, Migrations, Repositories
│       └── Security/            ← JwtService
└── tests/
    └── FemVed.Tests/
```

---

## Local Development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL 15+ (local or [Railway](https://railway.app))
- SendGrid account (for transactional email)
- Twilio account (for WhatsApp — set `WHATSAPP_ENABLED=false` to skip locally)
- CashFree sandbox account (for India payments)
- PayPal sandbox account (for UK/US payments)
- Cloudflare R2 bucket (or any S3-compatible storage)

### 1. Clone and restore

```bash
git clone https://github.com/your-org/FemVedAPI.git
cd FemVedAPI
dotnet restore
```

### 2. Configure environment variables

Create a `.env` file at the solution root (or set variables in your shell / Railway dashboard):

```bash
# Database
DB_CONNECTION_STRING="Host=localhost;Database=femved_db;Username=postgres;Password=yourpassword"

# JWT
JWT_SECRET="your-256-bit-secret-minimum-32-characters-long"
JWT_ISSUER="https://api.femved.com"
JWT_AUDIENCE="https://femved.com"
JWT_ACCESS_EXPIRY_MINUTES=15
JWT_REFRESH_EXPIRY_DAYS=7

# SendGrid
SENDGRID_API_KEY="SG.xxxxxxxxxxxx"
SENDGRID_FROM_EMAIL="hello@femved.com"
SENDGRID_FROM_NAME="FemVed"

# SendGrid template IDs (from SendGrid dashboard -> Email API -> Dynamic Templates)
SENDGRID_TEMPLATE_PURCHASE_SUCCESS="d-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
SENDGRID_TEMPLATE_PURCHASE_FAILED="d-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
SENDGRID_TEMPLATE_PROGRAM_REMINDER="d-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
SENDGRID_TEMPLATE_EXPERT_NEW_ENROLLMENT="d-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
SENDGRID_TEMPLATE_PASSWORD_RESET="d-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
SENDGRID_TEMPLATE_EMAIL_VERIFY="d-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
SENDGRID_TEMPLATE_EXPERT_PROGRESS_UPDATE="d-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"

# Twilio (set WHATSAPP_ENABLED=false to disable WhatsApp entirely)
WHATSAPP_ENABLED=false
TWILIO_ACCOUNT_SID="ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
TWILIO_AUTH_TOKEN="xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
TWILIO_WHATSAPP_FROM="whatsapp:+14155238886"

# CashFree (India payments)
CASHFREE_BASE_URL="https://sandbox.cashfree.com/pg"
CASHFREE_CLIENT_ID="your-cashfree-client-id"
CASHFREE_CLIENT_SECRET="your-cashfree-client-secret"

# PayPal (UK/US payments)
PAYPAL_BASE_URL="https://api-m.sandbox.paypal.com"
PAYPAL_CLIENT_ID="your-paypal-client-id"
PAYPAL_SECRET="your-paypal-secret"
PAYPAL_WEBHOOK_ID="your-paypal-webhook-id"

# Cloudflare R2
R2_ENDPOINT="https://<account-id>.r2.cloudflarestorage.com"
R2_ACCESS_KEY="your-r2-access-key"
R2_SECRET_KEY="your-r2-secret-key"
R2_BUCKET_NAME="femved-assets"

# App
APP_BASE_URL="https://femved.com"
ASPNETCORE_ENVIRONMENT="Development"
```

### 3. Apply database migrations

```bash
dotnet ef database update \
  --project src/FemVed.Infrastructure \
  --startup-project src/FemVed.API
```

### 4. Run

```bash
dotnet run --project src/FemVed.API
```

- **Swagger UI**: `https://localhost:5001/swagger`
- **Health check**: `https://localhost:5001/health`

---

## Environment Variables Reference

| Variable | Required | Description |
|---|---|---|
| `DB_CONNECTION_STRING` | Yes | Npgsql connection string |
| `JWT_SECRET` | Yes | Min 32-char signing secret |
| `JWT_ISSUER` | Yes | Token issuer claim |
| `JWT_AUDIENCE` | Yes | Token audience claim |
| `JWT_ACCESS_EXPIRY_MINUTES` | Yes | Access token TTL (recommended: 15) |
| `JWT_REFRESH_EXPIRY_DAYS` | Yes | Refresh token TTL (recommended: 7) |
| `SENDGRID_API_KEY` | Yes | SendGrid API key |
| `SENDGRID_FROM_EMAIL` | Yes | Sender email address |
| `SENDGRID_FROM_NAME` | No | Sender display name (default: FemVed) |
| `SENDGRID_TEMPLATE_PURCHASE_SUCCESS` | Yes | SendGrid template ID |
| `SENDGRID_TEMPLATE_PURCHASE_FAILED` | Yes | SendGrid template ID |
| `SENDGRID_TEMPLATE_PROGRAM_REMINDER` | Yes | SendGrid template ID |
| `SENDGRID_TEMPLATE_EXPERT_NEW_ENROLLMENT` | Yes | SendGrid template ID |
| `SENDGRID_TEMPLATE_PASSWORD_RESET` | Yes | SendGrid template ID |
| `SENDGRID_TEMPLATE_EMAIL_VERIFY` | Yes | SendGrid template ID |
| `SENDGRID_TEMPLATE_EXPERT_PROGRESS_UPDATE` | Yes | SendGrid template ID |
| `WHATSAPP_ENABLED` | No | `true` to enable WhatsApp (default: false) |
| `TWILIO_ACCOUNT_SID` | If WA | Twilio Account SID |
| `TWILIO_AUTH_TOKEN` | If WA | Twilio Auth Token |
| `TWILIO_WHATSAPP_FROM` | If WA | WhatsApp sender number |
| `CASHFREE_BASE_URL` | Yes | CashFree API base URL |
| `CASHFREE_CLIENT_ID` | Yes | CashFree Client ID |
| `CASHFREE_CLIENT_SECRET` | Yes | CashFree Client Secret |
| `PAYPAL_BASE_URL` | Yes | PayPal API base URL |
| `PAYPAL_CLIENT_ID` | Yes | PayPal Client ID |
| `PAYPAL_SECRET` | Yes | PayPal Secret |
| `PAYPAL_WEBHOOK_ID` | Yes | PayPal Webhook ID (for signature verification) |
| `R2_ENDPOINT` | Yes | Cloudflare R2 endpoint URL |
| `R2_ACCESS_KEY` | Yes | R2 Access Key |
| `R2_SECRET_KEY` | Yes | R2 Secret Key |
| `R2_BUCKET_NAME` | Yes | R2 Bucket name |
| `APP_BASE_URL` | Yes | Frontend URL (for CORS and email links) |

---

## API Endpoints

Base URL: `/api/v1`

### Authentication

| Method | Path | Auth | Rate Limit | Description |
|---|---|---|---|---|
| POST | `/auth/register` | None | 10/min | Register new user account |
| POST | `/auth/login` | None | 10/min | Login — returns JWT + refresh token |
| POST | `/auth/refresh` | None | 120/min | Rotate refresh token |
| POST | `/auth/logout` | Bearer | 120/min | Revoke refresh token |
| POST | `/auth/forgot-password` | None | 10/min | Send password reset email |
| POST | `/auth/reset-password` | None | 10/min | Complete password reset |
| GET | `/auth/verify-email?token=` | None | 120/min | Verify email address |

### Guided Catalog (public)

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/guided/tree` | None | Full catalog tree (cached 10 min) |
| GET | `/guided/tree/{locationCode}` | None | Catalog for specific location (IN/GB/US) |
| GET | `/guided/domains/{domainId}` | None | Single domain details |
| GET | `/guided/categories/{slug}` | None | Category page data |
| GET | `/guided/programs/{slug}` | None | Program detail page |

### Orders & Payments

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/orders/initiate` | Bearer (User) | Initiate a purchase |
| GET | `/orders/{orderId}` | Bearer | Get order details |
| GET | `/orders/my` | Bearer | List own orders |
| POST | `/payments/cashfree/webhook` | None (sig verified) | CashFree payment webhook |
| POST | `/payments/paypal/webhook` | None (sig verified) | PayPal payment webhook |
| POST | `/orders/{orderId}/refund` | Bearer (Admin) | Initiate refund |

### User Dashboard

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/users/me` | Bearer | Get own profile |
| PUT | `/users/me` | Bearer | Update own profile |
| GET | `/users/me/program-access` | Bearer | List purchased program access records |
| POST | `/users/me/gdpr-deletion-request` | Bearer | Submit GDPR right-to-erasure request |

### Expert Dashboard

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/experts/me` | Bearer (Expert) | Get own expert profile |
| GET | `/experts/me/programs` | Bearer (Expert) | List own programs with enrollment counts |
| GET | `/experts/me/enrollments` | Bearer (Expert) | List all enrolled users |
| POST | `/experts/me/enrollments/{accessId}/progress-update` | Bearer (Expert) | Send progress update to user |

### Admin

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/admin/summary` | Bearer (Admin) | Dashboard stats |
| GET | `/admin/users` | Bearer (Admin) | All user accounts |
| PUT | `/admin/users/{id}/activate` | Bearer (Admin) | Activate user |
| PUT | `/admin/users/{id}/deactivate` | Bearer (Admin) | Deactivate user |
| DELETE | `/admin/users/{id}` | Bearer (Admin) | Soft-delete user |
| GET | `/admin/experts` | Bearer (Admin) | All expert profiles |
| PUT | `/admin/experts/{id}/activate` | Bearer (Admin) | Activate expert |
| PUT | `/admin/experts/{id}/deactivate` | Bearer (Admin) | Deactivate expert |
| GET | `/admin/coupons` | Bearer (Admin) | All coupons |
| POST | `/admin/coupons` | Bearer (Admin) | Create coupon |
| PUT | `/admin/coupons/{id}` | Bearer (Admin) | Update coupon |
| PUT | `/admin/coupons/{id}/deactivate` | Bearer (Admin) | Deactivate coupon |
| GET | `/admin/orders` | Bearer (Admin) | All orders |
| GET | `/admin/gdpr-requests` | Bearer (Admin) | GDPR erasure requests (default: Pending) |
| POST | `/admin/gdpr-requests/{id}/process` | Bearer (Admin) | Complete or reject GDPR request |
| GET | `/admin/audit-log` | Bearer (Admin) | Admin action audit log |

### System

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/health` | None | EF Core + PostgreSQL health check |

---

## Authorization

Three roles seed-loaded into the `roles` table:

| Role | ID | Policy | Used on |
|---|---|---|---|
| Admin | 1 | `AdminOnly` | All `/admin/*` endpoints |
| Expert | 2 | `ExpertOrAdmin` | Expert-specific mutations |
| User | 3 | `[Authorize]` | User dashboard, order initiation |

JWT claims: `sub` = userId, `role` = role name.

---

## Key Business Rules

- **Soft deletes** — Users, experts, and programs are never hard-deleted (`is_deleted = true`).
- **Payment gateway selection** — `country_iso_code = IN` uses CashFree; all others use PayPal.
- **Coupon discount** is capped so the final price is never below 1 unit of the currency.
- **Idempotent orders** — Client supplies `idempotencyKey`; duplicate key returns the existing order.
- **Webhook signatures** — Verified before any DB update; unsigned webhooks → 401.
- **Refresh token rotation** — Old token is revoked immediately when a new pair is issued.
- **Program status flow** — `DRAFT → PENDING_REVIEW → PUBLISHED → ARCHIVED`.
- **Admin audit log** — Every admin mutation writes a before/after JSON snapshot to `admin_audit_log`.
- **24h reminders** — `ProgramReminderJob` runs every hour, sends email + optional WhatsApp to enrolled users whose program starts the next calendar day (UTC).

---

## Deployment (Railway)

### First deploy

1. Create a new Railway service and connect this GitHub repository.
2. Set **all required environment variables** in the Railway dashboard (Variables tab).
3. Railway detects the .NET project and builds with `dotnet publish` automatically.
4. After the first deploy, run migrations via Railway shell or CLI:

```bash
# Using Railway CLI
railway run dotnet ef database update \
  --project src/FemVed.Infrastructure \
  --startup-project src/FemVed.API
```

### Production environment variable overrides

| Variable | Production value |
|---|---|
| `CASHFREE_BASE_URL` | `https://api.cashfree.com/pg` |
| `PAYPAL_BASE_URL` | `https://api-m.paypal.com` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

### Webhook registration

After deploying, register these webhook URLs in each payment provider's dashboard:

- **CashFree**: `https://api.femved.com/api/v1/payments/cashfree/webhook`
- **PayPal**: `https://api.femved.com/api/v1/payments/paypal/webhook`

After registering the PayPal webhook, copy the Webhook ID into `PAYPAL_WEBHOOK_ID`.

---

## Pre-Launch Checklist

### Infrastructure
- [ ] PostgreSQL production database provisioned and connection string set
- [ ] All required environment variables set in Railway dashboard
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Database migrations applied: `dotnet ef database update`
- [ ] Seed data confirmed: `roles` table has Admin(1), Expert(2), User(3)
- [ ] At least one Admin user account created in production DB

### Payments
- [ ] CashFree production credentials set (`CASHFREE_BASE_URL` = production URL)
- [ ] PayPal production credentials set (`PAYPAL_BASE_URL` = production URL)
- [ ] CashFree webhook URL registered and responding
- [ ] PayPal webhook URL registered — `PAYPAL_WEBHOOK_ID` copied into Railway vars
- [ ] End-to-end payment tested: initiate → webhook → access record created → emails sent

### Notifications
- [ ] All 7 `SENDGRID_TEMPLATE_*` IDs populated with production template IDs
- [ ] SendGrid sender domain authenticated (SPF, DKIM DNS records added)
- [ ] Password reset email tested with real reset link
- [ ] Purchase confirmation email and WhatsApp tested end-to-end
- [ ] WhatsApp templates pre-approved by Meta before enabling `WHATSAPP_ENABLED=true`

### Security
- [ ] `JWT_SECRET` is a securely generated 256-bit random secret
- [ ] `APP_BASE_URL` set to the production frontend URL (controls CORS)
- [ ] Swagger UI access reviewed — consider restricting in production if needed
- [ ] Rate limiting verified: auth endpoints return 429 after 10 requests/min

### Content & Data
- [ ] Guided domains, categories, and programs created via Admin API or seeded
- [ ] Expert profiles created and linked to user accounts

### Monitoring
- [ ] Health check reachable: `GET https://api.femved.com/health` returns `Healthy`
- [ ] Serilog logs visible in Railway log viewer
- [ ] Background reminder job visible in logs after first hour interval

---

## Error Response Format

All 4xx and 5xx responses follow RFC 7807 Problem Details:

```json
{
  "type": "https://femved.com/errors/not-found",
  "title": "Resource not found",
  "status": 404,
  "detail": "Expert with ID 'abc...' was not found.",
  "instance": "/api/v1/admin/experts/abc...",
  "errors": {
    "fieldName": ["Validation message"]
  }
}
```

| Exception | HTTP Status |
|---|---|
| `NotFoundException` | 404 |
| `ValidationException` (FluentValidation) | 400 with `errors` field |
| `UnauthorizedException` | 401 |
| `ForbiddenException` | 403 |
| `DomainException` | 422 |
| Unhandled | 500 (generic message, full exception logged) |
