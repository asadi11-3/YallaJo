# Agency Dashboard — Area

**Actor:** Agency · **Plan section:** §6 · **Namespaces:** `/agency/*`

> **Source of truth:** YallaJo code (`yallajo-endpoints.txt`, 549 endpoints). All routes are `/api/v1`-prefixed.
> **Architecture & rules:** see [`0-architecture-and-rules.md`](0-architecture-and-rules.md) — four-tier pipeline (Controller → Facade → ApiClient → IApiClient), per-page **Stack** legend, cache tiers, permissions.
> **Code area:** Agency has **no dedicated code area** in the 9-area structure → hosted under **`Admin`**-area conventions, but gated to the **Agency role** via `WebPermission.Agency.*` permission claims (NOT the platform `Admin` policy).
> **No Webestica template page maps to the agency console** — every page here is built by you.

**Status legend:** ✅ Wire · ♻️ Repurpose · ⏭️ Skip · 🟥 USER builds (no template page)

**Shared shell:** top bar = global search + language switcher + notification bell (`GET /notifications/unread-count` `AJAX⟳`) + avatar menu (`GET /accounts/profile`). Nav driver = `GET /security/me`.
**Load tags:** `SSR` / `AJAX` / `AJAX⟳` / `AJAX↑`.

---

## Pages this area should have

| # | Page | Template | Redirects to |
|---|------|----------|--------------|
| 6.1 | Roster | 🟥 USER builds | guide row → §2.7 Guide detail · remove inline |
| 6.2 | Applications | 🟥 USER builds | approve/reject inline → §6.1 Roster |
| 6.3 | Invitations | 🟥 USER builds | — |
| 6.4 | Recruit | 🟥 USER builds | invite → §6.3 Invitations |

---

## Endpoints by page

### 6.1 Roster
- `GET /agency/guides` `SSR`
- `DELETE /agency/guides/{guideUserId}` `AJAX`
- **Buttons:** **Remove Guide** → `DELETE /agency/guides/{guideUserId}` (confirm) · **View Guide** → §2.7 (nav) · **Recruit** → §6.4 (nav).
- **Stack:** **Area** `Admin` (agency-hosted) · **Route** `/agency/roster` · **Cache** `NoStore` · **Perm** `WebPermission.Agency.Read` · **Rules** `R2` SSR roster · `D1`/§13 paging · `F8` confirm Remove Guide · `SEC7` anti-forgery · `A11Y5` status badges.

### 6.2 Applications
- `GET /agency/applications` `SSR`
- `POST /agency/applications/{id}/approve` · `/reject` `AJAX`
- **Buttons:** **Approve** → `POST /agency/applications/{id}/approve` · **Reject** → `POST /agency/applications/{id}/reject` · **View Applicant** → §2.7 (nav).
- **Stack:** **Area** `Admin` (agency-hosted) · **Route** `/agency/applications` · **Cache** `NoStore` · **Perm** `WebPermission.Agency.Manage` · **Rules** `R2` SSR queue · `D1`/§13 paging · `NF1` toast on approve/reject · `SEC7` anti-forgery.

### 6.3 Invitations
- `GET /agency/invitations/sent` `SSR`
- **Buttons:** **Invite More** → §6.4 (nav) · **View Guide** → §2.7 (nav). *(Read-only; no resend/revoke endpoint.)*
- **Stack:** **Area** `Admin` (agency-hosted) · **Route** `/agency/invitations` · **Cache** `NoStore` · **Perm** `WebPermission.Agency.Read` · **Rules** `R2` SSR read-only list · `D1`/§13 paging · `L6` empty-state CTA → Recruit.

### 6.4 Recruit
- `GET /agency/guides/available` `SSR`
- `POST /agency/guides/invite` `AJAX`
- **Buttons:** **Invite Guide** → `POST /agency/guides/invite` · **View Guide** → §2.7 (nav).
- **Stack:** **Area** `Admin` (agency-hosted) · **Route** `/agency/recruit` · **Cache** `NoStore` · **Perm** `WebPermission.Agency.Manage` · **Rules** `R2` SSR + `S1`/`AJAX` filter search · `D1`/§13 paging · `NF1` toast on invite · `SEC7` anti-forgery.
