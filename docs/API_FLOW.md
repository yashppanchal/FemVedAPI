# FemVed API — Complete Flow & Architecture Guide

> **Base URL:** `https://api.femved.com/api/v1`
> **Auth:** JWT Bearer — add `Authorization: Bearer <access_token>` to every authenticated request.

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Auth Flow](#2-auth-flow)
3. [Guided Catalog Flow](#3-guided-catalog-flow)
4. [Purchase & Payment Flow](#4-purchase--payment-flow)
5. [User Dashboard Flow](#5-user-dashboard-flow)
6. [Expert Dashboard Flow](#6-expert-dashboard-flow)
7. [Admin Dashboard Flow](#7-admin-dashboard-flow)
8. [Notification System](#8-notification-system)
9. [Background Jobs](#9-background-jobs)
10. [Error Response Format](#10-error-response-format)
11. [Role & Policy Summary](#11-role--policy-summary)
12. [End-to-End Scenario: User Buys a Program](#12-end-to-end-scenario-user-buys-a-program)
13. [Mutation Confirmation Responses](#13-mutation-confirmation-responses)

---

## 1. Architecture Overview

```
Request
  │
  ├─► CorrelationIdMiddleware   (sets X-Correlation-Id header)
  ├─► ExceptionHandlingMiddleware (catches domain exceptions → ProblemDetails)
  ├─► RequestLoggingMiddleware  (logs method, path, latency)
  ├─► Rate Limiter              (global: 120/min; auth: 10/min)
  ├─► Authentication / Authorization (JWT Bearer)
  │
  └─► Controller (thin — validates nothing, just calls MediatR)
        │
        └─► MediatR Pipeline
              ├─ ValidationBehaviour  (FluentValidation — runs first)
              ├─ LoggingBehaviour     (logs request start/end)
              ├─ PerformanceBehaviour (warns if > 500 ms)
              └─► Handler (all business logic lives here)
                    │
                    ├─► IRepository<T>  (EF Core Repository)
                    ├─► IUnitOfWork     (SaveChangesAsync)
                    ├─► IEmailService   (SendGrid)
                    ├─► IWhatsAppService (Twilio)
                    └─► IPaymentGateway  (CashFree | PayPal)
```

### Clean Architecture Layers

| Layer | Project | Rules |
|---|---|---|
| **Domain** | `FemVed.Domain` | Pure C# — no framework references. Entities, enums, exceptions, value objects. |
| **Application** | `FemVed.Application` | Business logic. MediatR handlers, validators, DTOs, interface definitions. |
| **Infrastructure** | `FemVed.Infrastructure` | EF Core, repositories, SendGrid, Twilio, CashFree, PayPal, R2 implementations. |
| **API** | `FemVed.API` | ASP.NET Core controllers, middleware, DI wiring. |

---

## 2. Auth Flow

### Register

```
POST /api/v1/auth/register
Body: { email, password, firstName, lastName, countryCode?, mobileNumber? }

Flow:
  1. FluentValidation: email format, password complexity, mobile cross-field rules
  2. Check email uniqueness → 400 if duplicate
  3. Hash password (BCrypt, work factor 12)
  4. Derive country ISO code from dial code (IN, GB, US, AU, AE…)
  5. Create User (role_id = 3)
  6. Generate refresh token → hash → store in refresh_tokens
  7. SaveChanges
  8. Generate JWT access token (15 min)
  9. Send "email_verify" email via SendGrid (non-blocking)

Response 201:
  { accessToken, refreshToken, accessTokenExpiry, user: { id, email, firstName, lastName, role } }
```

### Login

```
POST /api/v1/auth/login
Body: { email, password }

Flow:
  1. Look up user by email
  2. BCrypt.Verify password (constant-time: dummy hash run even if user not found)
  3. Check IsActive → 401 if deactivated
  4. Create new refresh token → hash → store
  5. Generate JWT access token

Response 200: { accessToken, refreshToken, accessTokenExpiry, user }
Errors: 401 invalid credentials | 401 deactivated
```

### Refresh Token

```
POST /api/v1/auth/refresh
Body: { accessToken (expired), refreshToken (raw) }

Flow:
  1. Extract userId from expired access token (signature checked, expiry ignored)
  2. SHA-256 hash the raw refresh token
  3. Find matching, non-revoked, non-expired refresh token for this user
  4. Revoke old token immediately (rotation)
  5. Issue new access token + new refresh token

Response 200: { accessToken, refreshToken, accessTokenExpiry, user }
Errors: 401 if token not found or revoked
```

### Forgot Password

```
POST /api/v1/auth/forgot-password  [Rate: 10/min]
Body: { email }

Flow:
  1. Silently succeed even if email not found (prevents enumeration)
  2. Generate a 6-hour password reset token (CSPRNG)
  3. Store hashed token in password_reset_tokens
  4. Send "password_reset" email with reset link

Response 200: { message: "If this email is registered, a reset link has been sent." }
```

### Reset Password

```
POST /api/v1/auth/reset-password  [Rate: 10/min]
Body: { token, newPassword }

Flow:
  1. SHA-256 hash the raw token
  2. Find valid (non-used, non-expired) token record
  3. BCrypt hash the new password
  4. Update user password
  5. Mark token as used
  6. Revoke ALL active refresh tokens for this user (force re-login everywhere)

Response 200: { message: "Password has been reset successfully." }
Errors: 400 if token invalid/expired
```

### Email Verification

```
GET /api/v1/auth/verify-email?token=<jwt>

Flow:
  1. Validate signed JWT (email verification token, 24-hour expiry)
  2. Set user.IsEmailVerified = true

Response 200: { message: "Email verified successfully." }
Errors: 400 if token invalid/expired
```

---

## 3. Guided Catalog Flow

### Get Full Tree (Public, cached 10 min)

```
GET /api/v1/guided/tree

Flow:
  1. GuidedCatalogReadService checks in-memory cache (key: "tree:{locationCode}")
  2. Cache miss → EF Core projection joins:
     guided_domains → guided_categories → programs → experts → durations → prices
  3. Location detection:
     - Authenticated? → use user.CountryIsoCode
     - Accept-Language header? → parse "en-GB" → "GB"
     - Default → "GB"
  4. Return prices in detected location's currency

Response 200: { domains: [ { domainId, domainName, categories: [...] } ] }
```

**Price format by location:**
| LocationCode | Symbol | Example |
|---|---|---|
| IN | ₹ | ₹33,000 |
| GB | £ | £320 |
| US | $ | $400 |

### Get Category by Slug (Public)

```
GET /api/v1/guided/categories/{slug}

Returns: full category page data including whatsIncluded[], keyAreas[], programsInCategory[]
Errors: 404 if not found
```

### Get Program by Slug (Public)

```
GET /api/v1/guided/programs/{slug}

Returns: full program detail + expert info + all durations with prices
Errors: 404 if not found
```

### Content Management (Admin only)

```
POST /api/v1/guided/domains          → Create guided domain
POST /api/v1/guided/categories       → Create category
PUT  /api/v1/guided/categories/{id}  → Update category
POST /api/v1/guided/programs         → Create program (Expert or Admin)
PUT  /api/v1/guided/programs/{id}    → Update program (Expert or Admin)
```

### Program Status Lifecycle

```
DRAFT ──► PENDING_REVIEW ──► PUBLISHED ──► ARCHIVED
  │                │
  │ (Expert)       │ (Admin publishes or rejects → back to DRAFT)
  └─ submit        └─ publish / archive
```

```
POST /api/v1/guided/programs/{id}/submit   [ExpertOrAdmin] → DRAFT → PENDING_REVIEW
POST /api/v1/guided/programs/{id}/publish  [AdminOnly]     → PENDING_REVIEW → PUBLISHED
POST /api/v1/guided/programs/{id}/archive  [AdminOnly]     → PUBLISHED → ARCHIVED
```

---

## 4. Purchase & Payment Flow

### Step 1: Initiate Order

```
POST /api/v1/orders/initiate  [Authenticated]
Body: { durationId, couponCode?, idempotencyKey }

Flow:
  1. Idempotency check: if idempotencyKey already exists → return existing order (no duplicate charge)
  2. Load DurationPrice → validate user has no active access to this program
  3. Apply coupon (if provided):
     - Check coupon is active, not expired, within MaxUses
     - Percentage: price × (1 - discount/100)
     - Flat: price - discount
     - Floor: final price ≥ 1 unit of currency
  4. Select payment gateway based on user.CountryIsoCode:
     - IN  → CashFree
     - Any → PayPal
  5. Create Order record (status = INITIATED)
  6. Call gateway API to create order:
     - CashFree: returns paymentSessionId
     - PayPal:   returns approvalUrl

Response 201 (CashFree):
  { orderId, gatewayOrderId, paymentSessionId, amount, currency, symbol, gateway: "CASHFREE" }

Response 201 (PayPal):
  { orderId, approvalUrl, amount, currency, symbol, gateway: "PAYPAL" }
```

### Step 2: User Completes Payment (Frontend)

- **CashFree:** Frontend uses `paymentSessionId` with CashFree JS SDK to show payment form
- **PayPal:** Frontend redirects user to `approvalUrl`, PayPal redirects back to `APP_BASE_URL/payment/success`

### Step 3: Webhook (Gateway → FemVed)

```
POST /api/v1/payments/cashfree/webhook  [Public — no auth]
POST /api/v1/payments/paypal/webhook    [Public — no auth]

Flow:
  1. Verify webhook signature FIRST — reject with 401 if invalid
  2. Parse gateway order ID → find our Order record
  3. Update Order.Status (PAID, FAILED, REFUNDED)
  4. If PAID → fire OrderPaidEvent (MediatR notification):
     a. Create user_program_access record
     b. Send "purchase_success" email (SendGrid)
     c. Send "purchase_confirmation_wa" WhatsApp (Twilio, if user opted in)
     d. Send "expert_new_enrollment" email to expert
     e. Log all to notification_log

Response 200: {} (gateway expects 200 immediately)
```

### Refund

```
POST /api/v1/orders/{id}/refund  [AdminOnly]
Body: { reason, amount? }

Flow:
  1. Validate order is PAID
  2. Call gateway refund API
  3. Create refund record (status = INITIATED)
  4. Order status → REFUNDED

Response 204: No Content
```

### View Orders

```
GET /api/v1/orders/{id}   [Authenticated — own orders + Admin]
GET /api/v1/orders/my     [Authenticated — returns caller's orders]
```

---

## 5. User Dashboard Flow

### Get My Profile

```
GET /api/v1/users/me  [Authenticated]
Returns: { userId, email, firstName, lastName, countryIsoCode, mobile, whatsAppOptIn, isEmailVerified, createdAt }
```

### Update My Profile

```
PUT /api/v1/users/me  [Authenticated]
Body: { firstName?, lastName?, countryCode?, mobileNumber?, whatsAppOptIn? }
Returns: updated UserProfileDto
```

### Get My Program Access

```
GET /api/v1/users/me/program-access  [Authenticated]
Returns: list of programs the user has purchased access to
  { accessId, programId, programName, expertName, status, reminderSent, createdAt }
```

### GDPR Deletion Request

```
POST /api/v1/users/me/gdpr-deletion-request  [Authenticated]

Flow:
  1. Check no pending request exists (idempotent)
  2. Insert into gdpr_deletion_requests (status = PENDING)
  3. Admin processes manually (see Admin flow)

Response 202: { message: "Your data deletion request has been received." }
```

---

## 6. Expert Dashboard Flow

### Get My Expert Profile

```
GET /api/v1/experts/me  [ExpertOrAdmin]
Returns: { expertId, userId, bio, specialisation, imageUrl, isActive, createdAt }
```

### Get My Programs

```
GET /api/v1/experts/me/programs  [ExpertOrAdmin]
Returns: list of programs the expert owns with status and enrollment counts
```

### Get My Enrollments

```
GET /api/v1/experts/me/enrollments  [ExpertOrAdmin]
Returns: list of all users enrolled in the expert's programs
  { accessId, userId, userName, userEmail, programName, status, createdAt }
```

### Send Progress Update

```
POST /api/v1/experts/me/enrollments/{accessId}/progress-update  [ExpertOrAdmin]
Body: { message }

Flow:
  1. Verify the accessId belongs to a program owned by this expert
  2. Insert ExpertProgressUpdate record
  3. Send "expert_progress_update" email to user (SendGrid)
  4. Optionally send WhatsApp notification
  5. Log to notification_log

Response 204: No Content
```

---

## 7. Admin Dashboard Flow

### Summary

```
GET /api/v1/admin/summary  [AdminOnly]
Returns: { totalUsers, activeUsers, totalExperts, totalPrograms, publishedPrograms,
           totalOrders, totalRevenue(INR), totalRevenue(GBP), pendingGdprRequests }
```

### User Management

```
GET /api/v1/admin/users                              → List all users
PUT /api/v1/admin/users/{userId}/activate            → Reactivate account
PUT /api/v1/admin/users/{userId}/deactivate          → Block login
DELETE /api/v1/admin/users/{userId}                  → Soft-delete (IsDeleted = true)
```

### Expert Management

```
GET /api/v1/admin/experts                            → List all experts
PUT /api/v1/admin/experts/{expertId}/activate        → Activate expert profile
PUT /api/v1/admin/experts/{expertId}/deactivate      → Deactivate expert profile
```

### Coupon Management

```
GET  /api/v1/admin/coupons                           → List all coupons
POST /api/v1/admin/coupons                           → Create coupon
  Body: { code, discountType (Percentage|Flat), discountValue, maxUses?, validFrom?, validUntil? }
  Rules:
    - code: uppercase letters/digits/hyphens/underscores, max 50 chars
    - discountType = Percentage: discountValue ≤ 100
    - validUntil must be future and after validFrom

PUT  /api/v1/admin/coupons/{couponId}                → Partial update
  Use ClearMaxUses/ClearValidFrom/ClearValidUntil = true to explicitly null optional fields

PUT  /api/v1/admin/coupons/{couponId}/deactivate     → Disable coupon
```

Every mutation logs to `admin_audit_log` with before/after JSON snapshot.

### Orders

```
GET /api/v1/admin/orders   → All orders with user/program/gateway details
```

### GDPR Deletion Requests

```
GET  /api/v1/admin/gdpr-requests?status=Pending     → Filter by status
POST /api/v1/admin/gdpr-requests/{requestId}/process
  Body: { action: "Complete" | "Reject", rejectionReason? }

Flow (Complete):
  - Mark request COMPLETED, set ProcessedBy = adminId, CompletedAt = now
  - Admin then manually: anonymise PII, export data, notify user

Flow (Reject):
  - Mark request REJECTED, RejectionReason required
```

### Audit Log

```
GET /api/v1/admin/audit-log?limit=100  → Last N admin/expert mutations
  Returns: { action, entityType, entityId, beforeValue, afterValue, adminUserId, ipAddress, createdAt }
```

---

## 8. Notification System

### Email (SendGrid Dynamic Templates)

| Template Key | Trigger | Recipient |
|---|---|---|
| `email_verify` | Registration | User |
| `password_reset` | Forgot password | User |
| `purchase_success` | Order PAID | User |
| `purchase_failed` | Order FAILED | User |
| `program_reminder` | 24h before start_date | User |
| `expert_new_enrollment` | Order PAID | Expert |
| `expert_progress_update` | Expert sends update | User |

**All emails are non-blocking.** Failures are caught, logged as `NotificationStatus.Failed` in `notification_log`, and never abort the main transaction.

### WhatsApp (Twilio — Meta-approved templates)

| Template | Trigger | Condition |
|---|---|---|
| `purchase_confirmation_wa` | Order PAID | `user.WhatsAppOptIn = true` AND `WHATSAPP_ENABLED = true` |
| `program_reminder_wa` | 24h before start_date | Same conditions |

### Notification Log

Every send attempt (success or failure) writes a row to `notification_log`:

```json
{
  "id": "uuid",
  "userId": "uuid",
  "type": "Email | WhatsApp | Sms",
  "templateKey": "purchase_success",
  "recipient": "user@example.com",
  "status": "Sent | Failed",
  "errorMessage": null,
  "createdAt": "2025-01-15T10:30:00Z"
}
```

---

## 9. Background Jobs

### ProgramReminderJob

Runs every hour as a `BackgroundService`.

```
Every hour:
  1. Compute tomorrow = DateOnly.FromDateTime(UtcNow + 1 day)
  2. Find all PUBLISHED programs with StartDate == tomorrow
  3. Find UserProgramAccess records where:
     - ProgramId in above set
     - Status = Active
     - ReminderSent = false   ← idempotency guard
  4. Batch-load all users
  5. For each access record:
     a. Send "program_reminder" email
     b. If user.WhatsAppOptIn + WHATSAPP_ENABLED → send "program_reminder_wa"
     c. Log to notification_log (per channel)
     d. Set access.ReminderSent = true
  6. SaveChanges (batch commit)
```

Failures for individual users never abort the batch — logged as Failed.

---

## 10. Error Response Format

All errors return RFC 7807 ProblemDetails:

```json
{
  "type": "https://femved.com/errors/validation",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/auth/register",
  "errors": {
    "email": ["A valid email address is required."],
    "password": ["Password must contain at least one uppercase letter."]
  }
}
```

| Exception | HTTP Status | type suffix |
|---|---|---|
| `ValidationException` | 400 | `/errors/validation` |
| `NotFoundException` | 404 | `/errors/not-found` |
| `UnauthorizedException` | 401 | `/errors/unauthorized` |
| `ForbiddenException` | 403 | `/errors/forbidden` |
| `DomainException` | 422 | `/errors/domain` |
| Any other | 500 | `/errors/internal` |

---

## 11. Role & Policy Summary

| Role | role_id | Policy |
|---|---|---|
| Admin | 1 | `[Authorize(Policy = "AdminOnly")]` |
| Expert | 2 | `[Authorize(Policy = "ExpertOrAdmin")]` |
| User | 3 | `[Authorize]` |

Roles are stored in the `roles` table and seeded in the initial migration.

---

## 12. End-to-End Scenario: User Buys a Program

```
1. User visits /guided/tree (no auth) → sees list of programs with prices
2. User clicks a program → GET /api/v1/guided/programs/{slug}
3. User registers → POST /api/v1/auth/register
   ← Receives: accessToken, refreshToken
   ← Receives: "email_verify" email in inbox
4. User selects a duration (e.g. 6 weeks) → POST /api/v1/orders/initiate
   Body: { durationId: "uuid", idempotencyKey: "client-uuid" }
   ← India user: receives paymentSessionId
   ← UK user: receives approvalUrl → redirected to PayPal
5. User completes payment on gateway
6. Gateway fires webhook → POST /api/v1/payments/cashfree/webhook
   ← Signature verified
   ← Order status → PAID
   ← UserProgramAccess record created
   ← "purchase_success" email sent to user
   ← "expert_new_enrollment" email sent to expert
   ← WhatsApp sent (if opted in)
7. User views their access → GET /api/v1/users/me/program-access
8. 24 hours before program starts:
   ← ProgramReminderJob sends "program_reminder" email
   ← ProgramReminderJob sends "program_reminder_wa" (if opted in)
9. Expert views enrollments → GET /api/v1/experts/me/enrollments
10. Expert sends update → POST /api/v1/experts/me/enrollments/{accessId}/progress-update
    ← "expert_progress_update" email sent to user
```

---

## 13. Mutation Confirmation Responses

All mutation endpoints now return explicit success JSON payloads instead of empty `204 No Content` responses.

Common response shapes:

```json
{ "id": "uuid", "isDeleted": true }
```

```json
{ "id": "uuid", "isUpdated": true }
```

```json
{ "id": "uuid", "isActive": false, "isUpdated": true }
```

```json
{ "id": "uuid", "status": "PUBLISHED", "isUpdated": true }
```

```json
{ "orderId": "uuid", "action": "REFUND_INITIATED", "isUpdated": true }
```

```json
{ "userId": "uuid", "isLoggedOut": true }
```

For quick automated verification, use the script:

```powershell
./docs/test-mutation-confirmations.ps1
```


## Quick Reference: All Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | /auth/register | Public (rate: 10/min) | Register new user |
| POST | /auth/login | Public (rate: 10/min) | Login |
| POST | /auth/refresh | Public | Refresh token |
| POST | /auth/logout | Public | Revoke refresh token |
| POST | /auth/forgot-password | Public (rate: 10/min) | Send reset email |
| POST | /auth/reset-password | Public (rate: 10/min) | Reset password |
| GET | /auth/verify-email | Public | Verify email |
| GET | /guided/tree | Public | Full guided catalog |
| GET | /guided/categories/{slug} | Public | Category page data |
| GET | /guided/programs/{slug} | Public | Program detail |
| POST | /guided/domains | Admin | Create domain |
| POST | /guided/categories | Admin | Create category |
| PUT | /guided/categories/{id} | Admin | Update category |
| POST | /guided/programs | Expert/Admin | Create program |
| PUT | /guided/programs/{id} | Expert/Admin | Update program |
| POST | /guided/programs/{id}/submit | Expert/Admin | Submit for review |
| POST | /guided/programs/{id}/publish | Admin | Publish program |
| POST | /guided/programs/{id}/archive | Admin | Archive program |
| POST | /orders/initiate | User | Initiate purchase |
| GET | /orders/{id} | User/Admin | Get order |
| GET | /orders/my | User | My orders |
| POST | /orders/{id}/refund | Admin | Initiate refund |
| POST | /payments/cashfree/webhook | Public | CashFree webhook |
| POST | /payments/paypal/webhook | Public | PayPal webhook |
| GET | /users/me | User | My profile |
| PUT | /users/me | User | Update profile |
| GET | /users/me/program-access | User | My program access |
| POST | /users/me/gdpr-deletion-request | User | Request data erasure |
| GET | /experts/me | Expert | My expert profile |
| GET | /experts/me/programs | Expert | My programs |
| GET | /experts/me/enrollments | Expert | My enrollments |
| POST | /experts/me/enrollments/{id}/progress-update | Expert | Send update |
| GET | /admin/summary | Admin | Dashboard stats |
| GET | /admin/users | Admin | All users |
| PUT | /admin/users/{id}/activate | Admin | Activate user |
| PUT | /admin/users/{id}/deactivate | Admin | Deactivate user |
| DELETE | /admin/users/{id} | Admin | Soft-delete user |
| GET | /admin/experts | Admin | All experts |
| PUT | /admin/experts/{id}/activate | Admin | Activate expert |
| PUT | /admin/experts/{id}/deactivate | Admin | Deactivate expert |
| GET | /admin/coupons | Admin | All coupons |
| POST | /admin/coupons | Admin | Create coupon |
| PUT | /admin/coupons/{id} | Admin | Update coupon |
| PUT | /admin/coupons/{id}/deactivate | Admin | Deactivate coupon |
| GET | /admin/orders | Admin | All orders |
| GET | /admin/gdpr-requests | Admin | GDPR requests |
| POST | /admin/gdpr-requests/{id}/process | Admin | Process GDPR |
| GET | /admin/audit-log | Admin | Audit log |
| GET | /health | Public | Health check |
