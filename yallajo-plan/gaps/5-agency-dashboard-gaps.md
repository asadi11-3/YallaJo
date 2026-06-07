# Gap Report — `5-agency-dashboard.md` vs. shipped code

> ## ✅ RESOLVED — docs reconciled + a real backend defect fixed
> **Docs (G1/G2):** `5-agency-dashboard.md` updated — §6.3 Invitations changed from "🟥 not yet shipped" to **✅ shipped inline** on the Roster Index (`AgencyRosterVm.SentInvitations` + `PendingInvitationCount`, via `GetSentInvitationsAsync`); page table + structure note now make explicit the console is **2 routes** (Roster Index renders §6.1+§6.2+§6.3 inline; §6.4 Invite is the only second route).
>
> **Code — backend audit (scope explicitly extended to the backend):** the web BFF (controller/facade/apiclient/VM) and most of the Accounts-module Agency backend are production-grade (endpoints all `RequireAuthorization` + `MustHavePermission(AgencyRoster.*)`; handlers have ownership checks, state guards, concurrency handling, persistence, cache invalidation, logging). **One real shippable defect found & fixed:**
> - **NRE→500 on null reason/message** (same class as the prior "F126" fix that added `InviteGuideCommandValidator`): `RejectGuideApplicationCommand`, `RemoveGuideCommand`, and `ApplyToAgencyCommand` had **no validators**, and their domain methods (`AgencyApplication.Reject`, `AgencyAffiliation.Terminate`, `AgencyApplication.Create`) call `.Trim()` on a possibly-null string → a direct API call with a null/missing `reason`/`message` threw `NullReferenceException` → HTTP 500.
> - **Fix (Oracle-approved):** added `RejectGuideApplicationCommandValidator`, `RemoveGuideCommandValidator`, `ApplyToAgencyCommandValidator` (auto-discovered by the existing FluentValidation assembly scan); Reject/Remove now require a non-empty `Reason` (clean 400); `ApplyToAgencyCommand.Message` made `string?` and `AgencyApplication.Create` null-coalesces (`message?.Trim() ?? string.Empty`) since the apply message is optional. Build: `0 Error(s)`; LSP: 0 errors.
>
> **Documented follow-ups (out of scope — business-semantics/refactor, not the precedent-backed defect):** (a) Apply/Invite handlers don't verify the target agency/guide exists or is *approved* (only check caller's own `ProviderType`); (b) guide-side `GetMyInvitationsQuery` returns all statuses though the endpoint comment says "pending"; (c) no domain events on approve/reject/accept/decline; (d) sent/received invitations share one cache key (only collides if one user is both agency-owner and guide). These change business behavior and should be separately specced.


> **Method:** deep code-vs-plan audit against the **shipped** `Areas/Guide/Controllers/AgencyRosterController.cs` (138 lines, read in full),
> its `GuideAgencyRosterFacade`, `AgencyRosterApiClient`, and `AgencyRosterVm`. This file was already heavily corrected in the earlier §5
> audit (area `Guide`, `/guide/agency/roster/*`, `AgencyRoster.*` perms, POST+reason) — so this deep pass goes to the **facade/VM layer** to
> verify the page structure and the one remaining unverified claim (§6.3 "Invitations unbuilt").
>
> **Scope:** `yallajo-plan/5-agency-dashboard.md` (§6 agency).
> **Severity:** 🔴 plan contradicts code · 🟠 structure wrong · 🟡 cosmetic/clarity.
> **Status:** ❌ not built · ✏️ differs · ✅ matches.

---

## 0. Summary

| Topic | Verdict |
|-------|---------|
| Area / routes / perms / verbs | ✅ Correct (prior §5 audit): `[Area("Guide")]`, `/guide/agency/roster/*`, `AgencyRoster.Read/Create/Approve/Reject/Delete`, all `POST` + `reason` on Reject/Remove. Re-verified line-by-line against the controller. |
| **§6.3 Invitations** | 🔴 **Plan is wrong.** It says "🟥 not yet shipped — API exists, no BFF route." In reality the **sent-invitations list IS shipped** — rendered **inline on the Roster Index** (`AgencyRosterVm.SentInvitations` + `PendingInvitationCount`, fetched via `GetSentInvitationsAsync`). |
| **Page structure** | 🟠 The shipped agency console is **2 routes** — Roster Index (Guides + Applications + **SentInvitations**) and the Invite form — not the plan's 4 logical pages. §6.1/§6.2/§6.3 all render on **one** Index. |
| Recruit (§6.4) | ✅ Matches — `GET/POST /guide/agency/roster/invite`, `AgencyRoster.Create`. |

---

## 1. 🔴 Plan-contradicts-code

### G1 — §6.3 Invitations is shipped (inline on Roster Index), not "🟥 unbuilt"
- **Plan says (§6.3):** *"The API `GET /agency/invitations/sent` exists, but the shipped `AgencyRosterController` has **no `/invitations` route**. This is a genuine 🟥 USER-builds page."*
- **Code reality:**
  - `AgencyRosterApiClient.GetSentInvitationsAsync()` → `GET /api/v1/agency/invitations/sent` (returns `List<AgencySentInvitationResponse>`).
  - `GuideAgencyRosterFacade.GetRosterAsync()` fetches **guides + applications + sent-invitations in one pass** and builds:
    ```
    AgencyRosterVm {
        Guides,                 // affiliated roster
        Applications,           // pending guide applications
        SentInvitations,        // ← invitations ARE here
        PendingApplicationCount,
        PendingInvitationCount  // ← invitation badge ships too
    }
    ```
  - The Roster Index view (`GET /guide/agency/roster`) renders all three lists.
- **Conclusion:** Invitations are **built and rendered** — they just don't have a *dedicated route*; they live on the Index. The "no BFF route → unbuilt" reasoning is incorrect (a sub-list on a shipped page is still shipped).
- **Action:** Rewrite §6.3 to: *"Invitations-sent is **shipped inline on the Roster Index** (`AgencyRosterVm.SentInvitations`, badge `PendingInvitationCount`). No dedicated route — it is a section of `/guide/agency/roster`. (No resend/revoke action exists.)"* Change its build status from 🟥 to **✅ shipped (inline)**.

---

## 2. 🟠 Structure gaps

### G2 — The console is one consolidated Index + an Invite form (2 routes, not 4 pages)
- **Code (`AgencyRosterController`, full surface):**

  | Route | Action | Perm | Notes |
  |-------|--------|------|-------|
  | `GET /guide/agency/roster` | Index | `AgencyRoster.Read` | renders **Guides + Applications + SentInvitations** |
  | `GET /guide/agency/roster/invite` | Invite (form) | `AgencyRoster.Create` | available-guides picker |
  | `POST /guide/agency/roster/invite` | Invite (submit) | `AgencyRoster.Create` | re-populates picker on validation fail |
  | `POST /guide/agency/roster/applications/{id}/approve` | Approve | `AgencyRoster.Approve` | PRG → Index |
  | `POST /guide/agency/roster/applications/{id}/reject` | Reject | `AgencyRoster.Reject` | **requires `reason`** |
  | `POST /guide/agency/roster/guides/{guideUserId}/remove` | Remove | `AgencyRoster.Delete` | **requires `reason`** |

- **vs plan:** §6.1 Roster, §6.2 Applications, **§6.3 Invitations** all live on the **single Index**; only §6.4 Recruit is a second route (the Invite form). The plan's 4-page table is logically fine but should make explicit that **3 of the 4 are sections of one page**.
- **Action:** update the page table — mark §6.1 + §6.2 + §6.3 as **"inline on Roster Index"** (one route), §6.4 as the separate Invite form. Note the Invite form (`AgencyRoster.Create`) is also where "Recruit" lives.

---

## 3. ✅ Confirmed-correct (re-verified line-by-line)

- **Area:** `[Area("Guide")]` ✅. **Class perm:** `[RequirePermission(AgencyRoster.Read)]` ✅.
- **Invite:** GET + POST `/guide/agency/roster/invite`, `AgencyRoster.Create`, re-populates available guides on `ModelState` failure ✅ (plan §6.4 correct).
- **Approve:** `POST .../applications/{id}/approve`, `AgencyRoster.Approve`, PRG to Index ✅.
- **Reject:** `POST .../applications/{id}/reject`, `AgencyRoster.Reject`, **rejects empty `reason`** ✅ (plan correctly notes reason).
- **Remove:** `POST .../guides/{guideUserId}/remove`, `AgencyRoster.Delete`, **rejects empty `reason`** ✅ (plan correctly notes reason).
- **All actions** `[ValidateAntiForgeryToken]` + `SetFlash`/`_Alerts` + `RedirectToAction(Index)` (PRG) ✅.
- **Distinct from §5.11** — guide-side `AgencyController` (`/guide/agency`, join/leave) is correctly separated ✅.

---

## 4. Recommended plan edits (apply order)

1. **G1** §6.3 — change build status 🟥 → **✅ shipped (inline on Roster Index)**; rewrite the note (invitations ARE rendered via `AgencyRosterVm.SentInvitations`, no dedicated route, no resend/revoke).
2. **G2** page table — annotate §6.1/§6.2/§6.3 as **sections of the single `/guide/agency/roster` Index**; §6.4 = the Invite form route. Add `PendingInvitationCount` to the badge notes.

> **Net:** the agency plan is otherwise **accurate** (area, routes, perms, verbs, reason-required all verified against the 138-line controller). The single real gap is **§6.3**: the deep facade/VM read proves invitations are **already built inline** on the Roster Index — the plan's "🟥 not yet shipped" is wrong. Everything else is a structure-clarity note (3 of 4 "pages" are one consolidated Index).
