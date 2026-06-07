# SuperAdmin — RBAC Console — Area

**Actor:** SuperAdmin / RBAC · **Plan section:** §9 · **Namespaces:** `/security/roles/*`, `/security/users/*`, `/security/audit-logs`

> **Source of truth:** YallaJo code (`yallajo-endpoints.txt`, 549 endpoints). All routes are `/api/v1`-prefixed.
> **Architecture & rules:** see [`0-architecture-and-rules.md`](0-architecture-and-rules.md) — four-tier pipeline (Controller → Facade → ApiClient → IApiClient), per-page **Stack** annotations below.
> **Code area:** RBAC has **no dedicated code area** — it is hosted under the **`Admin`** area (`/admin/security/*`) but gated to **SuperAdmin** via `WebPermission.Security.Manage` (stricter than the platform `Admin` policy). All panes are `NoStore`; every mutation is `[ValidateAntiForgeryToken]` + PRG.
> **No Webestica template page maps to the RBAC console** — built by you. It is a single console with Roles / Users / Audit panes.

**Status legend:** ✅ Wire · ♻️ Repurpose · ⏭️ Skip ·  🟥 Build Using Design skills (match the template theme)

**Shared shell:** top bar = global search + language switcher + notification bell (`GET /notifications/unread-count` `AJAX⟳`) + avatar menu (`GET /accounts/profile`). Nav driver = `GET /security/me`.
**Load tags:** `SSR` / `AJAX` / `AJAX⟳` / `AJAX↑`.

---

## Pages this area should have

| # | Page | Template | Redirects to |
|---|------|----------|--------------|
| 9 | RBAC Console (Roles / Users / Audit) |  🟥 Build Using Design skills (match the template theme)| role row → role detail · user row → user detail · activate inline · audit → audit-logs |

---

## Endpoints

### Roles
- `GET /security/roles[/{roleId}]` `SSR`
- `POST /security/roles` · `PATCH /security/roles/{roleId}[/deactivate]` `AJAX`
- Role-claims: `POST / DELETE /security/roles/{roleId}/claims[/{claimId}]` `AJAX`
- **Buttons:** **Create Role** → `POST /security/roles` · **Update Role** → `PATCH /security/roles/{roleId}` · **Deactivate Role** → `/deactivate` · **Add/Remove Role-Claim** → `POST /security/roles/{roleId}/claims` / `DELETE .../claims/{claimId}`.

### Users
- `GET /security/users[/{userId}]` (paged) `SSR`
- User-roles: `POST / DELETE` `AJAX`
- User-claims: `POST / DELETE` `AJAX`
- Activation: `PATCH /security/users/{userId}/activate` · `/deactivate` `AJAX`
- **Buttons:** **Assign/Remove User-Role** → `POST /security/users/{userId}/roles` / `DELETE .../roles/{roleId}` · **Add/Remove User-Claim** → `POST /security/users/{userId}/claims` / `DELETE .../claims/{claimId}` · **Activate/Deactivate User** → `PATCH /security/users/{userId}/activate|deactivate`.

### Audit
- `GET /security/audit-logs` `AJAX`
- **Buttons:** read-only audit log; **Export** is client-only (no endpoint). *(No write-endpoints.)*

---

- **Stack:** **Area** `Admin` (security/*, SuperAdmin-gated — no dedicated code area) · **Route** `/admin/security` (panes `/roles` · `/users` · `/audit`) · **Cache** `NoStore` (C2) · **Perm** policy `Admin` + `WebPermission.Security.Manage` (SuperAdmin-tier; strictest in the app) · **Rules** R2 SSR for Roles/Users lists · D1+§13 paging (Users) · ST1 RowVersion on role/user edits → 409 `[Reload]` · F8 confirm modal on **Deactivate Role** / **Deactivate User** (destructive) · A11Y5 active/inactive badge (color+icon+text) · NF1 toast on inline claim/role mutations · SEC7 anti-forgery on every role/user/claim write · SEC2 hardening headers + full audit trail (highest-privilege surface) · R6 DataTables (server-side) + client-only CSV export on the Audit pane (read-only, no write-endpoint).
