# Wave 1 — The Absolute Foundations

> **Sources:** `Agents/agent-context.md` §Wave 1 (Endpoints) + `Agents/guide.md` §1 (Provider), §3 (Categories), §12 (Notifications)
> **Dependencies:** None — can be built immediately
> **Focus:** Auth, Categories, Languages, Roles, Specializations, Tags, Users, Base Profile

---

## 1. Current Status

| Area | Built | Missing |
|---|---|---|
| Auth Credential | 6/6 ✅ | — |
| Auth Sessions | 9/9 ✅ | — |
| Auth Registration | 1/1 ✅ | — |
| Auth External (OAuth) | 1 generic ⚠️ | 3 explicit (`/external/apple`, `/external/facebook`, `/external/google`) |
| Categories | All ✅ | — |
| Languages | All ✅ | — |
| Roles | 6/7 ⚠️ | `GET /roles/{id}/claims`, `DELETE /roles/{id}` (currently `PATCH /deactivate`) |
| Specializations | All ✅ | — |
| Tags | All ✅ | — |
| Users | 7/9 ⚠️ | `DELETE /users/{id}/roles/{roleName}` (currently GUID), `PUT /users/{id}/status` (currently split) |
| Profile | 1/2 ⚠️ | Profile module exists but in **Accounts**, not exposed under `/profile` per PDF — verify route group |

**Total Wave 1 missing:** ~7 endpoints (~8 hours)

---

## 2. Missing Endpoints — Detail

### 2.1 OAuth Split (3 endpoints, 4h)

**Currently:** Single `POST /api/v1/auth/external` accepts a `Provider` field (Apple/Facebook/Google).

**PDF1 spec:** Three separate endpoints:
- `POST /external/apple` — provider-specific request shape (Apple identity token + nonce)
- `POST /external/facebook` — provider-specific request shape (Facebook access token)
- `POST /external/google` — provider-specific request shape (Google ID token)

**Implementation:**
- Add 3 thin routes in `Auth.Presentation/Endpoints/ExternalProviderEndpoints.cs` that delegate to the existing `ExternalSignInCommand` with `Provider = "Apple|Facebook|Google"` hardcoded.
- Add provider-specific request DTOs (`AppleExternalSignInRequest`, etc.) for OpenAPI clarity.
- Keep the generic endpoint as backward-compat OR deprecate (decide with team).
- All `.AllowAnonymous()` per PDF.

### 2.2 Roles cleanup (2 endpoints, 2h)

**`GET /api/v1/admin/roles/{id}/claims`** — list claims for a role
- Add to `Security.Presentation/Endpoints/RoleEndpoints.cs`
- Query handler in `Security.Application/Queries/GetRoleClaims/`
- Returns `IReadOnlyList<RoleClaimDto(Type, Value)>`
- `MustHavePermission(Role, Read)` + admin

**`DELETE /api/v1/admin/roles/{id}`** — hard delete role
- Currently exists as `PATCH /roles/{id}/deactivate` — rename/add DELETE alias
- Reserved roles (Admin, User, Provider, SuperAdmin) cannot be deleted (already enforced)
- `MustHavePermission(Role, Delete)`

### 2.3 User role/status fixes (1h)

- `DELETE /api/v1/admin/users/{id}/roles/{roleName}` — currently uses `{roleId:guid}`, change route segment to `{roleName}` and look up role server-side. Add backward-compat with both.
- `PUT /api/v1/admin/users/{id}/status` — currently `POST /activate` + `POST /deactivate`. Add unified PUT that accepts `{ isActive: bool }` and dispatches the same internal commands.

### 2.4 Profile (verify, ~0h)

PDF1 lists `GET /profile` under Wave 1. We have `Accounts.Presentation/Endpoints/Profile/` with GET/PUT. Confirm route group is mounted at `/api/v1/profile` (not `/api/v1/accounts/profile`) — if not, add alias.

---

## 3. Business Rules from `guide.md`

### 3.1 Auth (Section 1.x — Auth/OTP/Sessions)

- **OTP:** 6-digit, 10-min TTL, max 5 attempts, rate limit 5/hour (Register, Verify-Email, Resend-OTP, Forgot/Reset-Password) ✅ already enforced
- **Sessions:** 30-day expiry, JWT 15-min, refresh token rotation ✅
- **Refresh token reuse detection:** revoke ALL sessions on reuse (security breach) ✅
- **Password strength:** min 8 chars + uppercase + lowercase + digit + special ✅
- **Email normalization:** `Trim().ToLowerInvariant()` ✅
- **Login:** Create Device + Session records; track last login, IP from `IRequestContext` ✅

### 3.2 Categories (guide §3)

- **Hierarchy:** max 3 nesting levels (Root → Sub → Sub-Sub). Enforced at create. ✅
- **Uniqueness:** name unique within parent ✅
- **Soft-delete:** cannot delete with active tours/businesses linked ✅
- **Translations:** AR + EN minimum via `CategoryTranslations` ✅
- **Slug:** auto-generated from name (transliteration AR→Latin), unique per level ✅
- **Sort order:** SortOrder field controls display ✅

### 3.3 Roles & Claims (PDF1 Wave 1)

- **Reserved names:** Admin, User, Provider, SuperAdmin — cannot create or delete ✅
- **Claim types:** `permission`, `feature`, `tier` for feature gating ✅
- **Provider role assignment:** triggers provider profile setup flow (Wave 2 onboarding) ✅
- **Removing Provider role:** verify no active bookings or pending payouts first

### 3.4 Users (PDF1 Wave 1)

- **List filters:** email (partial), role, status (active/inactive/suspended), registration date range
- **Pagination:** page + pageSize (max 50), sort by name/email/createdAt
- **Status change:** revoke all sessions on deactivate; cannot self-deactivate (prevent admin lockout); reactivation requires re-verify email if previously suspended for violations
- **Status event:** `UserStatusChangedIntegrationEvent` → other modules react (Booking cancels pending, etc.)

---

## 4. WBS

| # | Step | Hours | Owner |
|---|---|---|---|
| 1 | OAuth 3 explicit endpoints + DTOs | 4 | Backend Dev A |
| 2 | `GET /roles/{id}/claims` query + handler + tests | 2 | Backend Dev B |
| 3 | `DELETE /roles/{id}` permission + route | 1 | Backend Dev B |
| 4 | `DELETE /users/{id}/roles/{roleName}` alias | 0.5 | Backend Dev A |
| 5 | `PUT /users/{id}/status` unified | 0.5 | Backend Dev A |
| 6 | Verify `/profile` route mount | 0.5 | Backend Dev A |
| 7 | Build + integration tests (4+) | 1.5 | Both |
| **Total** | | **~10h** | |

---

## 5. Acceptance Criteria

- [ ] All 7 missing endpoints respond per PDF1 spec
- [ ] All endpoints use `MustHavePermissionAttribute` (no bare `.RequireAuthorization()` — see Auth-Cleanup sprint rules)
- [ ] OAuth endpoints return 200/201 with token bundle matching existing login response shape
- [ ] Integration tests: 4 new tests (1 per OAuth provider + claims list + role delete + status unified)
- [ ] OpenAPI / Swagger reflects new routes correctly
- [ ] No regressions in existing Wave 1 tests
- [ ] `dotnet build` green for Auth.Presentation, Security.Presentation, Accounts.Presentation, YallaJo.Api
