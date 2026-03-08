# FemVed Backend API Reference

> **Base URL:** `https://api.femved.com/api/v1`
> **Auth:** JWT Bearer tokens. Include `Authorization: Bearer <accessToken>` header.
> **Response format:** All responses are JSON (camelCase). Errors use RFC 7807 ProblemDetails.
> **Swagger UI:** `https://api.femved.com/` | OpenAPI spec: `https://api.femved.com/openapi.json`

---

## Authentication

All endpoints except public ones require a JWT access token. The token contains claims:
- `sub` — user UUID
- `role` — 1 (Admin), 2 (Expert), 3 (User)
- `country_iso_code` — e.g. "GB", "IN", "US"

Access tokens expire in 15 minutes. Use the refresh endpoint to get new ones silently.

---

## 1. AUTH ENDPOINTS (`/auth`)

### POST `/auth/register` — Public
```json
// Request
{
  "email": "jane@example.com",
  "password": "SecureP@ss1",
  "firstName": "Jane",
  "lastName": "Doe",
  "countryDialCode": "+44",
  "mobileNumber": "7890001234",
  "whatsAppOptIn": true
}

// Response 201
{
  "accessToken": "eyJ...",
  "refreshToken": "uuid-string",
  "accessTokenExpiresAt": "2026-03-08T12:15:00Z",
  "user": {
    "id": "uuid",
    "email": "jane@example.com",
    "firstName": "Jane",
    "lastName": "Doe",
    "role": 3
  }
}
```

### POST `/auth/login` — Public
```json
// Request
{ "email": "jane@example.com", "password": "SecureP@ss1" }

// Response 200 — same shape as register response (AuthResponse)
```

### POST `/auth/refresh` — Public
```json
// Request
{ "refreshToken": "uuid-string" }

// Response 200 — same AuthResponse shape with new token pair
```

### POST `/auth/logout` — Authenticated
```json
// Request
{ "refreshToken": "uuid-string" }

// Response 200
{ "userId": "uuid", "isLoggedOut": true }
```

### POST `/auth/forgot-password` — Public
```json
// Request
{ "email": "jane@example.com" }

// Response 200 (always, even if email not found)
{ "message": "If that email exists, a reset link has been sent." }
```

### POST `/auth/reset-password` — Public
```json
// Request
{ "token": "reset-token-from-email", "newPassword": "NewSecure@1" }

// Response 200
{ "message": "Password has been reset." }
```

### GET `/auth/verify-email?token=jwt-token` — Public
```json
// Response 200
{ "message": "Email verified successfully." }
```

---

## 2. GUIDED CATALOG — PUBLIC (`/guided`)

### GET `/guided/tree?countryCode=GB` — Public (cached 10 min)

Returns the full catalog tree. Prices formatted for the detected/requested country.

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
            "categoryHeroSubtext": "...",
            "categoryCtaLabel": "Book Your Program",
            "categoryCtaTo": "/guided/hormonal-health-support",
            "whatsIncludedInCategory": ["item1", "item2"],
            "categoryPageHeader": "...",
            "categoryPageKeyAreas": ["area1", "area2"]
          },
          "programsInCategory": [
            {
              "programId": "uuid",
              "programName": "Break the Stress-Hormone-Health Triangle",
              "programGridDescription": "...",
              "programGridImage": "https://...",
              "expertId": "uuid",
              "expertName": "Dr. Prathima Nagesh",
              "expertTitle": "Ayurvedic Physician",
              "expertGridDescription": "Short bio (max 500)",
              "expertDetailedDescription": "Full bio (nullable)",
              "expertGridImageUrl": "https://...",
              "programDurations": [
                {
                  "durationId": "uuid",
                  "durationLabel": "6 weeks",
                  "durationPrice": "£320"
                }
              ],
              "programPageDisplayDetails": {
                "overview": "...",
                "whatYouGet": ["item1"],
                "whoIsThisFor": ["item1"],
                "detailSections": [
                  { "heading": "Reset Stress Patterns", "description": "..." }
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

### GET `/guided/categories/{slug}?countryCode=GB` — Public
Returns a single `GuidedCategoryDto` (same shape as one category from the tree, with programs nested).

### GET `/guided/programs/{slug}?countryCode=GB` — Public
Returns a single `ProgramInCategoryDto` (same shape as one program from the tree).

---

## 3. GUIDED MANAGEMENT — ExpertOrAdmin (`/guided`)

### POST `/guided/programs` — ExpertOrAdmin
```json
// Request (experts omit expertId — resolved from JWT; admins must provide it)
{
  "expertId": "uuid (admin only)",
  "categoryId": "uuid",
  "name": "Program Name (optional)",
  "slug": "program-name-slug",
  "gridDescription": "Short description (optional)",
  "gridImageUrl": "https://... (optional)",
  "overview": "Full overview (optional)",
  "sortOrder": 0,
  "durations": [
    {
      "label": "6 weeks",
      "weeks": 6,
      "sortOrder": 0,
      "prices": [
        { "locationCode": "GB", "amount": 320.00, "currencyCode": "GBP", "currencySymbol": "£" }
      ]
    }
  ],
  "whatYouGet": ["item1"],
  "whoIsThisFor": ["item1"],
  "tags": ["stress", "hormones"],
  "detailSections": [
    { "heading": "Section Title", "description": "Section body", "sortOrder": 0 }
  ]
}

// Response 201 — returns programId (Guid)
```

### PUT `/guided/programs/{id}` — ExpertOrAdmin
```json
// Request (all fields optional — only provided fields are updated)
{
  "name": "Updated Name",
  "gridDescription": "Updated",
  "gridImageUrl": "https://...",
  "overview": "Updated overview",
  "sortOrder": 1,
  "whatYouGet": ["replaces entire list"],
  "whoIsThisFor": ["replaces entire list"],
  "tags": ["replaces entire list"],
  "detailSections": [{ "heading": "New", "description": "New", "sortOrder": 0 }]
}

// Response 200
{ "id": "uuid", "isUpdated": true }
```

### POST `/guided/programs/{id}/submit` — ExpertOrAdmin
Transitions DRAFT → PENDING_REVIEW. No request body.
```json
// Response 200
{ "id": "uuid", "status": "PendingReview", "isUpdated": true }
```

### POST `/guided/programs/{id}/publish` — AdminOnly
Transitions PENDING_REVIEW → PUBLISHED. Evicts cache.
```json
// Response 200
{ "id": "uuid", "status": "Published", "isUpdated": true }
```

### POST `/guided/programs/{id}/archive` — AdminOnly
Transitions PUBLISHED → ARCHIVED. Evicts cache. Notifies enrolled users.
```json
// Response 200
{ "id": "uuid", "status": "Archived", "isUpdated": true }
```

### DELETE `/guided/programs/{id}` — ExpertOrAdmin
Soft-delete. Returns `{ "id": "uuid", "isDeleted": true }`.

---

## 4. DOMAIN & CATEGORY MANAGEMENT — AdminOnly (`/guided`)

### POST `/guided/domains` — AdminOnly
```json
{ "name": "Guided 1:1 Care", "slug": "guided-1-1-care", "sortOrder": 0 }
// Response 201 — returns domainId (Guid)
```

### PUT `/guided/domains/{id}` — AdminOnly
```json
{ "name": "Updated (optional)", "slug": "updated (optional)", "sortOrder": 1 }
// Response 200: { "id": "uuid", "isUpdated": true }
```

### DELETE `/guided/domains/{id}` — AdminOnly
Soft-delete (cascades to categories → programs). Returns `{ "id": "uuid", "isDeleted": true }`.

### POST `/guided/categories` — AdminOnly
```json
{
  "domainId": "uuid",
  "name": "Category Name",
  "slug": "category-slug",
  "categoryType": "Display Type (optional)",
  "heroTitle": "Hero Title (optional)",
  "heroSubtext": "Hero Subtext (optional)",
  "ctaLabel": "CTA Button Text (optional)",
  "ctaLink": "/guided/slug (optional)",
  "pageHeader": "Page Header (optional)",
  "imageUrl": "https://... (optional)",
  "sortOrder": 0,
  "parentId": "uuid (optional, for subcategories)",
  "whatsIncluded": ["item1"],
  "keyAreas": ["area1"]
}
// Response 201 — returns categoryId (Guid)
```

### PUT `/guided/categories/{id}` — AdminOnly
All fields optional. Returns `{ "id": "uuid", "isUpdated": true }`.

### DELETE `/guided/categories/{id}` — AdminOnly
Soft-delete (cascades to programs). Returns `{ "id": "uuid", "isDeleted": true }`.

---

## 5. DURATION & PRICE MANAGEMENT — ExpertOrAdmin (`/guided/programs/{programId}`)

### GET `/guided/programs/{programId}/durations?isActive=true` — ExpertOrAdmin
```json
[
  {
    "durationId": "uuid",
    "label": "6 weeks",
    "weeks": 6,
    "sortOrder": 0,
    "isActive": true,
    "prices": [
      {
        "priceId": "uuid",
        "locationCode": "GB",
        "amount": 320.00,
        "currencyCode": "GBP",
        "currencySymbol": "£",
        "isActive": true
      }
    ]
  }
]
```

### GET `/guided/programs/{programId}/durations/{durationId}` — ExpertOrAdmin
Returns single `DurationManagementDto` (same shape as above).

### POST `/guided/programs/{programId}/durations` — ExpertOrAdmin
```json
{
  "label": "4 weeks",
  "weeks": 4,
  "sortOrder": 1,
  "prices": [
    { "locationCode": "GB", "amount": 250.00, "currencyCode": "GBP", "currencySymbol": "£" }
  ]
}
// Response 201 — returns durationId (Guid)
```

### PUT `/guided/programs/{programId}/durations/{durationId}` — ExpertOrAdmin
```json
{ "label": "Updated (optional)", "weeks": 5, "sortOrder": 2 }
// Response 200: { "id": "uuid", "isUpdated": true }
```

### DELETE `/guided/programs/{programId}/durations/{durationId}` — ExpertOrAdmin
Deactivates (IsActive=false). Returns `{ "id": "uuid", "isDeleted": true }`.

### GET `/.../durations/{durationId}/prices?isActive=true&locationCode=GB` — ExpertOrAdmin
Returns `List<DurationPriceManagementDto>`.

### GET `/.../durations/{durationId}/prices/{priceId}` — ExpertOrAdmin
Returns single `DurationPriceManagementDto`.

### POST `/.../durations/{durationId}/prices` — ExpertOrAdmin
```json
{ "locationCode": "AU", "amount": 450.00, "currencyCode": "AUD", "currencySymbol": "A$" }
// Response 201 — returns priceId (Guid)
```

### PUT `/.../durations/{durationId}/prices/{priceId}` — ExpertOrAdmin
```json
{ "amount": 280.00, "currencyCode": "GBP (optional)", "currencySymbol": "£ (optional)" }
// Response 200: { "id": "uuid", "isUpdated": true }
```

### DELETE `/.../durations/{durationId}/prices/{priceId}` — ExpertOrAdmin
Deactivates. Returns `{ "id": "uuid", "isDeleted": true }`.

---

## 6. TESTIMONIALS — ExpertOrAdmin (`/guided/programs/{programId}/testimonials`)

### GET `/guided/programs/{programId}/testimonials` — ExpertOrAdmin
```json
[
  {
    "testimonialId": "uuid",
    "programId": "uuid",
    "reviewerName": "Sarah",
    "reviewerTitle": "Marketing Manager",
    "reviewText": "Amazing program...",
    "rating": 5,
    "isActive": true,
    "sortOrder": 0,
    "createdAt": "2026-01-15T10:00:00Z"
  }
]
```

### POST `/guided/programs/{programId}/testimonials` — ExpertOrAdmin
```json
{
  "reviewerName": "Sarah",
  "reviewerTitle": "Marketing Manager (optional)",
  "reviewText": "Amazing program...",
  "rating": 5,
  "sortOrder": 0
}
// Response 201: { "testimonialId": "uuid" }
```

### PUT `/guided/programs/{programId}/testimonials/{testimonialId}` — ExpertOrAdmin
All fields optional. Returns `{ "id": "uuid", "isUpdated": true }`.

### DELETE `/guided/programs/{programId}/testimonials/{testimonialId}` — ExpertOrAdmin
Hides (IsActive=false). Returns `{ "id": "uuid", "isDeleted": true }`.

---

## 7. ORDERS & PAYMENTS (`/orders`, `/payments`)

### POST `/orders/initiate` — Authenticated
```json
// Request
{
  "durationId": "uuid",
  "couponCode": "SAVE20 (optional)",
  "idempotencyKey": "client-generated-uuid",
  "countryCode": "GB (optional, override detection)",
  "gateway": "PAYPAL (optional, override auto-select)"
}

// Response 201 (PayPal — non-India)
{
  "orderId": "uuid",
  "gateway": "PAYPAL",
  "amount": 320.00,
  "currency": "GBP",
  "symbol": "£",
  "gatewayOrderId": "paypal-order-id",
  "paymentSessionId": null,
  "approvalUrl": "https://paypal.com/checkoutnow?token=..."
}

// Response 201 (CashFree — India)
{
  "orderId": "uuid",
  "gateway": "CASHFREE",
  "amount": 33000.00,
  "currency": "INR",
  "symbol": "₹",
  "gatewayOrderId": "cf_xxx",
  "paymentSessionId": "cashfree_session_token",
  "approvalUrl": null
}
```

**Frontend Payment Flow:**
- **PayPal:** Redirect user to `approvalUrl`. After approval, PayPal webhook notifies backend.
- **CashFree:** Use `@cashfreepayments/cashfree-js` SDK. Call `cashfree.checkout({ paymentSessionId })`. SDK handles the payment UI. Webhook notifies backend.

### GET `/orders/{id}` — Authenticated
Returns `OrderDto`:
```json
{
  "orderId": "uuid",
  "userId": "uuid",
  "durationId": "uuid",
  "amountPaid": 320.00,
  "currencyCode": "GBP",
  "locationCode": "GB",
  "discountAmount": 0,
  "status": "PAID",
  "gateway": "PAYPAL",
  "gatewayOrderId": "paypal-id",
  "failureReason": null,
  "createdAt": "2026-03-01T10:00:00Z"
}
```
Status values: `PENDING`, `PAID`, `FAILED`, `REFUNDED`, `CANCELLED`

### GET `/orders/my` — Authenticated
Returns `List<OrderDto>` (newest first).

### POST `/orders/{id}/refund` — AdminOnly
```json
{ "refundAmount": 320.00, "reason": "Customer requested" }
// Response 200: { "orderId": "uuid", "action": "Refund", "isUpdated": true }
```

### POST `/orders/{id}/cancel` — Authenticated
Cancels PENDING order. Response: `{ "orderId": "uuid", "action": "Cancel", "isUpdated": true }`

### POST `/payments/cashfree/webhook` — Public (signature verified)
### POST `/payments/paypal/webhook` — Public (signature verified)
Backend-only. Frontend doesn't call these.

---

## 8. USER DASHBOARD (`/users`)

### GET `/users/me` — Authenticated
```json
{
  "userId": "uuid",
  "email": "jane@example.com",
  "firstName": "Jane",
  "lastName": "Doe",
  "countryCode": "+44",
  "countryIsoCode": "GB",
  "mobileNumber": "7890001234",
  "fullMobile": "+447890001234",
  "whatsAppOptIn": true,
  "isEmailVerified": false,
  "createdAt": "2026-01-01T00:00:00Z"
}
```

### PUT `/users/me` — Authenticated
```json
// Request
{
  "firstName": "Jane",
  "lastName": "Doe",
  "countryCode": "+44 (optional)",
  "mobileNumber": "7890001234 (optional)",
  "whatsAppOptIn": true
}
// Response 200 — returns updated UserProfileDto
```

### GET `/users/me/program-access` — Authenticated
```json
[
  {
    "accessId": "uuid",
    "orderId": "uuid",
    "programId": "uuid",
    "programName": "Break the Stress Triangle",
    "programImageUrl": "https://...",
    "expertId": "uuid",
    "expertName": "Dr. Prathima",
    "durationLabel": "6 weeks",
    "status": "Active",
    "startedAt": "2026-02-01T00:00:00Z",
    "pausedAt": null,
    "completedAt": null,
    "purchasedAt": "2026-01-15T00:00:00Z"
  }
]
```
Status values: `NotStarted`, `Active`, `Paused`, `Completed`, `Cancelled`

### GET `/users/me/orders` — Authenticated
Same as `GET /orders/my`.

### GET `/users/me/refunds` — Authenticated
```json
[
  {
    "refundId": "uuid",
    "orderId": "uuid",
    "refundAmount": 320.00,
    "currencyCode": "GBP",
    "reason": "Customer requested",
    "status": "COMPLETED",
    "gatewayRefundId": "paypal-refund-id",
    "createdAt": "2026-03-01T00:00:00Z"
  }
]
```

### POST `/users/me/enrollments/{accessId}/pause` — Authenticated
```json
// Request (optional body)
{ "note": "Going on holiday" }
// Response 200
{ "accessId": "uuid", "action": "Paused" }
```

### POST `/users/me/enrollments/{accessId}/end` — Authenticated
```json
// Request (optional body)
{ "note": "Completed my goals" }
// Response 200
{ "accessId": "uuid", "action": "Ended" }
```

### POST `/users/me/gdpr-deletion-request` — Authenticated
```json
// Response 202
{ "message": "Your deletion request has been submitted and will be processed within 30 days." }
```

---

## 9. EXPERT DASHBOARD (`/experts`)

### GET `/experts/me` — ExpertOrAdmin
```json
{
  "expertId": "uuid",
  "userId": "uuid",
  "displayName": "Dr. Prathima Nagesh",
  "title": "Ayurvedic Physician",
  "bio": "Full bio text...",
  "gridDescription": "Short bio for cards",
  "detailedDescription": "Detailed bio for program page",
  "profileImageUrl": "https://...",
  "gridImageUrl": "https://...",
  "specialisations": ["Hormonal Health", "Stress"],
  "yearsExperience": 15,
  "credentials": ["BAMS", "MD Ayurveda"],
  "locationCountry": "IN",
  "commissionRate": 80.00,
  "isActive": true,
  "createdAt": "2026-01-01T00:00:00Z"
}
```

### PUT `/experts/me` — ExpertOrAdmin
```json
// Request (all optional; commissionRate ignored for non-admins)
{
  "displayName": "Dr. Prathima",
  "title": "Updated Title",
  "bio": "Updated bio",
  "gridDescription": "Short bio",
  "detailedDescription": "Full bio",
  "profileImageUrl": "https://...",
  "gridImageUrl": "https://...",
  "specialisations": ["Hormonal Health"],
  "yearsExperience": 16,
  "credentials": ["BAMS"],
  "locationCountry": "IN"
}
// Response 200 — returns updated ExpertProfileDto
```

### GET `/experts/me/programs` — ExpertOrAdmin
```json
[
  {
    "programId": "uuid",
    "name": "Program Name",
    "slug": "program-slug",
    "status": "Published",
    "gridImageUrl": "https://...",
    "activeEnrollments": 5,
    "totalEnrollments": 12,
    "createdAt": "2026-01-01T00:00:00Z",
    "updatedAt": "2026-02-01T00:00:00Z"
  }
]
```

### GET `/experts/me/earnings` — ExpertOrAdmin
```json
{
  "expertId": "uuid",
  "expertName": "Dr. Prathima",
  "commissionRate": 80.00,
  "totalEarned": [{ "currencyCode": "GBP", "currencySymbol": "£", "amount": 5000.00, "orderCount": 15 }],
  "expertShare": [{ "currencyCode": "GBP", "currencySymbol": "£", "amount": 4000.00, "orderCount": 15 }],
  "platformCommission": [{ "currencyCode": "GBP", "currencySymbol": "£", "amount": 1000.00, "orderCount": 15 }],
  "totalPaid": [{ "currencyCode": "GBP", "currencySymbol": "£", "amount": 3000.00, "orderCount": 0 }],
  "outstandingBalance": [{ "currencyCode": "GBP", "currencySymbol": "£", "amount": 1000.00, "orderCount": 0 }],
  "lastPayoutAt": "2026-02-15T00:00:00Z"
}
```

### GET `/experts/me/payouts` — ExpertOrAdmin
```json
[
  {
    "payoutId": "uuid",
    "expertId": "uuid",
    "expertName": "Dr. Prathima",
    "amount": 3000.00,
    "currencyCode": "GBP",
    "currencySymbol": "£",
    "paymentReference": "BANK-REF-123",
    "notes": "March payout",
    "paidBy": "uuid",
    "paidByName": "Admin User",
    "paidAt": "2026-02-15T00:00:00Z",
    "createdAt": "2026-02-15T00:00:00Z"
  }
]
```

### GET `/experts/me/enrollments` — ExpertOrAdmin
```json
[
  {
    "accessId": "uuid",
    "orderId": "uuid",
    "userId": "uuid",
    "userFirstName": "Jane",
    "userLastName": "Doe",
    "userEmail": "jane@example.com",
    "programId": "uuid",
    "programName": "Program Name",
    "durationLabel": "6 weeks",
    "accessStatus": "Active",
    "startedAt": "2026-02-01T00:00:00Z",
    "pausedAt": null,
    "completedAt": null,
    "endedBy": null,
    "endedByRole": null,
    "enrolledAt": "2026-01-15T00:00:00Z"
  }
]
```

### Session Lifecycle — ExpertOrAdmin
```
POST /experts/me/enrollments/{accessId}/start   — NOT_STARTED → ACTIVE
POST /experts/me/enrollments/{accessId}/pause   — ACTIVE → PAUSED
POST /experts/me/enrollments/{accessId}/resume  — PAUSED → ACTIVE
POST /experts/me/enrollments/{accessId}/end     — any → COMPLETED
```
All accept optional `{ "note": "reason" }`. Response: `{ "accessId": "uuid", "action": "Started|Paused|Resumed|Ended" }`.

### Comments — ExpertOrAdmin
```
POST /experts/me/enrollments/{accessId}/comments
  Request: { "updateNote": "Great progress this week!" }
  Response: { "accessId": "uuid", "sent": true }

GET /experts/me/enrollments/{accessId}/comments
  Response: [
    {
      "commentId": "uuid",
      "accessId": "uuid",
      "expertId": "uuid",
      "updateNote": "Great progress this week!",
      "createdAt": "2026-03-01T10:00:00Z"
    }
  ]
```

---

## 10. ADMIN DASHBOARD (`/admin`)

### GET `/admin/summary` — AdminOnly
```json
{
  "totalUsers": 150,
  "activeUsers": 140,
  "totalExperts": 5,
  "totalPrograms": 12,
  "publishedPrograms": 8,
  "totalOrders": 200,
  "paidOrders": 180,
  "totalRevenue": 50000.00,
  "pendingGdprRequests": 2,
  "activeCoupons": 3
}
```

### User Management — AdminOnly
```
GET    /admin/users                          → List<AdminUserDto>
PUT    /admin/users/{userId}/activate        → { id, isActive: true, isUpdated: true }
PUT    /admin/users/{userId}/deactivate      → { id, isActive: false, isUpdated: true }
DELETE /admin/users/{userId}                 → { id, isDeleted: true }
PUT    /admin/user/change-role               → body: { userId, roleId }
                                               response: { userId, roleId, isUpdated: true }
```

AdminUserDto:
```json
{
  "userId": "uuid",
  "email": "user@example.com",
  "firstName": "Jane",
  "lastName": "Doe",
  "roleId": 3,
  "roleName": "User",
  "countryIsoCode": "GB",
  "fullMobile": "+447890001234",
  "isEmailVerified": true,
  "whatsAppOptIn": true,
  "isActive": true,
  "isDeleted": false,
  "createdAt": "2026-01-01T00:00:00Z"
}
```

### Expert Management — AdminOnly
```
GET    /admin/experts                        → List<AdminExpertDto>
POST   /admin/experts                        → body: CreateExpertRequest → returns expertId (Guid)
PUT    /admin/experts/{expertId}             → body: UpdateExpertRequest → { id, isUpdated: true }
PUT    /admin/experts/{expertId}/activate    → { id, isActive: true, isUpdated: true }
PUT    /admin/experts/{expertId}/deactivate  → { id, isActive: false, isUpdated: true }
```

CreateExpertRequest:
```json
{
  "userId": "uuid (existing user account)",
  "displayName": "Dr. Prathima",
  "title": "Ayurvedic Physician",
  "bio": "Full bio...",
  "gridDescription": "Short bio (optional)",
  "detailedDescription": "Detailed bio (optional)",
  "profileImageUrl": "https://... (optional)",
  "gridImageUrl": "https://... (optional)",
  "specialisations": ["Hormonal Health"],
  "yearsExperience": 15,
  "credentials": ["BAMS"],
  "locationCountry": "IN",
  "commissionRate": 80.00
}
```

### Coupon Management — AdminOnly
```
GET    /admin/coupons                        → List<CouponDto>
POST   /admin/coupons                        → body: CreateCouponRequest → CouponDto
PUT    /admin/coupons/{couponId}             → body: UpdateCouponRequest → CouponDto
PUT    /admin/coupons/{couponId}/activate    → { id, isActive: true, isUpdated: true }
PUT    /admin/coupons/{couponId}/deactivate  → { id, isActive: false, isUpdated: true }
```

CreateCouponRequest:
```json
{
  "code": "SAVE20",
  "discountType": 0,
  "discountValue": 20.00,
  "minOrderAmount": 100.00,
  "maxUses": 50,
  "validFrom": "2026-03-01T00:00:00Z",
  "validUntil": "2026-06-01T00:00:00Z"
}
```
DiscountType: `0` = Percentage, `1` = Flat

UpdateCouponRequest supports `clearMinOrderAmount`, `clearMaxUses`, `clearValidFrom`, `clearValidUntil` boolean flags to explicitly null out those fields.

### Orders — AdminOnly
```
GET /admin/orders → List<OrderDto> (all orders, newest first)
```

### Programs — AdminOnly
```
GET /admin/programs?status=PendingReview → List<AdminProgramDto>
```
Status filter values: `Draft`, `PendingReview`, `Published`, `Archived`

### Enrollments — AdminOnly
```
GET    /admin/enrollments?status=Active                → List<EnrollmentDto>
POST   /admin/enrollments/{accessId}/start             → { accessId, action: "Started" }
POST   /admin/enrollments/{accessId}/pause             → { accessId, action: "Paused" }
POST   /admin/enrollments/{accessId}/resume            → { accessId, action: "Resumed" }
POST   /admin/enrollments/{accessId}/end               → { accessId, action: "Ended" }
POST   /admin/enrollments/{accessId}/comments          → body: { updateNote: "..." }
GET    /admin/enrollments/{accessId}/comments           → List<EnrollmentCommentDto>
```

### GDPR — AdminOnly
```
GET  /admin/gdpr-requests?status=Pending  → List<GdprRequestDto>
POST /admin/gdpr-requests/{requestId}/process
  body: { "action": "Complete" | "Reject", "rejectionReason": "optional" }
  response: { requestId, action, isUpdated: true }
```

### Analytics — AdminOnly

#### GET `/admin/analytics/sales`
```json
{
  "totalOrders": 200,
  "paidOrders": 180,
  "pendingOrders": 10,
  "failedOrders": 5,
  "refundedOrders": 5,
  "ordersWithDiscount": 30,
  "totalDiscountGiven": 2500.00,
  "revenueByCurrentcy": [
    { "currencyCode": "GBP", "currencySymbol": "£", "totalRevenue": 30000.00, "orderCount": 100, "averageOrderValue": 300.00 }
  ],
  "revenueByGateway": [
    { "gateway": "PAYPAL", "currencyCode": "GBP", "currencySymbol": "£", "totalRevenue": 30000.00, "orderCount": 100 }
  ],
  "revenueByCountry": [
    { "locationCode": "GB", "currencyCode": "GBP", "currencySymbol": "£", "totalRevenue": 30000.00, "orderCount": 100 }
  ],
  "revenueByMonth": [
    { "year": 2026, "month": 1, "monthLabel": "Jan 2026", "currencyCode": "GBP", "currencySymbol": "£", "totalRevenue": 5000.00, "orderCount": 15 }
  ]
}
```

#### GET `/admin/analytics/programs`
```json
{
  "programs": [
    {
      "programId": "uuid",
      "programName": "Program Name",
      "expertName": "Dr. Prathima",
      "categoryName": "Hormonal Health",
      "status": "Published",
      "totalSales": 50,
      "activeEnrollments": 10,
      "completedEnrollments": 35,
      "totalEnrollments": 50,
      "revenue": [{ "currencyCode": "GBP", "currencySymbol": "£", "amount": 15000.00, "orderCount": 50 }]
    }
  ],
  "experts": [
    {
      "expertId": "uuid",
      "expertName": "Dr. Prathima",
      "commissionRate": 80.00,
      "totalSales": 100,
      "totalEnrollments": 100,
      "activeEnrollments": 20,
      "totalRevenue": [{ "currencyCode": "GBP", "currencySymbol": "£", "amount": 30000.00, "orderCount": 100 }],
      "expertShare": [{ "currencyCode": "GBP", "currencySymbol": "£", "amount": 24000.00, "orderCount": 100 }],
      "platformRevenue": [{ "currencyCode": "GBP", "currencySymbol": "£", "amount": 6000.00, "orderCount": 100 }]
    }
  ]
}
```

#### GET `/admin/analytics/users`
```json
{
  "totalRegistered": 150,
  "totalBuyers": 80,
  "repeatBuyers": 20,
  "repeatRatio": 0.25,
  "conversionRate": 0.53,
  "newUsersByMonth": [
    { "year": 2026, "month": 1, "monthLabel": "Jan 2026", "newUsers": 30, "newBuyers": 15 }
  ],
  "cohorts": [
    {
      "year": 2026, "month": 1, "monthLabel": "Jan 2026",
      "usersRegistered": 30,
      "purchasedWithin30Days": 10,
      "purchasedWithin60Days": 15,
      "purchasedWithin90Days": 18,
      "rate30Days": 0.33
    }
  ]
}
```

#### GET `/admin/analytics/expert-payouts`
Returns `List<ExpertPayoutBalanceDto>` for all experts.

### Expert Payouts — AdminOnly
```
GET  /admin/expert-payouts/{expertId}/balance  → ExpertPayoutBalanceDto
GET  /admin/expert-payouts/{expertId}          → List<ExpertPayoutRecordDto>
POST /admin/expert-payouts                     → body: RecordExpertPayoutRequest → ExpertPayoutRecordDto
```

RecordExpertPayoutRequest:
```json
{
  "expertId": "uuid",
  "amount": 3000.00,
  "currencyCode": "GBP",
  "paidAt": "2026-03-01T00:00:00Z",
  "paymentReference": "BANK-REF-123 (optional)",
  "notes": "March payout (optional)"
}
```

### Audit Log — AdminOnly
```
GET /admin/audit-log?limit=100 → List<AuditLogDto>
```
```json
[
  {
    "logId": "uuid",
    "adminUserId": "uuid",
    "adminEmail": "admin@femved.com",
    "action": "UpdateExpert",
    "entityType": "Expert",
    "entityId": "uuid",
    "beforeValue": "{json snapshot}",
    "afterValue": "{json snapshot}",
    "ipAddress": "1.2.3.4",
    "createdAt": "2026-03-01T10:00:00Z"
  }
]
```

---

## Error Response Format (All Endpoints)

```json
{
  "type": "https://femved.com/errors/validation-error",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/auth/register",
  "errors": {
    "email": ["Email is required."],
    "password": ["Password must be at least 8 characters."]
  }
}
```

Error types:
- `400` — Validation errors (field-level in `errors` object)
- `401` — Unauthorized (token expired/invalid)
- `403` — Forbidden (insufficient role)
- `404` — Not found
- `422` — Domain error (business rule violation)
- `500` — Internal server error

---

## User Roles

| roleId | Name | Dashboard Access |
|--------|------|-----------------|
| 1 | Admin | Full admin dashboard + all expert features |
| 2 | Expert | Expert dashboard + own programs/enrollments |
| 3 | User | User dashboard + catalog browsing + purchases |

---

## Country & Payment Gateway Mapping

| Country ISO | Currency | Symbol | Payment Gateway |
|-------------|----------|--------|-----------------|
| IN | INR | ₹ | CashFree |
| GB | GBP | £ | PayPal |
| US | USD | $ | PayPal |
| AU | AUD | A$ | PayPal |
| AE | AED | د.إ | PayPal |

---

## Program Status Flow

```
DRAFT → PENDING_REVIEW → PUBLISHED → ARCHIVED
```

- Expert creates as DRAFT
- Expert submits → PENDING_REVIEW
- Admin publishes → PUBLISHED (visible in catalog)
- Admin archives → ARCHIVED (hidden, enrollees notified)

## Enrollment Status Flow

```
NOT_STARTED → ACTIVE → PAUSED → COMPLETED
                └──────────────→ COMPLETED
```

- Purchase creates as NOT_STARTED
- Expert/Admin starts → ACTIVE
- Expert/Admin/User pauses → PAUSED
- Expert/Admin resumes → ACTIVE
- Expert/Admin/User ends → COMPLETED
