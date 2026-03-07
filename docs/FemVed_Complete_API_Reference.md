# FemVed API — Complete Reference
**Version:** Phase 8b
**Base URL:** `https://api.femved.com/api/v1`
**Last Updated:** March 2026

---

## Table of Contents

1. [Authentication & Authorization](#1-authentication--authorization)
2. [Standard Error Format](#2-standard-error-format)
3. [Auth Endpoints](#3-auth-endpoints)
4. [User Endpoints](#4-user-endpoints)
5. [Guided Catalog — Public](#5-guided-catalog--public)
6. [Orders & Payments](#6-orders--payments)
7. [Expert Dashboard](#7-expert-dashboard)
8. [Admin Dashboard](#8-admin-dashboard)
9. [Email Templates Reference](#9-email-templates-reference)
10. [Enum Values Reference](#10-enum-values-reference)

---

## 1. Authentication & Authorization

All protected endpoints require a JWT access token in the Authorization header:

```
Authorization: Bearer <access_token>
```

Access tokens expire after **15 minutes**. Use the refresh endpoint to get a new one without re-login.

### Roles & Policies

| Policy | Who Can Access |
|--------|---------------|
| *(no policy)* | Public — no token required |
| `Authorize` | Any logged-in user (User, Expert, Admin) |
| `ExpertOrAdmin` | Expert or Admin role only |
| `AdminOnly` | Admin role only |

---

## 2. Standard Error Format

All `4xx` and `5xx` errors return this shape:

```json
{
  "type": "https://femved.com/errors/not-found",
  "title": "Resource not found",
  "status": 404,
  "detail": "UserProgramAccess abc-123 was not found.",
  "instance": "/api/v1/experts/me/enrollments/abc-123/start"
}
```

Validation errors (400) also include a field-level `errors` object:

```json
{
  "type": "https://femved.com/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "email": ["Email is required.", "Email must be a valid email address."],
    "password": ["Password must be at least 8 characters."]
  }
}
```

### HTTP Status Code Summary

| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Created |
| 202 | Accepted (async operations) |
| 400 | Validation error |
| 401 | Missing or invalid token |
| 403 | Authenticated but not authorised |
| 404 | Resource not found |
| 422 | Business rule / domain error |
| 500 | Internal server error |

---

## 3. Auth Endpoints

**Base:** `/api/v1/auth`
**Auth:** All public unless noted.
**Rate limiting** applied to: register, login, forgot-password, reset-password.

---

### POST `/auth/register`

Creates a new user account.

**Request body:**
```json
{
  "firstName": "Priya",
  "lastName": "Sharma",
  "email": "priya@example.com",
  "password": "SecurePass123!",
  "countryCode": "+91",
  "mobileNumber": "9876543210",
  "whatsAppOptIn": true
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| firstName | string | ✅ | |
| lastName | string | ✅ | |
| email | string | ✅ | Must be valid email, unique |
| password | string | ✅ | Min 8 chars |
| countryCode | string | ✅ | e.g. `+91`, `+44`, `+1` |
| mobileNumber | string | ✅ | Digits only, no dial code |
| whatsAppOptIn | boolean | ✅ | `true` or `false` |

**Response 201:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "d4f5a6b7-c8d9-...",
  "expiresIn": 900,
  "user": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "firstName": "Priya",
    "email": "priya@example.com",
    "role": "User"
  }
}
```

---

### POST `/auth/login`

**Request body:**
```json
{
  "email": "priya@example.com",
  "password": "SecurePass123!"
}
```

**Response 200:** Same shape as register response.

---

### POST `/auth/refresh`

Use when the access token expires. The old refresh token is invalidated immediately and replaced.

**Request body:**
```json
{
  "refreshToken": "d4f5a6b7-c8d9-..."
}
```

**Response 200:** Same shape as register response (new access token + new refresh token).

---

### POST `/auth/logout`

**Auth:** `Authorize`

**Request body:**
```json
{
  "refreshToken": "d4f5a6b7-c8d9-..."
}
```

**Response 200:**
```json
{ "isLoggedOut": true }
```

---

### POST `/auth/forgot-password`

Sends a password reset email. Always returns 200 regardless of whether the email exists (security).

**Request body:**
```json
{ "email": "priya@example.com" }
```

**Response 200:**
```json
{ "message": "If that email exists, a reset link has been sent." }
```

---

### POST `/auth/reset-password`

**Request body:**
```json
{
  "token": "reset-token-from-email-link",
  "newPassword": "NewSecurePass456!"
}
```

**Response 200:**
```json
{ "message": "Password reset successfully." }
```

---

### GET `/auth/verify-email?token={token}`

**Response 200:**
```json
{ "message": "Email verified successfully." }
```

---

## 4. User Endpoints

**Base:** `/api/v1/users`
**Auth:** `Authorize` (all endpoints)

---

### GET `/users/me`

Returns the authenticated user's full profile.

**Response 200:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "firstName": "Priya",
  "lastName": "Sharma",
  "email": "priya@example.com",
  "role": "User",
  "countryCode": "+91",
  "mobileNumber": "9876543210",
  "fullMobile": "+919876543210",
  "countryIsoCode": "IN",
  "whatsAppOptIn": true,
  "isEmailVerified": false,
  "isActive": true,
  "createdAt": "2026-01-01T10:00:00Z"
}
```

---

### PUT `/users/me`

Updates editable profile fields. Email cannot be changed.

**Request body:**
```json
{
  "firstName": "Priya",
  "lastName": "Sharma",
  "countryCode": "+44",
  "mobileNumber": "7700900123",
  "whatsAppOptIn": false
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| firstName | string | ✅ | |
| lastName | string | ✅ | |
| countryCode | string | ❌ | Must be sent together with mobileNumber |
| mobileNumber | string | ❌ | Must be sent together with countryCode |
| whatsAppOptIn | boolean | ✅ | |

**Response 200:** Same shape as GET `/users/me`

---

### GET `/users/me/program-access`

Returns all programs the user has purchased, including current session status.

**Response 200:**
```json
[
  {
    "accessId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "orderId": "uuid",
    "programId": "uuid",
    "programName": "Break the Stress–Hormone–Health Triangle",
    "programImageUrl": "https://assets.femved.com/programs/stress-program.jpg",
    "expertId": "uuid",
    "expertName": "Dr. Prathima Nagesh",
    "durationLabel": "6 weeks",
    "status": "NotStarted",
    "startedAt": null,
    "pausedAt": null,
    "completedAt": null,
    "purchasedAt": "2026-03-01T10:00:00Z"
  }
]
```

**`status` possible values:** `NotStarted` · `Active` · `Paused` · `Completed` · `Cancelled`

---

### POST `/users/me/enrollments/{accessId}/pause`

User pauses their own active enrollment.

**Request body** *(optional)*:
```json
{ "note": "Need a break this week." }
```

**Response 200:**
```json
{ "accessId": "uuid", "action": "paused" }
```

**Errors:**
- `403` — enrollment does not belong to this user
- `404` — accessId not found
- `422` — enrollment is not currently Active

---

### POST `/users/me/enrollments/{accessId}/end`

User ends their own enrollment (marks as completed).

**Request body** *(optional)*:
```json
{ "note": "I've finished the program." }
```

**Response 200:**
```json
{ "accessId": "uuid", "action": "ended" }
```

**Errors:**
- `403` — enrollment does not belong to this user
- `422` — enrollment is already Completed or Cancelled

---

### POST `/users/me/gdpr-deletion-request`

Submits a GDPR right-to-erasure request. For GB/EU users only. No request body required.

**Response 202 Accepted:**
```json
{ "message": "Your data erasure request has been received and will be processed within 30 days." }
```

> Duplicate requests while a Pending one exists return 202 without creating a new record.

---

## 5. Guided Catalog — Public

**Base:** `/api/v1/guided`
**Auth:** All public (no token required). Cached 10 minutes.

---

### GET `/guided/tree?countryCode=GB`

Returns the complete browsable catalog tree used to build the frontend homepage and category pages.

**Query parameters:**

| Parameter | Type | Required | Notes |
|-----------|------|----------|-------|
| countryCode | string | ❌ | ISO code e.g. `GB`, `IN`, `US`. Auto-detected from JWT if logged in. Defaults to `GB`. |

**Response 200:**
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
            "categoryPageDataImage": "https://...",
            "categoryType": "Hormonal Health Support",
            "categoryHeroTitle": "Get Guided Hormonal Care",
            "categoryHeroSubtext": "Expert-led 1:1 programs...",
            "categoryCtaLabel": "Book Your Program",
            "categoryCtaTo": "/guided/hormonal-health-support",
            "whatsIncludedInCategory": [
              "Weekly 1:1 video sessions",
              "Personalised protocol"
            ],
            "categoryPageHeader": "Your hormonal health, expertly guided.",
            "categoryPageKeyAreas": [
              "PCOS & hormonal imbalance",
              "Perimenopause & menopause"
            ]
          },
          "programsInCategory": [
            {
              "programId": "uuid",
              "programName": "Break the Stress–Hormone–Health Triangle",
              "programGridDescription": "A 6-week guided program to reset your stress response.",
              "programGridImage": "https://...",
              "expertId": "uuid",
              "expertName": "Dr. Prathima Nagesh",
              "expertTitle": "Ayurvedic Physician & Women's Health Specialist",
              "expertGridDescription": "Short bio shown on program card (max 500 chars)",
              "expertDetailedDescription": "Full bio shown on program detail page",
              "expertGridImageUrl": "https://...",
              "programDurations": [
                {
                  "durationId": "uuid",
                  "durationLabel": "6 weeks",
                  "durationPrice": "£320"
                },
                {
                  "durationId": "uuid",
                  "durationLabel": "8 weeks",
                  "durationPrice": "£420"
                }
              ],
              "programPageDisplayDetails": {
                "overview": "This program helps you identify and break the cycle...",
                "whatYouGet": [
                  "6 weekly 1:1 video calls (45 min each)",
                  "Personalised Ayurvedic protocol",
                  "WhatsApp support between sessions"
                ],
                "whoIsThisFor": [
                  "Women experiencing burnout and hormonal disruption",
                  "Those struggling with sleep, mood, or energy crashes"
                ],
                "detailSections": [
                  {
                    "heading": "Reset Stress Patterns",
                    "description": "In weeks 1–2 we identify your unique stress triggers..."
                  },
                  {
                    "heading": "Rebalance Your Hormones",
                    "description": "Weeks 3–4 focus on targeted nutritional and lifestyle shifts..."
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

> **`durationPrice` format:** symbol + amount string — e.g. `"£320"`, `"$400"`, `"₹33,000"`

---

### GET `/guided/categories/{slug}?countryCode=GB`

Returns a single category with all its programs.

**Response 200:** One category object matching the shape inside `categories[]` above.

---

### GET `/guided/programs/{slug}?countryCode=GB`

Returns a single program's full detail.

**Response 200:** One program object matching the shape inside `programsInCategory[]` above.

---

## 6. Orders & Payments

**Base:** `/api/v1/orders`

---

### POST `/orders/initiate`

**Auth:** `Authorize`

Initiates a purchase. Gateway is auto-selected based on the user's country (`IN` → CashFree, all others → PayPal).

**Request body:**
```json
{
  "durationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "couponCode": "WELCOME20",
  "idempotencyKey": "client-generated-uuid"
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| durationId | uuid | ✅ | From `programDurations[].durationId` in the tree |
| couponCode | string | ❌ | Leave null if no coupon |
| idempotencyKey | uuid | ✅ | Generate on client, reuse on retry |

**Response 200 — PayPal (GB / US / AU etc.):**
```json
{
  "orderId": "uuid",
  "approvalUrl": "https://www.paypal.com/checkoutnow?token=EC-xxx",
  "amount": 320.00,
  "currency": "GBP",
  "symbol": "£",
  "gateway": "PAYPAL"
}
```

**Response 200 — CashFree (India):**
```json
{
  "orderId": "uuid",
  "gatewayOrderId": "cf_order_abc123",
  "paymentSessionId": "session_xxx",
  "amount": 33000.00,
  "currency": "INR",
  "symbol": "₹",
  "gateway": "CASHFREE"
}
```

**Payment completion flow:**
- **PayPal:** Redirect user to `approvalUrl`. After payment, PayPal fires a webhook to the server. Enrollment is created automatically — no further frontend action needed.
- **CashFree:** Pass `paymentSessionId` to the CashFree JS SDK checkout. After payment, CashFree fires a webhook. Enrollment is created automatically.

---

### GET `/orders/my`

**Auth:** `Authorize`

Returns all orders for the logged-in user.

**Response 200:**
```json
[
  {
    "orderId": "uuid",
    "programId": "uuid",
    "programName": "Break the Stress Triangle",
    "durationLabel": "6 weeks",
    "amount": 320.00,
    "currency": "GBP",
    "symbol": "£",
    "status": "Paid",
    "gateway": "PAYPAL",
    "couponCode": "WELCOME20",
    "discountAmount": 64.00,
    "createdAt": "2026-03-01T10:00:00Z"
  }
]
```

**`status` values:** `Pending` · `Paid` · `Failed` · `Refunded`

---

### GET `/orders/{orderId}`

**Auth:** `Authorize`

Returns one order. Same shape as the array item above.

---

### Webhook Endpoints (Server → Server only)

The frontend does not call these. They are called by PayPal and CashFree after payment.

```
POST /api/v1/payments/paypal/webhook
POST /api/v1/payments/cashfree/webhook
```

---

## 7. Expert Dashboard

**Base:** `/api/v1/experts`
**Auth:** `ExpertOrAdmin` (all endpoints)

---

### GET `/experts/me`

Returns the expert profile for the logged-in user.

**Response 200:**
```json
{
  "expertId": "uuid",
  "userId": "uuid",
  "displayName": "Dr. Prathima Nagesh",
  "title": "Ayurvedic Physician & Women's Health Specialist",
  "bio": "Full biography shown on program page...",
  "gridDescription": "Short bio for grid cards (max 500 chars)",
  "detailedDescription": "Longer bio for program detail page",
  "profileImageUrl": "https://...",
  "gridImageUrl": "https://...",
  "specialisations": ["Hormonal Health", "PCOS", "Stress & Burnout"],
  "yearsExperience": 12,
  "credentials": ["BAMS", "MD Ayurveda"],
  "locationCountry": "IN",
  "isActive": true
}
```

---

### GET `/experts/me/programs`

Returns a summary of all the expert's programs with enrollment counts.

**Response 200:**
```json
[
  {
    "programId": "uuid",
    "programName": "Break the Stress Triangle",
    "status": "Published",
    "totalEnrollments": 24,
    "activeEnrollments": 18
  }
]
```

**Program `status` values:** `Draft` · `PendingReview` · `Published` · `Archived`

---

### GET `/experts/me/enrollments`

Returns all enrolled users across all of the expert's programs, newest first.

**Response 200:**
```json
[
  {
    "accessId": "uuid",
    "orderId": "uuid",
    "userId": "uuid",
    "userFirstName": "Priya",
    "userLastName": "Sharma",
    "userEmail": "priya@example.com",
    "programId": "uuid",
    "programName": "Break the Stress Triangle",
    "durationLabel": "6 weeks",
    "accessStatus": "NotStarted",
    "startedAt": null,
    "pausedAt": null,
    "completedAt": null,
    "endedBy": null,
    "endedByRole": null,
    "enrolledAt": "2026-03-01T10:00:00Z"
  }
]
```

| Field | Type | Notes |
|-------|------|-------|
| accessId | uuid | Use this for all session and comment actions |
| accessStatus | string | `NotStarted` · `Active` · `Paused` · `Completed` · `Cancelled` |
| startedAt | datetime? | null until expert starts |
| pausedAt | datetime? | null unless currently paused |
| completedAt | datetime? | null until ended |
| endedBy | uuid? | User ID of whoever ended it |
| endedByRole | string? | `EXPERT` · `ADMIN` · `USER` |

---

### POST `/experts/me/enrollments/{accessId}/start`

Transitions enrollment from `NotStarted → Active`.
Sends **`session_started`** email to the enrolled user.

**Request body** *(optional — can send empty body `{}` or `null`)*:
```json
{ "note": "Welcome Priya! Your 6-week journey starts today." }
```

**Response 200:**
```json
{ "accessId": "uuid", "action": "started" }
```

**Errors:**
| Code | Reason |
|------|--------|
| 403 | Enrollment belongs to a different expert |
| 404 | accessId not found |
| 422 | Status is not `NotStarted` |

---

### POST `/experts/me/enrollments/{accessId}/pause`

Transitions enrollment from `Active → Paused`.
Sends **`session_paused`** email to the enrolled user.

**Request body** *(optional)*:
```json
{ "note": "Pausing for the holiday period." }
```

**Response 200:**
```json
{ "accessId": "uuid", "action": "paused" }
```

**Error 422:** Status is not `Active`

---

### POST `/experts/me/enrollments/{accessId}/resume`

Transitions enrollment from `Paused → Active`.
Sends **`session_resumed`** email to the enrolled user.

**Request body** *(optional)*:
```json
{ "note": "Welcome back! Picking up from week 3." }
```

**Response 200:**
```json
{ "accessId": "uuid", "action": "resumed" }
```

**Error 422:** Status is not `Paused`

---

### POST `/experts/me/enrollments/{accessId}/end`

Transitions enrollment from `Active` or `Paused → Completed`.
Sends **`session_ended`** email to the enrolled user.

**Request body** *(optional)*:
```json
{ "note": "Congratulations on completing all 6 weeks!" }
```

**Response 200:**
```json
{ "accessId": "uuid", "action": "ended" }
```

**Error 422:** Status is already `Completed` or `Cancelled`

---

### POST `/experts/me/enrollments/{accessId}/comments`

Sends a progress comment to the enrolled user.
**Always emails** the user via the **`expert_progress_update`** SendGrid template.

**Request body** *(required)*:
```json
{ "updateNote": "Great progress this week! Your sleep patterns are improving noticeably." }
```

| Field | Required | Validation |
|-------|----------|------------|
| updateNote | ✅ | Min 10 chars, max 2000 chars |

**Response 200:**
```json
{ "accessId": "uuid", "sent": true }
```

---

### GET `/experts/me/enrollments/{accessId}/comments`

Returns all comments ever sent for this enrollment, oldest first.

**Response 200:**
```json
[
  {
    "commentId": "uuid",
    "accessId": "uuid",
    "expertId": "uuid",
    "updateNote": "Great progress this week!",
    "createdAt": "2026-03-07T14:00:00Z"
  }
]
```

---

## 8. Admin Dashboard

**Base:** `/api/v1/admin`
**Auth:** `AdminOnly` (all endpoints)

---

### GET `/admin/summary`

```json
{
  "totalUsers": 142,
  "totalExperts": 8,
  "totalPrograms": 23,
  "totalOrders": 89,
  "totalRevenue": 28640.00
}
```

---

### User Management

#### GET `/admin/users`
Returns all users (non-deleted).

```json
[{
  "userId": "uuid",
  "firstName": "Priya",
  "lastName": "Sharma",
  "email": "priya@example.com",
  "role": "User",
  "countryIsoCode": "GB",
  "isActive": true,
  "isDeleted": false,
  "createdAt": "2026-01-01T10:00:00Z"
}]
```

#### PUT `/admin/users/{userId}/activate`
No request body. Response: `{ "id": "uuid", "isActive": true, "isUpdated": true }`

#### PUT `/admin/users/{userId}/deactivate`
No request body. Response: `{ "id": "uuid", "isActive": false, "isUpdated": true }`

#### DELETE `/admin/users/{userId}`
Soft delete (sets `isDeleted = true`). Response: `{ "id": "uuid", "isDeleted": true }`

#### PUT `/admin/user/change-role`
```json
// Request
{ "userId": "uuid", "roleId": 2 }
// roleId: 1 = Admin, 2 = Expert, 3 = User
```

---

### Expert Management

#### GET `/admin/experts`
Returns all expert profiles.

#### POST `/admin/experts`
Creates an expert profile linked to an existing user account.

```json
// Request
{
  "userId": "uuid",
  "displayName": "Dr. Prathima Nagesh",
  "title": "Ayurvedic Physician",
  "bio": "Full biography...",
  "gridDescription": "Short bio for program card",
  "detailedDescription": "Long bio for program detail page",
  "profileImageUrl": "https://...",
  "gridImageUrl": "https://...",
  "specialisations": ["Hormonal Health", "PCOS"],
  "yearsExperience": 12,
  "credentials": ["BAMS", "MD Ayurveda"],
  "locationCountry": "IN"
}

// Response 201: "uuid" (new expertId as plain string)
```

#### PUT `/admin/experts/{expertId}`
All fields optional. Only include fields you want to change.

```json
{
  "displayName": "Dr. Prathima Nagesh",
  "title": "Updated Title",
  "gridDescription": "New short bio"
}
```

#### PUT `/admin/experts/{expertId}/activate` / `/deactivate`
No body. Same response as user activate/deactivate.

---

### Coupon Management

#### GET `/admin/coupons`
#### POST `/admin/coupons`

```json
// Request
{
  "code": "WELCOME20",
  "discountType": "Percentage",
  "discountValue": 20.0,
  "minOrderAmount": 100.00,
  "maxUses": 500,
  "validFrom": "2026-01-01T00:00:00Z",
  "validUntil": "2026-12-31T23:59:59Z"
}
```

| Field | Notes |
|-------|-------|
| discountType | `"Percentage"` or `"Flat"` |
| discountValue | 0–100 for Percentage; flat amount for Flat |
| minOrderAmount | Optional. Null = no minimum |
| maxUses | Optional. Null = unlimited |
| validFrom / validUntil | Optional. Null = always valid |

#### PUT `/admin/coupons/{couponId}`
All fields optional + explicit clear flags:
```json
{
  "code": "NEW20",
  "discountValue": 25.0,
  "clearMaxUses": true,
  "clearValidUntil": false
}
```

#### PUT `/admin/coupons/{couponId}/deactivate`
No body.

---

### Order Management

#### GET `/admin/orders`
Returns all platform orders.

#### POST `/orders/{orderId}/refund`
**Auth:** `AdminOnly`

```json
// Request
{
  "refundAmount": 320.00,
  "reason": "User requested refund within 48 hours"
}
```

---

### Enrollment Session Management (Admin)

Admin can manage **any** enrollment on the platform, regardless of which expert owns it.

#### POST `/admin/enrollments/{accessId}/start`
#### POST `/admin/enrollments/{accessId}/pause`
#### POST `/admin/enrollments/{accessId}/resume`
#### POST `/admin/enrollments/{accessId}/end`

All have the same optional body and response as the expert equivalents:
```json
// Request (optional)
{ "note": "Admin note for this action." }

// Response 200
{ "accessId": "uuid", "action": "started" }
```

---

### Enrollment Comments (Admin)

#### POST `/admin/enrollments/{accessId}/comments`

Admin can send a comment on any enrollment. Always emails the enrolled user.

```json
// Request
{ "updateNote": "Admin message for this user." }

// Response 200
{ "accessId": "uuid", "sent": true }
```

#### GET `/admin/enrollments/{accessId}/comments`

Returns all comments for any enrollment, oldest first.

```json
[{
  "commentId": "uuid",
  "accessId": "uuid",
  "expertId": "uuid",
  "updateNote": "Great progress this week!",
  "createdAt": "2026-03-07T14:00:00Z"
}]
```

---

### Audit Log

#### GET `/admin/audit-log?limit=100`

Returns recent admin/expert mutation audit entries, newest first.

```json
[{
  "logId": "uuid",
  "performedBy": "uuid",
  "performedByEmail": "admin@femved.com",
  "action": "UpdateProgram",
  "entityType": "Program",
  "entityId": "uuid",
  "before": "{ \"name\": \"Old Name\" }",
  "after": "{ \"name\": \"New Name\" }",
  "createdAt": "2026-03-07T10:00:00Z"
}]
```

---

### GDPR Requests

#### GET `/admin/gdpr-requests?status=Pending`
#### POST `/admin/gdpr-requests/{requestId}/process`

```json
// Request
{
  "action": "Complete",
  "rejectionReason": null
}
// action: "Complete" or "Reject"
// rejectionReason required when action = "Reject"
```

---

## 9. Email Templates Reference

These emails fire automatically from the server. The frontend does not trigger them directly.

| Template Key | When Sent | Recipient | Variables |
|---|---|---|---|
| `purchase_success` | Order paid | User | `{{first_name}}`, `{{order_id}}` |
| `purchase_failed` | Payment failed | User | `{{first_name}}` |
| `expert_new_enrollment` | Order paid | Expert | `{{expert_first_name}}`, `{{order_id}}`, `{{program_id}}` |
| `session_started` | Expert/Admin starts program | User | `{{first_name}}` |
| `session_paused` | Anyone pauses program | User | `{{first_name}}` |
| `session_resumed` | Expert/Admin resumes program | User | `{{first_name}}` |
| `session_ended` | Anyone ends program | User | `{{first_name}}` |
| `expert_progress_update` | Expert/Admin sends comment | User | `{{first_name}}`, `{{update_note}}` |
| `password_reset` | Forgot password | User | Reset link in email |
| `email_verify` | After registration | User | Verify link in email |
| `program_reminder` | 24h before start date | User | `{{first_name}}` |

---

## 10. Enum Values Reference

### Enrollment / Access Status

| Value | Meaning | Next Possible States |
|-------|---------|---------------------|
| `NotStarted` | Purchase complete, expert hasn't started yet | `Active` (Expert/Admin starts) |
| `Active` | Program in progress | `Paused`, `Completed` |
| `Paused` | Temporarily on hold | `Active` (Expert/Admin resumes), `Completed` |
| `Completed` | Program ended | *(terminal — no further transitions)* |
| `Cancelled` | Refunded or admin cancelled | *(terminal)* |

### Who Can Trigger Each Action

| Action | Expert (own program) | Admin | User (own) |
|--------|---------------------|-------|------------|
| Start | ✅ | ✅ | ❌ |
| Pause | ✅ | ✅ | ✅ |
| Resume | ✅ | ✅ | ❌ |
| End | ✅ | ✅ | ✅ |

### Order Status

| Value | Meaning |
|-------|---------|
| `Pending` | Payment initiated, not yet confirmed |
| `Paid` | Payment confirmed via webhook |
| `Failed` | Payment failed |
| `Refunded` | Full or partial refund processed |

### Program Status (Content Lifecycle)

| Value | Meaning | Who Can Set |
|-------|---------|-------------|
| `Draft` | In progress by expert | Auto on create |
| `PendingReview` | Submitted for admin review | Expert (submit action) |
| `Published` | Live on the platform | Admin only |
| `Archived` | Hidden from catalog | Admin only |

### Discount Type (Coupons)

| Value | Meaning |
|-------|---------|
| `Percentage` | e.g. 20% off |
| `Flat` | e.g. £50 off |

### Payment Gateway

| Value | Used for |
|-------|---------|
| `PAYPAL` | GB, US, AU, NZ, IE, DE, FR, AE, etc. |
| `CASHFREE` | India (IN) only |

---

*Generated from FemVed API codebase — Phase 8b*
