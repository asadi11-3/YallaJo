# SuperAdmin — RBAC Console — Area

**Actor:** SuperAdmin / RBAC · **Plan section:** §9 · **Namespaces:** `/security/roles/*`, `/security/users/*`, `/security/audit-logs`

> **Source of truth:** YallaJo code (`yallajo-endpoints.txt`, 549 endpoints). All routes are `/api/v1`-prefixed.
> **Architecture & rules:** see [`0-architecture-and-rules.md`](0-architecture-and-rules.md) — four-tier pipeline (Controller → Facade → ApiClient → IApiClient), per-page **Stack** annotations below.
> **Code area:** RBAC has **no dedicated code area** — it is **already shipped** under the **`Admin`** area as `RolesController` (`/admin/roles/*`), `UsersController` (`/admin/users/*`) and `AuditLogsController` (`/admin/audit-logs*`). There is **no** `/admin/security` route and **no** `WebPermission.Security.*` namespace; the shipped controllers gate per-action with the real `Role.*` / `RoleClaim.*` / `User.*` / `UserRole.*` / `System.Read` / `AuditLog.*` constants (standard Admin-tier permissions — there is **no distinct SuperAdmin policy**; "SuperAdmin-only" is enforced by *which roles hold these permissions*, not by a separate gate). All panes are `NoStore`; every mutation is `[HttpPost]` + `[ValidateAntiForgeryToken]` + PRG.
> **BFF verbs are `POST` (PRG):** the `PATCH`/`DELETE` shown against `/security/*` are **API-layer** verbs (ApiClient → API). Every BFF action is `[HttpPost]` (e.g. `POST /admin/roles/{roleId}/update`, `/deactivate`, `/claims/{claimId}/remove`).
> **No Webestica template page maps to the RBAC console** — but the controllers are shipped, so this is a **view (re)design**. It is a single console with Roles / Users / Audit panes.

**Status legend:** ✅ Wire · ♻️ Repurpose · ⏭️ Skip · 🟥 USER builds (no template page — controllers shipped, so view-redesign)

**Shared shell:** top bar = global search + language switcher + notification bell (`GET /notifications/unread-count` `AJAX⟳`) + avatar menu (`GET /accounts/profile`). Nav driver = `GET /security/me`.
**Load tags:** `SSR` / `AJAX` / `AJAX⟳` / `AJAX↑`.

---

## Pages this area should have

| # | Page | Template | Redirects to |
|---|------|----------|--------------|
| 9 | RBAC Console (Roles / Users / Audit) | 🟥 view redesign (shipped `Roles`/`Users`/`AuditLogs` controllers) | role row → role detail · user row → user detail · activate inline · audit → audit-logs |

---

## Endpoints

### Roles *(shipped `RolesController`, `/admin/roles/*`)*
- BFF read: `GET /admin/roles` (list) · `GET /admin/roles/details/{roleId}` → API `GET /security/roles[/{roleId}]` `SSR`
- BFF writes (all `POST`, PRG): `POST /admin/roles/create` → API `POST /security/roles` · `POST /admin/roles/{roleId}/update` → API `PATCH /security/roles/{roleId}` · `POST /admin/roles/{roleId}/deactivate` → API `PATCH .../deactivate` · `POST /admin/roles/{roleId}/claims` → API `POST .../claims` · `POST /admin/roles/{roleId}/claims/{claimId}/remove` → API `DELETE .../claims/{claimId}`
- **Perm (shipped):** class `Role.Read`; **Create** `Role.Create`; **Update**/**Deactivate** `Role.Update`; **Add Claim** `RoleClaim.Create`; **Remove Claim** `RoleClaim.Delete`.
- **Buttons:** **Create Role** → `POST /admin/roles/create` · **Update Role** → `POST /admin/roles/{roleId}/update` · **Deactivate Role** → `POST /admin/roles/{roleId}/deactivate` · **Add/Remove Role-Claim** → `POST /admin/roles/{roleId}/claims` / `POST /admin/roles/{roleId}/claims/{claimId}/remove`.

### Users *(shipped `UsersController`, `/admin/users/*`)*
- BFF read: `GET /admin/users` (paged) · `GET /admin/users/details/{userId}` → API `GET /security/users[/{userId}]` `SSR`
- BFF writes (all `POST`, PRG): roles `POST /admin/users/{userId}/roles` / `POST /admin/users/{userId}/roles/{roleId}/remove` · claims `POST /admin/users/{userId}/claims` / `POST /admin/users/{userId}/claims/{claimId}/remove` · activation `POST /admin/users/{userId}/activate` / `POST /admin/users/{userId}/deactivate` (API: `POST .../roles`, `DELETE .../roles/{roleId}`, `POST .../claims`, `DELETE .../claims/{claimId}`, `PATCH .../activate|deactivate`)
- **Perm (shipped):** class `User.Read`; **Activate/Deactivate** + **Add/Remove Claim** `User.UpdateAny`; **Assign Role** `UserRole.Create`; **Remove Role** `UserRole.Delete`.
- **Buttons:** **Assign/Remove User-Role** → `POST /admin/users/{userId}/roles` / `POST /admin/users/{userId}/roles/{roleId}/remove` · **Add/Remove User-Claim** → `POST /admin/users/{userId}/claims` / `POST /admin/users/{userId}/claims/{claimId}/remove` · **Activate/Deactivate User** → `POST /admin/users/{userId}/activate|deactivate`.

### Audit *(shipped `AuditLogsController`, `/admin/audit-logs*` — shared with §8.13)*
- BFF read: `GET /admin/audit-logs` → API `GET /security/audit-logs` (also `GET /admin/audit-logs` admin-trail) `SSR`/`AJAX`
- **Export is a real server-side endpoint** (the plan previously claimed "client-only"): `GET /admin/audit-logs/export` (`AuditLog.Export`, `IAsyncEnumerable` stream for >10K rows per R6). Redact: `POST /admin/audit-logs/{id}/redact` (`AuditLog.Redact`).
- **Perm (shipped):** class `System.Read` (list) · `AuditLog.Export` (export) · `AuditLog.Redact` (redact).
- **Buttons:** **Export Audit** → `GET /admin/audit-logs/export` (download) · **Redact** → `POST /admin/audit-logs/{id}/redact`.

---

- **Stack:** **Area** `Admin` (RBAC has no dedicated code area; shipped under Admin) · **Route** `/admin/roles*` · `/admin/users*` · `/admin/audit-logs*` (there is **no** `/admin/security` route) · **Cache** `NoStore` (C2) · **Perm** `[Authorize]` + class-level `[RequirePermission(...)]` per-action constants — authorization is enforced **by which roles hold these permission claims** (no `[Authorize(Policy="Admin")]`; the Web host uses bare `AddAuthorization()`), consistent with the "no distinct SuperAdmin policy" rule above. The real per-action constants (shipped): `Role.Read`/`Create`/`Update`, `RoleClaim.Create`/`Delete`, `User.Read`/`UpdateAny`, `UserRole.Create`/`Delete`, `System.Read` + `AuditLog.Redact`/`Export` (audit) — **no `Security.Manage`, no separate SuperAdmin policy** (privilege is governed by which roles hold these permissions) · **Rules** R2 SSR for Roles/Users lists · D1+§13 paging (Users) · F8 confirm modal on **Deactivate Role** / **Deactivate User** (destructive) · A11Y5 active/inactive badge (color+icon+text) · NF1 toast on inline claim/role mutations · SEC7 anti-forgery on every role/user/claim write · SEC2 hardening headers + full audit trail (highest-privilege surface) · R6 DataTables (server-side) + **server-side `GET /admin/audit-logs/export`** (`IAsyncEnumerable`) on the Audit pane *(no `ST1`: Role/User DTOs expose no `RowVersion` token; the API guards state preconditions server-side)*.
