# Playwright MCP Test Scenarios — Role System

> **API-ONLY MODE.** This checkout has no `YallaJo.Web`. Every `browser_navigate("https://localhost:57065/swagger…")` step below is a docking step; the actual request runs through `window.__yj.apiFetch(...)` defined in [`Playwright-APIOnly-Adapter.md`](./Playwright-APIOnly-Adapter.md). Read that adapter once at the start of every Playwright session — it also lists the 8 seeded test users and their credentials.

## Source Plans

Sources read in full:

- `Agents/Plans/Role-System.md`
- `Agents/Plans/Role-System-Audit-Report.md`
- `Agents/Plans/Role-System-FixPlan.md`

References used:

- `Agents/Plans/Master-RoadmapTo10.md` — Role-System is low remaining gap; target `9.5 → 10.0`, main ask is role-permission integration tests.
- `Agents/Plans/CrossDocumentAnalysisReport.md` — systemic issue: ~110 redundant `ICurrentUser` checks across 7 modules; authorization should be policy/permission based.
- Code sampled: `Security.Contracts/Authorization/AppRoles.cs`, `Security.Infrastructure/Seeding/RolePermissionMapping.cs`, `Security.Presentation/SecurityEndpoints.cs`, `Security.Presentation/Endpoints/User/UserEndpoints.cs`, `Security.Presentation/Endpoints/Role/RoleEndpoints.cs`, role assignment handlers under `Security.Infrastructure/EventHandlers/`, Accounts provider/agency integration events.

Execution note: Web URL is `https://localhost:57065/swagger`; API URL is `https://localhost:57065`. App is currently not running because of a SQL bug. Prefer UI role-management pages if present; otherwise use `browser_evaluate` authenticated API calls under `/api/v1/security/*`. Use `browser_network_requests` to capture status codes for protected endpoint probes.

## 0. Prerequisites (roles seeded: Administrator, ContentAdmin, FinancialAdmin, SupportAdmin, ProviderApprover, etc.)

1. SQL startup bug fixed and both hosts running:
   - Web: `https://localhost:57065/swagger`
   - API: `https://localhost:57065`
2. Recaptcha disabled for seeded users.
3. Seed users exist, password `TestPass!23`:
   - `admin@yallajo.test` — Administrator/all permissions.
   - `userA@yallajo.test`, `userB@yallajo.test` — regular customers.
   - `guide-approved@yallajo.test` — TourGuide/guide dashboard access.
   - `business@yallajo.test` — Provider-like business owner.
   - `agency@yallajo.test` — Provider/agency actor; can affiliate guides where seeded flow allows.
   - `suspended@yallajo.test` — suspended actor.
4. Core runtime roles seeded by current role system:
   - `Owner`, `SuperAdmin`, `Admin`, `User`, `Provider`, `TourGuide`, `Creator`, `Guest`.
5. If legacy/documented admin role labels are seeded (`Administrator`, `ContentAdmin`, `FinancialAdmin`, `SupportAdmin`, `ProviderApprover`), tests should map them to current permissions or mark as environment-specific aliases.
6. Current system seeder is `SecurityDataSeeder`; do not rely on legacy `SecurityDbInitializer` (only older 5-role bootstrap behavior).
7. Security API prefix from current code: `/api/v1/security`.
8. Playwright helper functions available:
   - `loginAs(email)` stores cookies/JWT.
   - `apiFetch(token, path, { method, body })` through `browser_evaluate`.
   - `getMe(token)` calls `GET /api/v1/security/me` to inspect roles/claims.

Recommended `browser_evaluate` helper shape:

```js
async ({ apiUrl, token, path, method = 'GET', body }) => {
  const res = await fetch(`${apiUrl}${path}`, {
    method,
    credentials: 'include',
    headers: {
      'content-type': 'application/json',
      ...(token ? { authorization: `Bearer ${token}` } : {})
    },
    body: body ? JSON.stringify(body) : undefined
  });
  const text = await res.text();
  return { status: res.status, headers: Object.fromEntries(res.headers), body: text ? JSON.parse(text) : null };
}
```

## 1. Built — Active Scenarios

### Role Assignment

**R-001 — Admin lists active roles**

- Actor: `admin@yallajo.test`.
- Route/API: `GET /api/v1/security/roles`.
- Expected:
  - `200 OK`.
  - Contains `Owner`, `SuperAdmin`, `Admin`, `User`, `Provider`, `TourGuide`, `Creator`, `Guest`.
  - If role aliases like `Administrator`/`ContentAdmin` exist, record them separately as seed aliases, not core `AppRoles`.

**R-002 — Admin lists users and opens target user**

- Actor: admin.
- Routes: `GET /api/v1/security/users`, `GET /api/v1/security/users/{userId}`.
- Expected:
  - Admin receives `200`.
  - Regular user receives `403` for list/get-any user.
  - User dto exposes enough role identifiers to perform assignment/revocation setup.

**R-003 — Admin assigns role to user**

- Actor: admin.
- Target: `userB` test account; role: `TourGuide` or disposable non-protected test role.
- Route/API: `POST /api/v1/security/users/{userId}/roles` with `{ roleId }`.
- Expected:
  - `200 OK`.
  - `GET /api/v1/security/users/{userId}` shows role assigned.
  - If JWT claims do not update until next login, re-login as target and assert `GET /security/me` includes role/permission claims.

**R-004 — Admin revokes role from user**

- Actor: admin.
- Route/API: `DELETE /api/v1/security/users/{userId}/roles/{roleId}`.
- Expected:
  - `200 OK`.
  - User no longer has role after reload/re-login.
  - Role permissions disappear from protected endpoint access.

**R-005 — User cannot self-assign role**

- Actor: `userA`.
- Route/API: `POST /api/v1/security/users/{userAId}/roles`.
- Expected:
  - `403 Forbidden` due missing `Security.UserRole.Create` permission.
  - Role list for userA unchanged.

**R-006 — User cannot assign role to another user**

- Actor: `userA`.
- Target: `userB`.
- Expected:
  - `403 Forbidden`.
  - No role mutation.

**R-007 — Protected role hierarchy prevents forbidden grants**

- Actor: admin (not Owner/SuperAdmin if seed supports lower admin).
- Target role: `Owner` or `SuperAdmin`.
- Expected:
  - Non-owner cannot grant owner-only/protected roles.
  - Error is `403`/business forbidden, not `500`.
  - Owner/SuperAdmin behavior follows privilege hierarchy if those accounts are seeded.

**R-008 — Duplicate role assignment is idempotent/conflict-clean**

- Actor: admin.
- Steps: assign same role twice to target.
- Expected:
  - Second call returns `409 Conflict`, success/no-op, or validation outcome per implementation.
  - No duplicate `UserRole` rows or duplicate JWT role claims.

### Permission Checks

**R-009 — `/security/me` returns current JWT roles and permission claims**

- Actor: each seed user.
- Route/API: `GET /api/v1/security/me`.
- Expected:
  - Authenticated users receive `200`.
  - Roles array matches expected seed role(s).
  - Claims include permission claims except JWT metadata.
  - Anonymous receives `401/403`.

**R-010 — Owner/SuperAdmin/Admin hierarchy permission smoke**

- Actor: admin and any higher-privilege seeded account.
- Routes: `/api/v1/security/roles`, `/api/v1/security/users`, `/api/v1/security/audit-logs`.
- Expected:
  - Admin can read management surfaces except owner/super-admin-only operations.
  - SuperAdmin can do near-full management except `Security.System.Update` where relevant.
  - Owner can access all permissions if seeded.

**R-011 — Regular User permission set**

- Actor: `userA`.
- Expected allows:
  - Own/self endpoints such as profile/user self update where exposed.
  - Guest-accessible/public reads.
- Expected denies:
  - `/api/v1/security/users`, `/roles`, admin analytics endpoints, finance admin endpoints, content management mutations.

**R-012 — Provider permission set**

- Actor: `business@yallajo.test`.
- Current mapping: Provider gets all `ContentManagement` permissions with `Read/Create/Update/Delete`, `Security.User.UpdateSelf`, guest-accessible permissions.
- Expected:
  - Provider can access provider/business content-management endpoints where status checks pass.
  - Provider cannot access security role/user management or finance/admin dashboards.
  - Suspended provider status checks can still block despite role claims.

**R-013 — TourGuide permission set**

- Actor: `guide-approved@yallajo.test`.
- Current mapping: TourGuide gets `ContentManagement` `Read/Create/Delete`, `Security.User.UpdateSelf`, guest-accessible permissions.
- Expected:
  - Can access guide dashboard/guide-owned tour flows.
  - Cannot create ContentPlaces business listings if workflow status/service rules are enforced.
  - Cannot access security role management or finance admin.

**R-014 — Creator permission set**

- Actor: creator seed if available; otherwise create/assign Creator to disposable user through admin.
- Current mapping: Creator gets `ContentManagement` `Read/Create/Delete`, `Security.User.UpdateSelf`, guest-accessible permissions.
- Expected:
  - Creator can access creator/blog content creation flows where profile status is active.
  - Cannot update content if current broad mapping excludes `Update` for Creator.
  - Cannot access security/finance admin.

**R-015 — Guest permission set before email verification**

- Actor: newly registered email user before verification, or seeded Guest account.
- Expected:
  - Has `Guest` role only.
  - Can access guest-accessible public reads.
  - Cannot book/write/manage until email verification promotes to User.

**R-016 — Policy-based auth rejects missing permission claims**

- Actor: `userA`.
- Routes:
  - `GET /api/v1/security/users` requires `Security.User.Read`.
  - `POST /api/v1/security/users/{id}/roles` requires `Security.UserRole.Create`.
  - `GET /api/v1/analytics/admin/dashboard` requires admin dashboard permission.
- Expected: all return `403`, proving `[MustHavePermission]`/policy checks are active.

**R-017 — Module-specific role access: content-admin vs finance-admin aliases**

- Actors: `ContentAdmin`, `FinancialAdmin`, `SupportAdmin`, `ProviderApprover` if seeded as aliases.
- Expected:
  - ContentAdmin can edit content modules but not finance/security admin.
  - FinancialAdmin can access finance admin but not content moderation.
  - SupportAdmin can access support/messaging tooling but not finance/content mutations.
  - ProviderApprover can approve provider applications but not role management.
- Note: If these roles are not seeded in current `AppRoles`, mark this scenario as seed-alias NOT_BUILT and test the nearest permission claims instead.

### Suspended User

**R-018 — Suspended user login behavior is clarified**

- Actor: `suspended@yallajo.test`.
- Steps: attempt login through Web/API.
- Expected:
  - If workflow enforces suspension at auth layer: login fails with `401/403` and no usable token.
  - If status-gate model applies: login succeeds but protected domain actions return `403`.
  - Record actual behavior; do not assume role removal.

**R-019 — Suspended user receives 403 on protected role-gated endpoints**

- Actor: suspended user.
- Routes: `/api/v1/security/me`, analytics guide/provider endpoints, content/booking/provider actions according to seed role.
- Expected:
  - Either no login token, or role/status-gated requests return `403`.
  - No admin/security endpoint data leaks.

**R-020 — Suspension does not remove roles**

- Actor: admin checks suspended user.
- Route/API: `GET /api/v1/security/users/{suspendedUserId}`.
- Expected:
  - Roles remain assigned, consistent with plan: suspension blocks via entity/account status, not role removal.
  - Reinstate should restore access without needing re-assignment where the relevant entity status is reinstated.

### Multi-Role User

**R-021 — User with two standard roles receives union of permissions**

- Actor: disposable target assigned `Provider` + `Creator`, or seeded business/agency with multiple roles.
- Steps:
  1. Admin assigns second role.
  2. Target re-logs in.
  3. Probe endpoint set for both roles.
- Expected:
  - JWT contains both role claims.
  - Permissions are additive/unioned.
  - No permission is lost from original role.

**R-022 — User + TourGuide + Creator stack remains standard privilege**

- Actor: disposable multi-role user.
- Expected:
  - `AppRoles.HighestPrivilegeLevel` equivalent remains Standard for User/TourGuide/Creator.
  - User cannot manage roles or security users despite multiple standard roles.

**R-023 — Provider + TourGuide distinction in entity capabilities**

- Actor: agency/provider with TourGuide additionally assigned.
- Expected:
  - Provider capabilities still work.
  - Guide dashboard works if TourGuide permission present.
  - IndependentGuide-only business creation restriction remains enforced by provider type/status services, not merely role stacking.

### Role Hierarchy (if exists)

**R-024 — Privilege level order is enforced**

- Actors: admin, superadmin/owner if seeded.
- Expected:
  - Owner (100) > SuperAdmin (80) > Admin (60) > Standard (10).
  - Lower privilege cannot mutate higher/protected roles.
  - Standard users cannot mutate any roles.

**R-025 — Protected role deactivation is forbidden**

- Actor: admin/non-owner.
- Route/API: `PATCH /api/v1/security/roles/{ownerOrSuperAdminRoleId}/deactivate`.
- Expected:
  - `403 Forbidden` or clean protected-role business error.
  - Role remains active.

### Automatic Role Assignment via Integration Events

**R-026 — ProviderApproved assigns Provider for non-IndependentGuide**

- Actor: provider approval flow, admin/provider approver.
- Trigger: `ProviderApprovedIntegrationEvent` with `ProviderType != IndependentGuide`.
- Expected:
  - Security handler assigns `Provider` role idempotently.
  - User keeps `User` role; final roles include `User + Provider`.

**R-027 — ProviderApproved assigns TourGuide for IndependentGuide**

- Trigger: `ProviderApprovedIntegrationEvent` with `ProviderType=IndependentGuide`.
- Expected:
  - Security handler assigns `TourGuide`, not `Provider`.
  - User cannot create ContentPlaces business listing solely from IndependentGuide approval.

**R-028 — Creator approval assigns Creator**

- Trigger: `CreatorApplicationApprovedIntegrationEvent`.
- Expected:
  - `Creator` role assigned once.
  - Replayed event is idempotent via inbox/role existing check.

**R-029 — Agency guide affiliation assigns TourGuide**

- Trigger: `AgencyGuideAffiliatedIntegrationEvent` or agency roster UI.
- Expected:
  - Affiliated user receives `TourGuide` if absent.
  - Already-guides remain single-claimed/idempotent.

**R-030 — Email verification upgrades Guest to User**

- Actor: newly registered email user.
- Trigger: `EmailVerifiedIntegrationEvent`.
- Expected:
  - Guest role removed.
  - User role added.
  - External/OAuth registration path may assign User directly; record as documented divergence.

## 2. NOT_BUILT

1. Per-feature granular role mapping may be NOT_BUILT if product expects ContentAdmin/FinancialAdmin/SupportAdmin/ProviderApprover as first-class roles. Current verified implementation uses broad group/action filtering for `Provider`, `Creator`, `TourGuide`.
2. Admin role-management UI may be absent; API scenarios remain valid via `/api/v1/security/*`.
3. Integration event end-to-end tests may need an outbox test hook or approval UI; if not available, execute skeleton by driving provider/creator/agency approval workflows.
4. Explicit role aliases (`Administrator`, `ContentAdmin`, `FinancialAdmin`, `SupportAdmin`, `ProviderApprover`) are not part of current `AppRoles.cs`; mark alias-specific scenarios NOT_BUILT unless seed data defines them as custom roles with equivalent permission claims.

## 3. DEFERRED

1. Refactor `RolePermissionMapping` from broad `PermissionGroup.ContentManagement` filtering to per-feature permissions if product rejects MVP broad mapping.
2. Remove or retire legacy `SecurityDbInitializer` to avoid confusion with `SecurityDataSeeder`.
3. Dedicated admin UI for role assignment/revocation and role-claim editing if Razor pages are missing.
4. Automated cross-module suspension cascade tests beyond status-gate probes.
5. Full integration test suite for all 8 roles and all endpoint families, as Master Roadmap W4-F target.

## 4. Integration Events (UserRoleGranted, UserRoleRevoked, UserSuspended)

Current plan-verified role assignment events/handlers:

1. `ProviderApprovedIntegrationEvent` → `ProviderApprovedAssignRoleHandler`:
   - `IndependentGuide` → `TourGuide`.
   - All other provider types → `Provider`.
2. `CreatorApplicationApprovedIntegrationEvent` → `CreatorApprovedAssignRoleHandler`:
   - Assigns `Creator`.
3. `AgencyGuideAffiliatedIntegrationEvent` → `AgencyGuideAffiliatedAssignRoleHandler`:
   - Assigns `TourGuide` if not already present.
4. `EmailVerifiedIntegrationEvent` → `EmailVerifiedUpgradeRoleHandler`:
   - Removes `Guest`, adds `User`.

Workflow-requested event scenarios:

**R-EV-001 — UserRoleGranted observable side effect**

- Trigger role assignment through `POST /api/v1/security/users/{id}/roles`.
- Expected: role appears in target user details/JWT; if audit/outbox event `UserRoleGranted` exists, one event/log entry is created.

**R-EV-002 — UserRoleRevoked observable side effect**

- Trigger role removal through `DELETE /api/v1/security/users/{id}/roles/{roleId}`.
- Expected: role disappears; if `UserRoleRevoked` exists, one event/log entry is created.

**R-EV-003 — UserSuspended observable side effect**

- Trigger account/user/provider suspension through current admin endpoint (`PATCH /security/users/{id}/deactivate` or provider suspension flow).
- Expected: protected actions fail; if `UserSuspended`/lifecycle event exists, one audit/outbox entry is created.

## 5. Validation Matrix

| Area | Positive | Negative/edge |
|---|---|---|
| Role list | Admin reads roles | User/anonymous denied |
| Assign role | Admin assigns standard role | Self-assign denied; duplicate clean conflict/no-op |
| Revoke role | Admin revokes standard role | Protected role revoke denied; missing role 404 |
| Create role | Authorized admin creates non-duplicate | Duplicate name rejected; invalid name/empty desc rejected |
| Update/deactivate role | Authorized admin updates normal role | Owner/SuperAdmin protected from lower privilege |
| Role claims | Authorized admin adds/removes claims | Duplicate claim conflict; invalid claim type rejected |
| User claims | Authorized admin adds/removes claims | JWT meta claims cannot be abused; invalid user 404 |
| Guest lifecycle | Registration gives Guest | Guest cannot book/write/manage |
| Email verification | Guest→User | Replay idempotent; external registration documented User direct |
| Provider approval | Non-guide provider gets Provider | IndependentGuide does not get Provider |
| Creator approval | Creator role added | Replay idempotent |
| Agency affiliation | TourGuide role added | Existing TourGuide not duplicated |
| Suspension | Blocks protected actions | Roles retained; reinstatement restores without reassign |

## 6. Auth Matrix (full role × endpoint cross-product per Role-System workflow)

Legend: `A` allow, `D` deny, `S` status/ownership scoped.

| Role / Endpoint family | Public reads | `/security/me` | Security users/roles | Provider/content create | Guide dashboard | Creator/blog create | Finance admin | Analytics admin | Own profile/update |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Anonymous | A public only | D | D | D | D | D | D | D | D |
| Guest | A | A | D | D | D | D | D | D | D/limited |
| User | A | A | D | D | D | D | D | D | A |
| Provider | A | A | D | S/A | D unless also guide | D unless also creator | D | D | A |
| TourGuide | A | A | D | D for business listing; S/A for guide/tour flows | A own | D unless also creator | D | D | A |
| Creator | A | A | D | D unless also provider | D unless also guide | S/A | D | D | A |
| Provider+TourGuide | A | A | D | S/A | A own | D unless creator | D | D | A |
| User+Provider+Creator | A | A | D | S/A | D unless guide | S/A | D | D | A |
| Admin | A | A | A except protected/owner-only | A | A or policy-scoped | A | A if granted | A | A |
| SuperAdmin | A | A | A except owner-only system update | A | A | A | A | A | A |
| Owner | A | A | A all | A | A | A | A | A | A |
| Suspended | D or login denied | D/A limited | D | D | D | D | D | D | D/A limited |

Endpoint probes for cross-product:

1. Public read: `GET /api/v1/popular/tours` or content public page.
2. Current user: `GET /api/v1/security/me`.
3. Role management: `GET /api/v1/security/roles`, `POST /api/v1/security/users/{id}/roles`.
4. Provider/content create: ContentPlaces/ContentTours create endpoint available in environment.
5. Guide dashboard: `GET /api/v1/guide/dashboard`.
6. Creator/blog create: ContentBlogs creator endpoint available in environment.
7. Finance admin: representative `/api/v1/payouts/admin/*` or finance admin route.
8. Analytics admin: `GET /api/v1/admin/dashboard` or `/api/v1/analytics/admin/batches` depending actual route.
9. Own profile/update: Accounts/Security self endpoint.

## 7. Known Divergence (~110 ICurrentUser redundant checks per CrossDocumentAnalysisReport)

1. CrossDocumentAnalysisReport documents ~110 redundant `ICurrentUser` checks across 7 modules and 11 admin bypass violations. Role-system tests should prefer endpoint policy outcomes over handler internals, but failures here indicate systemic auth debt.
2. Current Role-System audit score is `8.8/10` in audit report while Role-System plan header says `9.5/10`; Master roadmap treats Role-System as `9.5 → 10.0`. Test file targets the remaining integration-test gap, not a re-score.
3. Role plan originally described per-feature permission mapping; code uses broad group/action filtering in `RolePermissionMapping`. Tests must assert actual behavior and flag overbroad access if product expects granularity.
4. `TourGuide` UpdateSelf gap from audit appears fixed in sampled code: TourGuide mapping includes `Security.User.UpdateSelf`.
5. `EmailVerifiedIntegrationEvent` is in `Security.Contracts`, not Auth.Contracts.
6. `CreatorApplicationApprovedIntegrationEvent` property is `ApplicantUserId`, not generic `UserId`.
7. `EmailVerifiedIntegrationEvent` property is `EmailAddress`, not `Email`.
8. External/OAuth registration assigns `User` directly and skips Guest→User lifecycle.
9. Current core roles are `Owner/SuperAdmin/Admin/User/Provider/TourGuide/Creator/Guest`; workflow-requested roles such as `ContentAdmin`, `FinancialAdmin`, `SupportAdmin`, `ProviderApprover`, and `Administrator` may be seed aliases/custom roles rather than code constants.
