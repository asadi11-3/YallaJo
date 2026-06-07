# Agency Dashboard — Area

**Actor:** Agency · **Plan section:** §6 · **Namespaces:** `/agency/*`

> **Source of truth:** YallaJo code (`yallajo-endpoints.txt`, 549 endpoints). All routes are `/api/v1`-prefixed.
> **Architecture & rules:** see [`0-architecture-and-rules.md`](0-architecture-and-rules.md) — four-tier pipeline (Controller → Facade → ApiClient → IApiClient), per-page **Stack** legend, cache tiers, permissions.
> **Code area:** Agency has **no dedicated code area** → the agency-OWNER roster is built as a **self-service sub-feature of the `Guide` area** (shipped `AgencyRosterController`, `[Area("Guide")]`, routes under `/guide/agency/roster/*`), gated by `WebPermission.AgencyRoster.*` (NOT the platform `Admin` policy, and there is **no** `WebPermission.Agency.*` namespace). This is distinct from the guide self-service `AgencyController` (`/guide/agency`, §5.11 = joining/leaving an agency).
> **Verified against shipped code:** `Areas/Guide/Controllers/AgencyRosterController.cs` (Roster + Invite + inline approve/reject/remove) + `WebPermission.cs`.
> **BFF verbs are `POST` (PRG):** the `DELETE`/`POST` shown against `/api/v1/agency/*` paths are the **API-layer** verbs (ApiClient → API). Every BFF action is `[HttpPost]` + anti-forgery + PRG under `/guide/agency/roster/*`.
> **No Webestica template page maps to the agency console** — every page here is built by you.

**Status legend:** ✅ Wire · ♻️ Repurpose · ⏭️ Skip · 🟥 USER builds (no template page)

**Shared shell:** top bar = global search + language switcher + notification bell (`GET /notifications/unread-count` `AJAX⟳`) + avatar menu (`GET /accounts/profile`). Nav driver = `GET /security/me`.
**Load tags:** `SSR` / `AJAX` / `AJAX⟳` / `AJAX↑`.

---

## Pages this area should have

> **Build status note:** the entire agency console is **already shipped** as `AgencyRosterController` in the `Guide` area across **just 2 routes**: the **Roster Index** (`GET /guide/agency/roster`, which renders §6.1 Guides + §6.2 Applications + §6.3 Sent-Invitations **all inline**) and the **Invite form** (§6.4). The view (`.cshtml`) may be redesigned, but the controller + facade + VM already cover every action. **Nothing here is unbuilt.**

| # | Page | Build status | Redirects to |
|---|------|--------------|--------------|
| 6.1 | Roster | ✅ shipped (`AgencyRosterController.Index`) — section of `/guide/agency/roster` | guide row → §2.7 Guide detail · remove inline (with reason) |
| 6.2 | Applications | ✅ shipped (**inline section** of Roster Index) — not a standalone route | approve/reject inline → §6.1 Roster |
| 6.3 | Invitations | ✅ shipped (**inline section** of Roster Index — `AgencyRosterVm.SentInvitations`, badge `PendingInvitationCount`) | — *(read-only; no resend/revoke action exists)* |
| 6.4 | Recruit | ✅ shipped (`AgencyRosterController.Invite`) — the only second route | invite → §6.1 Roster |

---

## Endpoints by page

### 6.1 Roster *(shipped `AgencyRosterController.Index`)*
- API: `GET /agency/guides` `SSR` · `DELETE /agency/guides/{guideUserId}` (API verb)
- BFF: `GET /guide/agency/roster` (Index — also renders pending applications inline) · `POST /guide/agency/roster/guides/{guideUserId}/remove` (**requires a `reason`** — controller rejects empty)
- **Buttons:** **Remove Guide** → `POST /guide/agency/roster/guides/{guideUserId}/remove` (confirm modal **with reason field**) · **View Guide** → §2.7 (nav) · **Recruit** → §6.4 (nav).
- **Stack:** **Area** `Guide` (agency-owner sub-feature) · **Route** `/guide/agency/roster` · **Cache** `NoStore` · **Perm** class-level `WebPermission.AgencyRoster.Read`; Remove layers `AgencyRoster.Delete` (shipped) · **Rules** `R2` SSR roster · `D1`/§13 paging · `F8` confirm Remove Guide (destructive + reason) · `SEC7` anti-forgery · `A11Y5` status badges.

### 6.2 Applications *(inline on the Roster page — not a standalone route)*
> The shipped controller renders pending applications **inside the Roster Index** (`GET /guide/agency/roster`) and exposes approve/reject as POST actions; there is **no** separate `/agency/applications` page.
- API read: `GET /agency/applications` `SSR` (folded into the roster view)
- BFF actions: `POST /guide/agency/roster/applications/{id}/approve` · `POST /guide/agency/roster/applications/{id}/reject` (**reject requires a `reason`**)
- **Buttons:** **Approve** → `POST /guide/agency/roster/applications/{id}/approve` · **Reject** → `POST /guide/agency/roster/applications/{id}/reject` (with reason) · **View Applicant** → §2.7 (nav).
- **Stack:** **Area** `Guide` · **Route** rendered within `/guide/agency/roster` · **Cache** `NoStore` · **Perm** `WebPermission.AgencyRoster.Approve` (approve) / `AgencyRoster.Reject` (reject) (shipped) · **Rules** `R2` SSR queue · `NF1` toast / `_Alerts` flash on approve/reject · `SEC7` anti-forgery.

### 6.3 Invitations *(✅ shipped — inline section of the Roster Index, not a standalone route)*
> **Already built.** The sent-invitations list is rendered **inline on the Roster Index** (`GET /guide/agency/roster`): `GuideAgencyRosterFacade.GetRosterAsync` fetches it via `AgencyRosterApiClient.GetSentInvitationsAsync` → `GET /api/v1/agency/invitations/sent`, maps it into `AgencyRosterVm.SentInvitations` (with `HasSentInvitations` empty-state flag), and surfaces a `PendingInvitationCount` badge. There is **no** dedicated `/invitations` route and none is needed — it is a section of the consolidated console.
- API: `GET /agency/invitations/sent` `SSR` (folded into the roster view)
- BFF: rendered within `GET /guide/agency/roster` (no separate route)
- **Buttons:** **Invite More** → §6.4 (nav) · **View Guide** → §2.7 (nav). *(Read-only; no resend/revoke endpoint exists.)*
- **Stack:** **Area** `Guide` · **Route** rendered within `/guide/agency/roster` · **Cache** `NoStore` · **Perm** `WebPermission.AgencyRoster.Read` (the Index's class-level perm) · **Rules** `R2` SSR read-only list · `L6` empty-state CTA → Recruit (via `HasSentInvitations`).

### 6.4 Recruit *(shipped `AgencyRosterController.Invite`)*
- API: `GET /agency/guides/available` `SSR` (available guides, populated into the invite form) · `POST /agency/guides/invite`
- BFF: `GET /guide/agency/roster/invite` (form) · `POST /guide/agency/roster/invite` (submit; on validation failure the form re-populates available guides)
- **Buttons:** **Invite Guide** → `POST /guide/agency/roster/invite` · **View Guide** → §2.7 (nav).
- **Stack:** **Area** `Guide` · **Route** `/guide/agency/roster/invite` · **Cache** `NoStore` · **Perm** `WebPermission.AgencyRoster.Create` (GET form + POST, shipped) · **Rules** `R2` SSR + `S1`/`AJAX` filter search · `D1`/§13 paging · `NF1`/`_Alerts` flash on invite · `SEC7` anti-forgery.
