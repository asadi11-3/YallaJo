# Gap Report — `8-superadmin-rbac.md` vs. shipped code

> **Method:** deep code-vs-plan audit against the **shipped** `RolesController`, `UsersController`, `AuditLogsController`,
> `LifecycleController` (all `[Area("Admin")]`) **and their ApiClients** (`RolesApiClient`, `UsersApiClient`, `AuditLogsApiClient`).
> The plan was already corrected in the earlier §8 audit (routes, perms, verbs, the audit-export fix, "no SuperAdmin policy"); this pass
> goes to the **ApiClient/data-flow layer** to verify the API bindings and the §9-vs-§8.13 boundary.
>
> **Scope:** `yallajo-plan/8-superadmin-rbac.md` (§9 RBAC).
> **Severity:** 🔴 plan contradicts code · 🟠 conflation / structure · 🟡 cosmetic.
> **Status:** ❌ not built · ✏️ differs · ✅ matches.

---

## 0. Summary

| Topic | Verdict |
|-------|---------|
| Area / routes / perms / verbs | ✅ Accurate (prior §8 audit) — re-verified against all three controllers + ApiClients. API bindings confirmed: Roles→`security/roles`, Users→`security/users`. |
| **Audit read vs redact/export namespaces** | 🔴 **Plan conflates two API namespaces.** The **list read** is `GET /api/v1/security/audit-logs`; **redact + export** are `/api/v1/admin/audit-logs/{id}/redact` and `/admin/audit-logs/export`. These are **two different audit surfaces** (security trail vs admin trail). |
| **Export implementation** | 🟠 Plan's Stack says export is `IAsyncEnumerable` stream (R6). The ApiClient comment says **"returns serialized CSV content"** — not a streamed enumerable. |
| **Users page split** | 🟠 `/admin/users/details/{userId}` is **one shared detail page** rendering `UsersController` (roles/claims/activate) **and** `LifecycleController` (suspend/archive/reset/reassign/revoke-sessions). §9 RBAC user pane = §8.13 admin user page — same physical route, two controllers. |

---

## 1. 🔴 Plan-contradicts-code

### G1 — Audit list-read and redact/export hit different API namespaces
- **Plan says (§Audit, line 41):** *"BFF read: `GET /admin/audit-logs` → API `GET /security/audit-logs` (also `GET /admin/audit-logs` admin-trail)."* — treats them as one.
- **Code reality (`AuditLogsApiClient`):**
  - **List read:** `GET /api/v1/security/audit-logs?page&pageSize` ← this is the **RBAC/security audit trail** (role/claim/user-security changes).
  - **Redact:** `POST /api/v1/admin/audit-logs/{id}/redact` (with `reason`).
  - **Export:** `GET /api/v1/admin/audit-logs/export?from=&to=`.
  - So the **read** is `security/*` but **redact + export** are `admin/*` — they are **two distinct backend logs/surfaces**, not one. (The §9 console reads the *security* trail; §8.13 admin operates the *admin* trail. They may overlap in the UI but bind to different APIs.)
- **Action:** in §Audit, separate the bindings clearly:
  - Read (RBAC trail) → `GET /security/audit-logs`.
  - Redact → `POST /admin/audit-logs/{id}/redact` (`AuditLog.Redact`).
  - Export → `GET /admin/audit-logs/export` (`AuditLog.Export`).
  Note the read and the redact/export are **different namespaces** (don't imply one log).

---

## 2. 🟠 Structure / implementation gaps

### G2 — Audit export is CSV content, not an `IAsyncEnumerable` stream
- **Plan (Stack):** *"R6 DataTables (server-side) + server-side `GET /admin/audit-logs/export` (`IAsyncEnumerable`)."*
- **Code (`AuditLogsApiClient` comment):** `GET /api/v1/admin/audit-logs/export?from=&to=` **"returns serialized CSV content."** The Web layer fetches the CSV string and returns it as a download — there is **no `IAsyncEnumerable` streaming** in the shipped Web client.
- **Action:** change the Stack note to "server-side CSV export (`GET /admin/audit-logs/export?from=&to=`, returns CSV content)"; drop the `IAsyncEnumerable` claim (that's an aspirational R6 pattern, not the shipped reality).

### G3 — The Users pane is one page split across two controllers (§9 = §8.13)
- **Code:** `GET /admin/users/details/{userId}` (`UsersController.Details`) renders the user detail VM (`UserDetailsVm`); the **same `/admin/users/{userId}/*` route space** carries:
  - `UsersController`: activate/deactivate (`User.UpdateAny`), roles (`UserRole.Create/Delete`), claims (`User.UpdateAny`).
  - `LifecycleController`: suspend/reactivate/archive/reset-password/reassign/revoke-sessions (all `User.UpdateAny`).
- **vs plan:** §9 RBAC "Users" pane documents only activate/deactivate + roles/claims. The **lifecycle actions** (suspend/archive/reset/reassign/revoke) live on the **same page** but the plan attributes them to §8.13 (admin). They are **one shared user-detail surface**.
- **Action:** note that the §9 Users pane and §8.13 admin user page are the **same `/admin/users/details/{userId}` page** (UsersController + LifecycleController), so the RBAC console's user actions include the full lifecycle set, not just roles/claims/activate.

---

## 3. ✅ Confirmed-correct (re-verified incl. ApiClient layer)

- **Roles** — BFF `/admin/roles*`, perms `Role.Read/Create/Update` + `RoleClaim.Create/Delete`; ApiClient binds `security/roles*` (`PatchAsync` for update/deactivate, `PostAsync`/`DeleteAsync` for claims) ✅.
- **Users** — BFF `/admin/users*`, perms `User.Read/UpdateAny` + `UserRole.Create/Delete`; ApiClient binds `security/users*` ✅.
- **Verbs** — every BFF action is `POST` (e.g. `/admin/roles/{id}/update`, `/claims/{claimId}/remove`); API verbs `PATCH`/`DELETE` are the ApiClient layer ✅.
- **No `/admin/security` route, no `WebPermission.Security.*`, no SuperAdmin policy** ✅ (all confirmed).
- **Audit export IS a real endpoint** (the plan's prior "client-only" fix was correct) ✅ — only the *implementation detail* (CSV vs IAsyncEnumerable) and the *namespace split* (G1) need refining.
- **No `ST1`** — Role/User DTOs expose no RowVersion ✅.

---

## 4. Recommended plan edits (apply order)

1. **G1** §Audit — split the bindings: read = `GET /security/audit-logs` (RBAC trail); redact/export = `/admin/audit-logs/*` (admin trail). Don't imply one log.
2. **G2** Stack — export is **CSV content** (`?from=&to=`), not `IAsyncEnumerable`.
3. **G3** note the §9 Users pane = the §8.13 `/admin/users/details/{userId}` page (UsersController + LifecycleController); RBAC user actions include the full lifecycle set.

> **Net:** the RBAC plan is **accurate at the controller level** (routes, perms, verbs, no-SuperAdmin-policy all verified). The deep ApiClient read surfaced three refinements: the audit **read** (`security/audit-logs`) and **redact/export** (`admin/audit-logs`) are **different namespaces**, export is **CSV not IAsyncEnumerable**, and the Users pane is **one shared page** with the §8.13 lifecycle controller. All are precision/namespace corrections — no broken routes or invented permissions.

---

## Series complete

This is the **8th and final** gap report. The `gaps/` folder now covers every actor area:

| File | Headline gap |
|------|--------------|
| 1 storefront | SignalR live-slots unbuilt; favorites cross-area (Accounts); `/agency` not `/agencies` |
| 2 customer | Header wrongly claims "no permissions"; `/discover`,`/billing` don't exist; onboarding/join-create/linking unbuilt |
| 3 provider | `/provider/register` doesn't exist (it's `/apply`); Settings not read-only; bookings richer (`/manage`, Cancel, join-requests) |
| 4 guide | §5.7 at `/guide/tours` not `/guide/offerings`; no tier/schedule **edit**; Reviews page missing |
| 5 agency | §6.3 Invitations **is** shipped (inline on Roster Index) — plan wrongly said unbuilt |
| 6 creator | §7.4 Audience over-claims comments (followers-only); Preview page missing |
| 7 admin | `/admin/trips` second tour-moderation screen undocumented; 14 pages compress 24 nav items |
| 8 rbac | Audit read (`security/*`) vs redact/export (`admin/*`) namespace split; export is CSV |

**Cross-cutting lesson:** controller-attribute audits catch permissions/routes, but the **facade/VM/ApiClient layer** is where the real behavioral gaps hide (agency invitations, creator comments, rbac audit namespaces) — both false-"unbuilt" and false-"built" claims surfaced only there.
