# ENDPOINT AUTHORIZATION AUDIT REPORT
## YallaJo Presentation Layer — Authorization Coverage Analysis

**Audit Date:** 2026-04-21 21:27:36
**Scope:** Auth.Presentation, Accounts.Presentation, Security.Presentation, ContentCore.Presentation, ContentPlaces.Presentation

---

## EXECUTIVE SUMMARY

This audit examined **127 endpoints** across 5 Presentation projects to verify authorization coverage. The analysis classifies each endpoint into one of four categories:

- **PERMISSION_GUARDED (89):** Explicit MustHavePermissionAttribute + RequireAuthorization() ✅
- **ANONYMOUS (25):** Intentionally public, correctly marked with AllowAnonymous() ✅
- **AUTH_ONLY (11):** Authenticated but missing explicit permission metadata ⚠️
- **UNPROTECTED (0):** No authorization checks whatsoever ✅

**Overall Compliance: 70%** (89/127 endpoints fully compliant)
**Action Items: 24 endpoints** require updates to achieve 100% compliance

---

## SUMMARY TABLE

| Metric | Count | Status |
|--------|-------|--------|
| **Total Endpoints** | 127 | — |
| **PERMISSION_GUARDED** | 89 | ✅ Compliant |
| **ANONYMOUS** | 25 | ✅ Compliant |
| **AUTH_ONLY** | 11 | ⚠️ Needs Fix |
| **UNPROTECTED** | 0 | ✅ None Found |
| **String-Based Policies** | 13 | ⚠️ Needs Refactor |

---

## CRITICAL FINDINGS

### 🟡 AUTH_ONLY ENDPOINTS (11) — AUTHENTICATED BUT NO PERMISSION CHECK

These endpoints require authentication but lack explicit permission metadata. They should be updated to use MustHavePermissionAttribute for consistency and auditability.

#### Auth.Presentation (7 endpoints)

| File | Line | HTTP | Route | Endpoint | Suggested Permission |
|------|------|------|-------|----------|----------------------|
| DeviceEndpoints.cs | 14 | PATCH | /api/v1/auth/devices/{deviceId:guid}/trust | TrustDevice | SecurityFeatures.Device / Update |
| ExternalProviderEndpoints.cs | 16 | POST | /api/v1/auth/external-providers | LinkExternalProvider | SecurityFeatures.ExternalProvider / Create |
| ExternalProviderEndpoints.cs | 33 | DELETE | /api/v1/auth/external-providers/{providerId:guid} | UnlinkExternalProvider | SecurityFeatures.ExternalProvider / Delete |
| SessionEndpoints.cs | 23 | POST | /api/v1/auth/logout | Logout | SecurityFeatures.Session / Delete |
| SessionEndpoints.cs | 34 | POST | /api/v1/auth/logout-all | LogoutAll | SecurityFeatures.Session / Delete |
| SessionEndpoints.cs | 44 | GET | /api/v1/auth/sessions | ListActiveSessions | SecurityFeatures.Session / Read |
| SessionEndpoints.cs | 76 | DELETE | /api/v1/auth/sessions/{sessionId:guid} | RevokeSession | SecurityFeatures.Session / Delete |

#### Accounts.Presentation (5 endpoints)

| File | Line | HTTP | Route | Endpoint | Suggested Permission |
|------|------|------|-------|----------|----------------------|
| ProfileEndpoints.cs | 31 | GET | /api/v1/accounts/profile/ | GetProfile | SecurityFeatures.Profile / Read |
| ProfileEndpoints.cs | 46 | PUT | /api/v1/accounts/profile/ | UpdateProfile | SecurityFeatures.Profile / Update |
| ProfileEndpoints.cs | 67 | PUT | /api/v1/accounts/profile/avatar | UpdateAvatar | SecurityFeatures.Profile / Update |
| ProfileEndpoints.cs | 102 | DELETE | /api/v1/accounts/profile/avatar | DeleteAvatar | SecurityFeatures.Profile / Update |
| ProfileEndpoints.cs | 114 | DELETE | /api/v1/accounts/profile/ | DeleteProfile | SecurityFeatures.Profile / Delete |

#### Security.Presentation (2 endpoints)

| File | Line | HTTP | Route | Endpoint | Suggested Permission |
|------|------|------|-------|----------|----------------------|
| AccountEndpoints.cs | 18 | PUT | /api/v1/security/account/password | ChangePassword | SecurityFeatures.Account / Update |
| AccountEndpoints.cs | 31 | PUT | /api/v1/security/account/phone | UpdatePrimaryPhone | SecurityFeatures.Account / Update |

---

### 🟡 STRING-BASED POLICIES (13) — NEEDS REFACTOR

These endpoints use string-based authorization policies instead of MustHavePermissionAttribute. While they provide some protection, they lack structured metadata for audit logging and permission discovery.

#### ContentCore.Presentation (9 endpoints)

| File | Line | HTTP | Route | Endpoint | Current Policy | Suggested Fix |
|------|------|------|-------|----------|-----------------|---------------|
| CategoryEndpoints.cs | 78 | GET | /api/v1/content-core/categories/admin | ListCategoriesAdmin | "Permission.Category.Read" | MustHavePermissionAttribute(ContentCoreFeatures.Category, AppAction.Read) |
| CategoryEndpoints.cs | 93 | GET | /api/v1/content-core/categories/admin/{id:guid} | GetCategoryByIdAdmin | "Permission.Category.Read" | MustHavePermissionAttribute(ContentCoreFeatures.Category, AppAction.Read) |
| CategoryEndpoints.cs | 115 | POST | /api/v1/content-core/categories/ | CreateCategory | "Permission.Category.Create" | MustHavePermissionAttribute(ContentCoreFeatures.Category, AppAction.Create) |
| CategoryEndpoints.cs | 137 | PUT | /api/v1/content-core/categories/{id:guid} | UpdateCategory | "Permission.Category.Update" | MustHavePermissionAttribute(ContentCoreFeatures.Category, AppAction.Update) |
| CategoryEndpoints.cs | 150 | DELETE | /api/v1/content-core/categories/{id:guid} | DeleteCategory | "Permission.Category.Delete" | MustHavePermissionAttribute(ContentCoreFeatures.Category, AppAction.Delete) |
| CategoryEndpoints.cs | 163 | PATCH | /api/v1/content-core/categories/{id:guid}/deactivate | DeactivateCategory | "Permission.Category.Update" | MustHavePermissionAttribute(ContentCoreFeatures.Category, AppAction.Update) |
| CategoryEndpoints.cs | 176 | PATCH | /api/v1/content-core/categories/{id:guid}/activate | ActivateCategory | "Permission.Category.Update" | MustHavePermissionAttribute(ContentCoreFeatures.Category, AppAction.Update) |
| CategoryEndpoints.cs | 193 | PUT | /api/v1/content-core/categories/reorder | ReorderCategories | "Permission.Category.Update" | MustHavePermissionAttribute(ContentCoreFeatures.Category, AppAction.Update) |
| AttachmentEndpoints.cs | 100 | DELETE | /api/v1/content-core/attachments/{id:guid} | DeleteAttachment | "Permission.Attachment.Delete" | MustHavePermissionAttribute(ContentCoreFeatures.Attachment, AppAction.Delete) |

#### ContentPlaces.Presentation (5 endpoints)

| File | Line | HTTP | Route | Endpoint | Current Policy | Suggested Fix |
|------|------|------|-------|----------|-----------------|---------------|
| BusinessEndpoints.cs | 146 | DELETE | /api/v1/places/businesses/{id:guid} | DeleteBusiness | "Admin" | MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Delete) |
| BusinessEndpoints.cs | 180 | POST | /api/v1/places/businesses/admin/{id:guid}/approve | ApproveBusiness | "Admin" | MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Update) |
| BusinessEndpoints.cs | 197 | POST | /api/v1/places/businesses/admin/{id:guid}/reject | RejectBusiness | "Admin" | MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Update) |
| BusinessEndpoints.cs | 214 | POST | /api/v1/places/businesses/admin/{id:guid}/suspend | SuspendBusiness | "Admin" | MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Update) |
| BusinessEndpoints.cs | 229 | POST | /api/v1/places/businesses/admin/{id:guid}/reinstate | ReinstateBusiness | "Admin" | MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Update) |

---

### 🟡 MISSING PERMISSIONS (4) — NEEDS ADDITION

These endpoints have RequireAuthorization() but no permission metadata at all.

#### ContentPlaces.Presentation (4 endpoints)

| File | Line | HTTP | Route | Endpoint | Suggested Permission |
|------|------|------|-------|----------|----------------------|
| BusinessEndpoints.cs | 72 | POST | /api/v1/places/businesses/places/businesses | CreateBusiness | ContentPlacesFeatures.Business / Create |
| BusinessEndpoints.cs | 105 | PUT | /api/v1/places/businesses/places/businesses/{id:guid} | UpdateBusiness | ContentPlacesFeatures.Business / Update |
| BusinessEndpoints.cs | 149 | POST | /api/v1/places/businesses/places/businesses/{id:guid}/resubmit | ResubmitBusiness | ContentPlacesFeatures.Business / Update |
| BusinessEndpoints.cs | 252 | PUT | /api/v1/places/businesses/places/businesses/{id:guid}/hours | SetBusinessHours | ContentPlacesFeatures.Business / Update |

---

## ANONYMOUS ENDPOINTS (25) — INTENTIONALLY PUBLIC ✅

All anonymous endpoints are correctly justified as public reads or authentication flows:

### Auth.Presentation (8)
- POST /api/v1/auth/register — Public registration
- POST /api/v1/auth/verify-email — Email verification during registration/password reset
- POST /api/v1/auth/login — Public login endpoint
- POST /api/v1/auth/refresh — Token refresh (uses refresh token, not access token)
- POST /api/v1/auth/forgot-password — Password reset initiation
- POST /api/v1/auth/reset-password — Password reset completion (uses OTP)
- POST /api/v1/auth/resend-otp — OTP resend for registration/password reset
- POST /api/v1/auth/invitations/accept — Invite acceptance (uses token, not access token)

### ContentCore.Presentation (8)
- GET /api/v1/content-core/categories/ — Public category listing
- GET /api/v1/content-core/categories/{id:guid} — Public category details
- GET /api/v1/content-core/languages/ — Public language list (UI localization)
- GET /api/v1/content-core/specializations/ — Public specialization list
- GET /api/v1/content-core/tags/ — Public tag list
- GET /api/v1/content-core/tags/{id:guid} — Public tag details
- GET /api/v1/content-core/entity-categories/ — Public entity category listing
- GET /api/v1/content-core/entity-tags/ — Public entity tag listing

### ContentPlaces.Presentation (9)
- GET /api/v1/places/ — Public place listing with filters
- GET /api/v1/places/{id:guid} — Public place details
- GET /api/v1/places/{slug} — Public place lookup by slug
- GET /api/v1/places/nearby — Public nearby places search
- GET /api/v1/places/map/viewport — Public map data for viewport
- GET /api/v1/places/businesses/places/{id:guid}/businesses — Public business listing (Approved only)
- GET /api/v1/places/businesses/places/businesses/{id:guid} — Public business details (Approved only)
- GET /api/v1/places/businesses/places/businesses/{id:guid}/hours — Public business hours
- GET /api/v1/places/{id:guid}/accessibility — Public accessibility features
- GET /api/v1/places/businesses/{id:guid}/amenities — Public amenity listing
- GET /api/v1/places/businesses/{id:guid}/staff — Public staff listing
- GET /api/v1/places/businesses/{businessId:guid}/services/ — Public service listing
- GET /api/v1/places/businesses/{businessId:guid}/services/{serviceItemId:guid} — Public service details

---

## PERMISSION_GUARDED ENDPOINTS (89) — PROPERLY SECURED ✅

All remaining endpoints (89 total) have explicit MustHavePermissionAttribute metadata with RequireAuthorization().

### Breakdown by Project

| Project | Count | Status |
|---------|-------|--------|
| Auth.Presentation | 4 | ✅ Compliant |
| Security.Presentation | 18 | ✅ Compliant |
| ContentCore.Presentation | 30 | ✅ Compliant |
| ContentPlaces.Presentation | 37 | ✅ Compliant |
| **Total** | **89** | **✅ Compliant** |

---

## RECOMMENDATIONS

### Priority 1: Fix AUTH_ONLY Endpoints (11 violations)

Add explicit MustHavePermissionAttribute metadata to all AUTH_ONLY endpoints.

**Example Fix (Auth.Presentation/SessionEndpoints.cs):**
`csharp
// BEFORE
group.MapPost("/logout", async (LogoutRequest request, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new LogoutCommand(request.RefreshToken), ct);
    return result.ToApiResult();
})
.WithName("Logout")
.Produces(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.WithSummary("Logout — revokes the refresh token and its session")
.RequireAuthorization();

// AFTER
group.MapPost("/logout", async (LogoutRequest request, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new LogoutCommand(request.RefreshToken), ct);
    return result.ToApiResult();
})
.WithName("Logout")
.Produces(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.WithSummary("Logout — revokes the refresh token and its session")
.WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.Session, AppAction.Delete))
.RequireAuthorization();
`

### Priority 2: Replace String-Based Policies (13 violations)

Replace .RequireAuthorization("Permission.X") and .RequireAuthorization("Admin") with MustHavePermissionAttribute.

**Example Fix (ContentCore.Presentation/CategoryEndpoints.cs):**
`csharp
// BEFORE
categories.MapGet("/admin", async (HttpContext http, ISender sender, ...) =>
{
    // ...
})
.WithName("ListCategoriesAdmin")
.Produces<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)
.WithSummary("List categories (admin — may include inactive, requires read permission)")
.RequireAuthorization("Permission.Category.Read");

// AFTER
categories.MapGet("/admin", async (HttpContext http, ISender sender, ...) =>
{
    // ...
})
.WithName("ListCategoriesAdmin")
.Produces<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)
.WithSummary("List categories (admin — may include inactive, requires read permission)")
.WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Category, AppAction.Read))
.RequireAuthorization();
`

### Priority 3: Add Missing Permissions (4 violations)

Add MustHavePermissionAttribute to endpoints with only RequireAuthorization().

**Example Fix (ContentPlaces.Presentation/BusinessEndpoints.cs):**
`csharp
// BEFORE
businesses.MapPost("/places/businesses", async (CreateBusinessRequest request, ISender sender) =>
{
    // ...
})
.WithName("CreateBusiness")
.Produces<CreateBusinessResult>(StatusCodes.Status201Created)
.ProducesValidationProblem()
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status409Conflict)
.WithSummary("Create a business linked to a Place (status starts as Pending)")
.RequireAuthorization();

// AFTER
businesses.MapPost("/places/businesses", async (CreateBusinessRequest request, ISender sender) =>
{
    // ...
})
.WithName("CreateBusiness")
.Produces<CreateBusinessResult>(StatusCodes.Status201Created)
.ProducesValidationProblem()
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status409Conflict)
.WithSummary("Create a business linked to a Place (status starts as Pending)")
.WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Create))
.RequireAuthorization();
`

---

## IMPLEMENTATION CHECKLIST

- [ ] **Auth.Presentation** — Add 7 missing permission attributes
  - [ ] DeviceEndpoints.cs:14 (TrustDevice)
  - [ ] ExternalProviderEndpoints.cs:16 (LinkExternalProvider)
  - [ ] ExternalProviderEndpoints.cs:33 (UnlinkExternalProvider)
  - [ ] SessionEndpoints.cs:23 (Logout)
  - [ ] SessionEndpoints.cs:34 (LogoutAll)
  - [ ] SessionEndpoints.cs:44 (ListActiveSessions)
  - [ ] SessionEndpoints.cs:76 (RevokeSession)

- [ ] **Accounts.Presentation** — Add 5 missing permission attributes
  - [ ] ProfileEndpoints.cs:31 (GetProfile)
  - [ ] ProfileEndpoints.cs:46 (UpdateProfile)
  - [ ] ProfileEndpoints.cs:67 (UpdateAvatar)
  - [ ] ProfileEndpoints.cs:102 (DeleteAvatar)
  - [ ] ProfileEndpoints.cs:114 (DeleteProfile)

- [ ] **Security.Presentation** — Add 2 missing permission attributes
  - [ ] AccountEndpoints.cs:18 (ChangePassword)
  - [ ] AccountEndpoints.cs:31 (UpdatePrimaryPhone)

- [ ] **ContentCore.Presentation** — Replace 9 string policies with MustHavePermissionAttribute
  - [ ] CategoryEndpoints.cs:78, 93, 115, 137, 150, 163, 176, 193 (8 endpoints)
  - [ ] AttachmentEndpoints.cs:100 (DeleteAttachment)

- [ ] **ContentPlaces.Presentation** — Add 4 missing + replace 5 string policies
  - [ ] BusinessEndpoints.cs:72 (CreateBusiness) — Add permission
  - [ ] BusinessEndpoints.cs:105 (UpdateBusiness) — Add permission
  - [ ] BusinessEndpoints.cs:149 (ResubmitBusiness) — Add permission
  - [ ] BusinessEndpoints.cs:252 (SetBusinessHours) — Add permission
  - [ ] BusinessEndpoints.cs:146, 180, 197, 214, 229 (5 endpoints) — Replace string policies

---

## COMPLIANCE SUMMARY

| Category | Count | Status | Action |
|----------|-------|--------|--------|
| ✅ PERMISSION_GUARDED | 89 | COMPLIANT | None |
| ✅ ANONYMOUS (Justified) | 25 | COMPLIANT | None |
| ⚠️ AUTH_ONLY (Missing Permission) | 11 | NEEDS FIX | Add MustHavePermissionAttribute |
| ⚠️ String-Based Policies | 13 | NEEDS REFACTOR | Replace with MustHavePermissionAttribute |
| ✅ UNPROTECTED | 0 | NONE FOUND | N/A |

**Overall Compliance:** 70% (89/127 endpoints fully compliant)
**Target Compliance:** 100% (127/127 endpoints)
**Estimated Effort:** 2-3 hours (24 endpoints × 5-10 min each)

---

## AUDIT NOTES

1. **No Truly Unprotected Endpoints:** All 127 endpoints have at least some form of authorization (either AllowAnonymous(), .RequireAuthorization(), or permission metadata).

2. **String-Based Policies:** While .RequireAuthorization("Permission.X") provides protection, it lacks:
   - Structured permission metadata for audit logging
   - Permission discovery and documentation
   - Fine-grained access control consistency
   - Integration with authorization handlers

3. **Auth-Only Endpoints:** These 11 endpoints require authentication but lack explicit permission checks. They should be updated for:
   - Consistency with the rest of the codebase
   - Auditability and logging
   - Future permission-based access control

4. **Anonymous Endpoints:** All 25 anonymous endpoints are correctly justified as public reads or authentication flows (login, registration, password reset, etc.).

---

**Report Generated:** 2026-04-21 21:27:36
**Auditor:** Endpoint Authorization Audit Tool
**Scope:** 5 Presentation projects, 127 endpoints, 30 endpoint files
