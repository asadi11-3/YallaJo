# Authorization-Cleanup Sprint — README

> **Sprint type:** **Cross-cutting cleanup + small middleware feature** (not a feature module).
> No new aggregates, no new domain events, no new background services. Pure technical-debt elimination + one bonus middleware.
> **Predecessor sprint:** Analytics (folder moves to `Agents/decisions/closed/Analytics/` on close).
> **This sprint covers:** §8.1 (8 `ICurrentUser` violations in ContentPlaces handlers) + §8.2 (28 endpoint authorization violations across 5 Presentation projects) + bonus `SeoRedirectMiddleware`.
> **Difficulty vs Booking:** ⚙️⚙️ (2/5) — refactor pattern is mechanical, but blast radius covers 5 modules so coordination matters.
> **Endpoint count:** **0 new endpoints**, **28 endpoints refactored**, **1 middleware added**.
> **Working-day estimate:** **10 working days × 4 devs ≈ 60 person-hours** (plus 12h Tech Lead review buffer).

---

## 0. Sprint Window & Hard Deadlines

| Milestone | Date | Notes |
|---|---|---|
| Pre-work cut | Fri 2027-02-26 17:00 AST | Coincides with Analytics retro |
| Kickoff | Sun 2027-02-28 09:00 AST | All-hands 30 min |
| Pre-work merge deadline | Tue 2027-03-02 17:00 AST | PW-1, PW-2 must be on `main` |
| Earliest task start | Wed 2027-03-03 09:00 AST | TASKs begin |
| Mid-sprint integration freeze | Sun 2027-03-07 17:00 AST | No new commits to `main` |
| Hard PR cutoff | Wed 2027-03-10 17:00 AST | Final PRs opened |
| Hard merge-to-main cutoff | Thu 2027-03-11 17:00 AST | Everything green |
| Sprint retro + demo | Fri 2027-03-12 11:00 AST | 30 min |

Working week Sun→Thu (5 days). Daily standup 09:30 AST 15 min hard cap.

---

## 1. Working Days & Person-Hour Budget

| Item | Value |
|---|---|
| Working days | 10 |
| Hours/day/dev | 6 (lighter than feature sprints) |
| Devs | 4 (Mahmoud, Fadwa, Mohammad, Tech Lead) |
| Total hours | 240 |
| Task hours | 60 |
| Review hours | 12 (Tech Lead concentrated review) |
| Ceremony hours (standups + retro) | 8 |
| **Buffer** | **160h** (sprint runs ahead of schedule — buffer used for Phase 3 prep) |

This is intentionally a **light-touch sprint** to let the team catch its breath between Analytics (Wave 6 closure) and Phase 3 kickoff. **Do not pad with new feature work** — use leftover hours for documentation, ADR-writing, and onboarding the next sprint.

---

## 2. Team Members & High-Level Allocation

| Name | Level | Tasks | Endpoints touched | Files touched | Est hours | Hard deadline |
|---|---|---|---|---|---|---|
| Mahmoud | Intermediate | T1 §8.1 ICurrentUser violations (8 ContentPlaces handlers) | 0 (only handlers) | ~8 handler files + ~12 test files | 16h | Sun 2027-03-07 |
| Fadwa | Beginner | T2 §8.2 AUTH_ONLY violations (14 endpoints across Auth/Accounts/Security) | 14 | 3 Presentation projects | 12h | Sun 2027-03-07 |
| Mohammad | Intermediate | T3 §8.2 STRING_POLICY + MISSING_METADATA violations (14 endpoints in ContentCore + ContentPlaces) | 14 | 2 Presentation projects | 16h | Wed 2027-03-10 |
| Mohammad (continued) | – | T4 BONUS `SeoRedirectMiddleware` + DI wiring | 0 (middleware) | 1 new middleware + DI + Program.cs | 12h | Wed 2027-03-10 |
| Tech Lead | – | Pre-work PW-1 + PW-2 + dedicated PR review | – | – | 12h | Continuous |

**Note:** Mohammad doubles up on T3 + T4 because both are STRING_POLICY-adjacent (T3 fixes string policies, T4 adds the redirect middleware which fits in the same auth-pipeline mental model).

---

## 3. Scope Manifest (Per-Task Endpoint/Handler List)

Full breakdown lives in the per-task files. Quick summary:

### TASK 1 (§8.1 — Mahmoud) — 8 `ICurrentUser` violations to remove
All in `ContentPlaces.Application/Businesses/Commands/`:
1. `ApproveBusinessCommandHandler` — admin action, should NOT inject `ICurrentUser`
2. `RejectBusinessCommandHandler` — admin action
3. `SuspendBusinessCommandHandler` — admin action
4. `ReinstateBusinessCommandHandler` — admin action
5. `AddBusinessStaffCommandHandler` — provider-self via Business ownership lookup, NOT `ICurrentUser` for `IsAuthenticated`
6. `AddBusinessAmenityCommandHandler` — same pattern
7. `RemoveBusinessAmenityCommandHandler` — same pattern
8. `SetBusinessHoursCommandHandler` — same pattern

**Fix pattern:** remove `ICurrentUser` injection, replace `currentUser.IsAuthenticated` checks with explicit authorization on the endpoint (`MustHavePermission(ContentPlacesFeatures.Business, AppAction.Approve)`), and replace any `currentUser.UserId` self-ownership with explicit `IBusinessRepository.GetByIdWithOwnerAsync` followed by `if (business.OwnerUserId != contextUserId) return Result.Failure(new Error("Business.OwnerMismatch", "..."), Outcome.Forbidden)` where `contextUserId` is passed as a command property (set by the endpoint from `ICurrentUser`).

### TASK 2 (§8.2 AUTH_ONLY — Fadwa) — 14 endpoints
Each has `.RequireAuthorization()` alone (no permission attribute). Fix: add explicit `.WithMetadata(new MustHavePermissionAttribute({Module}Features.X, AppAction.Y))`.

**Auth.Presentation (7 endpoints):**
1. POST `/auth/change-password`
2. POST `/auth/logout`
3. POST `/auth/logout-all`
4. GET `/auth/sessions`
5. DELETE `/auth/sessions/{id}`
6. POST `/auth/devices/{deviceId}/revoke`
7. GET `/auth/profile-summary` (if it exists; if not, drop and adjust count to 6)

**Accounts.Presentation (5 endpoints):**
1. GET `/profile`
2. PUT `/profile`
3. POST `/profile/avatar`
4. POST `/provider/apply`
5. GET `/provider/status`

**Security.Presentation (2 endpoints):**
1. GET `/admin/roles` (admin only)
2. GET `/admin/permissions` (admin only)

### TASK 3 (§8.2 STRING_POLICY + MISSING_METADATA — Mohammad) — 14 endpoints

**ContentCore.Presentation (9 STRING_POLICY):**
- Various endpoints using `.RequireAuthorization("permission:contentcore.categories.update")` literals — replace with `MustHavePermissionAttribute(ContentCoreFeatures.Categories, AppAction.Update)`.

**ContentPlaces.Presentation (5 STRING_POLICY):**
- Similar pattern.

**ContentPlaces.Presentation (4 MISSING_METADATA):**
- Endpoints with NO authorization metadata at all (silent 401). These are the most dangerous — add explicit `MustHavePermission` or `.AllowAnonymous()` per spec.

### TASK 4 (BONUS — Mohammad) — `SeoRedirectMiddleware`
- New middleware in `YallaJo.Api/Middleware/SeoRedirectMiddleware.cs`
- Reads `ContentSeo.SeoRedirects` table via `ISeoRedirectLookupService` (registered in ContentSeo.Infrastructure)
- 5-min sliding cache via `HybridCache`
- Skips `/api/*` paths (only frontend slug paths)
- Returns 301 (permanent) or 302 (temporary) per redirect type
- Increments `HitCount` async fire-and-forget (Channel pattern — see Analytics A-R1)
- Wires into `Program.cs` middleware pipeline at position 12 (after `UseAuthorization`, before module endpoints — per `YallaJo.md` middleware layer #12)

---

## 4. Critical Rules (additive to master `Phase1-Phase2-Completion-INDEX.md §4`)

See `02-critical-rules.md` for the full list. **TL;DR:**
- **CR-1:** Every endpoint MUST have `MustHavePermissionAttribute` OR `AllowAnonymous` — NEVER bare `.RequireAuthorization()`.
- **CR-2:** `ICurrentUser` ONLY for ownership/IDOR/self-edit/creator-stamp — never for `IsAuthenticated` checks.
- **CR-3:** Test coverage MUST verify the policy attribute is present (assert `MustHavePermissionAttribute` exists in endpoint metadata) — fixes are useless without tests preventing regression.

---

## 5. Cross-Module Dependencies

| Module | Files touched | Risk |
|---|---|---|
| Auth.Presentation | 7 endpoint registrations | Low — mechanical replacement |
| Accounts.Presentation | 5 endpoint registrations | Low |
| Security.Presentation | 2 endpoint registrations | Low |
| ContentCore.Presentation | 9 endpoint registrations | Medium — must verify `ContentCoreFeatures` has all needed permissions |
| ContentPlaces.Presentation | 9 endpoint registrations (5 STRING_POLICY + 4 MISSING_METADATA) | Medium — same |
| ContentPlaces.Application | 8 command handlers | Medium — handler signature changes propagate to all callers + tests |
| ContentSeo.Application | New `ISeoRedirectLookupService` interface | Low — additive |
| YallaJo.Api | Program.cs middleware pipeline | Medium — order matters |

**No new permissions** unless audit reveals a missing one. All required permissions should already exist in the catalogs from prior sprints. **Verification step in PW-1.**

---

## 6. Out of Scope

1. **New permission catalogs** — only fix existing.
2. **Reorganizing permission catalogs** across modules (defer to dedicated refactor if needed).
3. **Auth flow changes** — JWT, OAuth, refresh tokens stay as-is.
4. **Role assignment UI** — admin manages via existing Security module endpoints.
5. **Audit log retention enforcement** (deferred per Analytics ADR-007).
6. **Phase 3 features** — Packaging, Subscriptions, Loyalty, Referrals, Disputes, Accessibility Reviews.
7. **Phase 4 features** — Live Tracking, Recommendations, ChatBot, Smart Accessibility (UI).
8. **Notification preference UI in admin dashboard** (deferred, Phase 3).

---

## 7. Folder Lifecycle

On close (after Sun 2027-03-12 sign-off):
```powershell
Move-Item -LiteralPath "Agents\tasks\Authorization-Cleanup" -Destination "Agents\decisions\closed\Authorization-Cleanup"
```

Master `Phase1-Phase2-Completion-INDEX.md` §1 row gets 🟢 + closed/ link. `agent-context.md §8.1` and `§8.2` sections updated from violation lists to "✅ All cleaned in Authorization-Cleanup sprint 2027-03-12 — see closed/" — keep historical context but mark resolved.

---

## 8. Reading Order for New Joiners

1. `agent-context.md §0.3` (5 non-negotiable rules — auth is rule #1)
2. `agent-context.md §8` (the original violation audit — gives context why this sprint exists)
3. `Agents/authorization-refactor-plan.md` (the 3-phase fix plan written long ago)
4. `Agents/endpoint-authorization-audit.md` + `endpoint-violations.csv` (raw violation data)
5. `Phase1-Phase2-Completion-INDEX.md` (master sprint index)
6. THIS folder, in numeric order: `00-README → 01-pre-work → 02-critical-rules → 03-task-icurrentuser-violations → 04-task-endpoint-auth-violations → 05-task-seoredirect-middleware → 06-cross-cutting → 99-acceptance-gate`
