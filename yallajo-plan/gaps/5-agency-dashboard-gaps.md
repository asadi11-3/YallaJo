# Gap Report — `5-agency-dashboard.md` vs. shipped code

> ## ✅ RESOLVED — docs reconciled + a real backend defect fixed
> **Docs (G1/G2):** `5-agency-dashboard.md` updated — §6.3 Invitations changed from "🟥 not yet shipped" to **✅ shipped inline** on the Roster Index (`AgencyRosterVm.SentInvitations` + `PendingInvitationCount`, via `GetSentInvitationsAsync`); page table + structure note now make explicit the console is **2 routes** (Roster Index renders §6.1+§6.2+§6.3 inline; §6.4 Invite is the only second route).
>
> **Code — backend audit (scope explicitly extended to the backend):** the web BFF (controller/facade/apiclient/VM) and most of the Accounts-module Agency backend are production-grade (endpoints all `RequireAuthorization` + `MustHavePermission(AgencyRoster.*)`; handlers have ownership checks, state guards, concurrency handling, persistence, cache invalidation, logging). **One real shippable defect found & fixed:**
> - **NRE→500 on null reason/message** (same class as the prior "F126" fix that added `InviteGuideCommandValidator`): `RejectGuideApplicationCommand`, `RemoveGuideCommand`, and `ApplyToAgencyCommand` had **no validators**, and their domain methods (`AgencyApplication.Reject`, `AgencyAffiliation.Terminate`, `AgencyApplication.Create`) call `.Trim()` on a possibly-null string → a direct API call with a null/missing `reason`/`message` threw `NullReferenceException` → HTTP 500.
> - **Fix (Oracle-approved):** added `RejectGuideApplicationCommandValidator`, `RemoveGuideCommandValidator`, `ApplyToAgencyCommandValidator` (auto-discovered by the existing FluentValidation assembly scan); Reject/Remove now require a non-empty `Reason` (clean 400); `ApplyToAgencyCommand.Message` made `string?` and `AgencyApplication.Create` null-coalesces (`message?.Trim() ?? string.Empty`) since the apply message is optional. Build: `0 Error(s)`; LSP: 0 errors.
>
> **Documented follow-ups (out of scope — business-semantics/refactor, not the precedent-backed defect):** (a) Apply/Invite handlers don't verify the target agency/guide exists or is *approved* (only check caller's own `ProviderType`); (b) guide-side `GetMyInvitationsQuery` returns all statuses though the endpoint comment says "pending"; (c) no domain events on approve/reject/accept/decline; (d) sent/received invitations share one cache key (only collides if one user is both agency-owner and guide). These change business behavior and should be separately specced.
>
> ## ✅ RESOLVED — Round 2 (promoted business-semantics gaps GAP-a..d + 1 sub-gap)
> The four "documented follow-ups" above were **promoted to in-scope** and fixed in code (Oracle-reviewed). All build green (`0 Error(s)`) and Accounts.Tests.Unit 94/94 + Messaging.Tests.Unit 2/2 pass. Detailed GAP-N entries in **Section 5** below.


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

---

## 5. ✅ RESOLVED (Round 2) — promoted business-semantics gaps GAP-a..d + sub-gap

> All four "documented follow-ups" from the top banner were promoted to in-scope and implemented in code. Oracle-reviewed fix plan (binding adjustments: 422 not 409 for not-approved targets; per-direction Pending filter; notification-only events with role-assignment staying solely on `AgencyAffiliationCreated`; option-b two-cache-keys-one-tag). Build `0 Error(s)`; LSP 0 errors; **Accounts.Tests.Unit 94/94** (73 baseline + 21 new), **Messaging.Tests.Unit 2/2**.

### GAP-a — Apply/Invite do not verify target exists or is approved (and caller approval unchecked)
- **Status:** ✅ RESOLVED
- **Severity:** 🟠 (incorrect business semantics — allows applying to / inviting non-existent or unapproved counterparties; and Draft/Pending callers could act)
- **Type:** Missing existence + state-precondition guards
- **Layer(s):** Accounts.Application (command handlers), Accounts.Domain (errors)
- **Plan requirement:** §6 agency affiliations are between *approved* providers (an approved guide applies to an approved agency; an approved agency invites an approved guide).
- **Code reality (before):** `ApplyToAgencyCommandHandler` / `InviteGuideCommandHandler` checked only the caller's own `ProviderType` (NotAGuide / NotAnAgency); never loaded the target, never checked approval status, never checked caller `Status == Approved`.
- **Rule impact:** Rule 11 (existence/approved-state checks before cross-actor mutations).
- **Fix:** Added 5 errors to `Accounts.Domain\Errors\AgencyErrors.cs` (`AgencyNotFound`, `AgencyNotApproved`, `GuideNotFound`, `GuideNotApproved`, `CallerNotApproved`). Both handlers now, after the existing caller-type check: (1) caller `Status != Approved` → `CallerNotApproved` (Forbidden); (2) load target via `providerApplicationRepository.GetByUserIdAsync(targetUserId)` — null or wrong `Type` → `AgencyNotFound`/`GuideNotFound` (NotFound, collapsing missing+wrong-type to avoid type-probing); (3) target `Status != Approved` → `AgencyNotApproved`/`GuideNotApproved` (**UnprocessableEntity / 422**, per Oracle — well-formed request, referenced entity's business state blocks; BFF facade already maps 422 to a friendly "not yet approved" message). Existing affiliation/pending guards and persistence/eviction unchanged.
- **Resolution:** Handlers LSP-clean; 10 new handler-guard tests (5 each) assert Forbidden / NotFound / 422 / success paths.

### GAP-b — `GetMyInvitationsQuery` returns all statuses though endpoint documents "pending"
- **Status:** ✅ RESOLVED
- **Severity:** 🟡 (received-invitations view showed non-actionable accepted/declined/expired rows)
- **Type:** Missing status filter + missing input validation
- **Layer(s):** Accounts.Application (query handler + new validator)
- **Plan requirement:** Guide-side "received invitations" surface lists **pending** invitations to act on; agency-side "sent" surface lists all (it renders status + RespondedAt).
- **Code reality (before):** Handler called `GetByGuideUserIdAsync`/`GetByAgencyUserIdAsync` with `statusFilter: null` for **both** directions; `Direction` was a free-form string with no allow-list.
- **Rule impact:** Endpoint contract vs implementation drift.
- **Fix (per-direction, per Oracle — a global Pending filter would wrongly truncate the sent view):** received (default) → `GetByGuideUserIdAsync(userId, statusFilter: AgencyInvitationStatus.Pending, ct)`; sent → `GetByAgencyUserIdAsync(userId, statusFilter: null, ct)`. Added `GetMyInvitationsQueryValidator` (`RuleFor(x => x.Direction).NotEmpty().Must(d => d is "sent" or "received")`), auto-discovered by the FluentValidation assembly scan. Both live callers verified: `GuideAgencyEndpoints` → "received", `AgencyPublicEndpoints` → "sent".
- **Resolution:** 3 new query-handler tests assert received=Pending-only, sent=all-statuses, and per-direction repo routing.

### GAP-c — No domain events on approve/reject/accept/decline
- **Status:** ✅ RESOLVED
- **Severity:** 🟠 (responders are never notified of application/invitation outcomes)
- **Type:** Missing domain events + integration events + outbox converters + consumers
- **Layer(s):** Accounts.Domain (events), Accounts.Contracts (integration events), Accounts.Infrastructure (outbox converters), Messaging.Domain (enum), Messaging.Infrastructure (consumers)
- **Plan requirement:** §6 lifecycle transitions notify the counterparty (approved/rejected → notify guide; accepted/declined → notify agency).
- **Code reality (before):** `AgencyApplication.Approve/Reject` and `AgencyInvitation.Accept/Decline` mutated state with **no** `AddDomainEvent`; only affiliation create/terminate raised events. No notification path for these four transitions.
- **Rule impact:** Eventing pattern; outbox transactional guarantee; module isolation (public IDs only in integration payloads).
- **Fix (NOTIFICATION-ONLY — role assignment stays exclusively on `AgencyAffiliationCreated`/`AgencyGuideAffiliated`, no double-notify since `PublishAgencyAffiliationCreatedHandler` does not fan out notifications):**
  - **c1** 4 domain events (`AgencyApplicationApprovedDomainEvent`, `AgencyApplicationRejectedDomainEvent(...,Reason)`, `AgencyInvitationAcceptedDomainEvent`, `AgencyInvitationDeclinedDomainEvent`) in `Accounts.Domain\Events\Agency\`.
  - **c2** raised via `AddDomainEvent` inside each domain method's `if (Status != Pending) return;` guard (Reject carries trimmed `RejectionReason`).
  - **c3** 4 integration events (`: IntegrationEventBase`, action-past-tense timestamps) in `Accounts.Contracts\IntegrationEvents\`.
  - **c4** 4 outbox converter handlers appended to `Accounts.Infrastructure\EventHandlers\AgencyIntegrationConverters.cs` (`INotificationHandler<DomainEventNotification<…>>`; only `outbox.WriteAsync(...)`, no repo / no SaveChanges → outbox guarantee preserved).
  - **c5** `NotificationType` enum (Messaging.Domain) extended: `AgencyApplicationApproved=57, AgencyApplicationRejected=58, AgencyInvitationAccepted=59, AgencyInvitationDeclined=60`.
  - **c6** NEW `Messaging.Infrastructure\EventHandlers\AgencyResponseNotificationHandlers.cs`: 4 inbox-idempotent consumers (mirror `AgencyAffiliationCreatedNotificationHandler`) — Approved/Rejected notify GuideUserId, Accepted/Declined notify AgencyUserId; Rejected body includes the reason.
- **Resolution:** 8 new domain-event tests assert each transition raises the correct event with payload (and no event when the Status≠Pending guard short-circuits).

### GAP-d — Sent/received invitations share one cache key
- **Status:** ✅ RESOLVED
- **Severity:** 🟡 (cache collision only when a single userId is both an agency-owner and a guide — but YallaJo supports multi-role providers, so real)
- **Type:** Cache-key collision
- **Layer(s):** Accounts.Application (cache keys + query handler)
- **Plan requirement:** Sent and received invitation lists for the same user must not overwrite each other in cache.
- **Code reality (before):** `AccountsCacheKeys.AgencyInvitations(userId)` produced one key for both directions; a dual-role user's "sent" and "received" results clobbered each other.
- **Rule impact:** Cache correctness (C3).
- **Fix (Oracle option-b — two keys, one tag):** added overload `AgencyInvitations(Guid userId, string direction) => $"accounts:agency:{userId}:invitations:{direction}"`; handler now keys on `(userId, request.Direction)`. The eviction **tag** `AgencyInvitationsTag(userId)` is unchanged, so a single `RemoveByTagAsync` still invalidates both direction keys atomically — **zero eviction-site changes**.
- **Resolution:** Query-handler tests assert the two directions produce distinct cache keys.

### SUB-GAP — No unique constraint preventing duplicate concurrent pending Apply/Invite
- **Status:** ✅ RESOLVED
- **Severity:** 🟡 (race: two concurrent Apply/Invite both pass the `HasPending*` check → duplicate Pending rows)
- **Type:** Missing unique filtered index + migration
- **Layer(s):** Accounts.Infrastructure (EF configs + migration)
- **Plan requirement:** At most one *pending* application/invitation per (guide, agency) pair.
- **Code reality (before):** Only non-unique indexes existed; the no-duplicate rule was enforced solely by an app-level `HasPending*` read (TOCTOU race).
- **Fix:** `AgencyApplicationConfiguration` → unique filtered index on `(GuideUserId, AgencyUserId)` `HasFilter("[Status] = 'Pending'")` `UX_AgencyApplications_Pending_Guide_Agency`; `AgencyInvitationConfiguration` → unique filtered index on `(AgencyUserId, GuideUserId)` `HasFilter("[Status] = 'Pending'")` `UX_AgencyInvitations_Pending_Agency_Guide` (Status persisted as string). Migration `20260608170658_AddAgencyPendingUniqueIndexes` (Up creates both, Down drops both). All 3 Agency entities already carry `RowVersion` → `DbUpdateConcurrencyException` maps to Conflict.
- **Resolution:** Both EF configs LSP-clean; migration verified.

> **Note (eviction audit — NOT a gap):** the "two-sided eviction" concern was investigated and **downgraded**: invitation handlers already evict both parties' tags; applications/guides lists are agency-scoped only, so single-side eviction is correct. The only real cache bug was GAP-d.

> **Round-2 net:** all promoted gaps (GAP-a..d) + the pending-uniqueness sub-gap are RESOLVED in code with tests; build + Accounts/Messaging unit tests green.
