# Authorization-Cleanup Module — Combined Sprint Task File

> **Sprint window:** Mon 2027-02-28 → Thu 2027-03-11 (10 working days, 60 person-hours)
> **Combined from 8 separate files** in `Agents/tasks/Authorization-Cleanup/` for single-file review.

---

## Table of Contents

- [00-README](#00-readme)
- [01-pre-work](#01-pre-work)
- [02-critical-rules](#02-critical-rules)
- [03-task-icurrentuser-violations](#03-task-icurrentuser-violations)
- [04-task-endpoint-auth-violations](#04-task-endpoint-auth-violations)
- [05-task-seoredirect-middleware](#05-task-seoredirect-middleware)
- [06-cross-cutting](#06-cross-cutting)
- [99-acceptance-gate](#99-acceptance-gate)

---

<a id="00-readme"></a>

## 00-README

> Source: `Authorization-Cleanup/00-README.md`

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

---

<a id="01-pre-work"></a>

## 01-pre-work

> Source: `Authorization-Cleanup/01-pre-work.md`

# Authorization-Cleanup Sprint — Pre-Work

> **Tech Lead drives PW-1..PW-3.** Hard deadline **Tue 2027-03-02 17:00 AST**. NO task work begins until pre-work is on `main`.

This sprint is lighter than feature modules — only **3 pre-work items** (vs 7-10 for modules with new aggregates). All three are about preparing the audit and verifying assumptions before bulk-refactoring 5 Presentation projects + 8 handlers.

---

## PW-1 — Re-Verify Violation Catalog

**Goal:** the violation lists in §8.1 and §8.2 of `agent-context.md` are based on an audit several sprints old. Verify they are STILL accurate before bulk-fixing.

**Steps:**
1. Run ast-grep query against every Presentation project to find bare `.RequireAuthorization()`:
   ```powershell
   # From repo root:
   ast-grep --pattern '.RequireAuthorization()' --lang csharp `
       Auth.Presentation Accounts.Presentation Security.Presentation `
       ContentCore.Presentation ContentPlaces.Presentation ContentTours.Presentation `
       ContentBlogs.Presentation ContentSeo.Presentation Booking.Presentation `
       Finance.Presentation Social.Presentation Messaging.Presentation Analytics.Presentation
   ```
   Output: file:line list of every bare call. Compare with §8.2 expected 14 AUTH_ONLY rows. Adjust task list if any new violations appeared in interim sprints (or any old ones already fixed).

2. Run ast-grep query against every Presentation project to find string-policy usage:
   ```powershell
   ast-grep --pattern '.RequireAuthorization($POLICY)' --lang csharp `
       --rewrite '$POLICY' `
       Auth.Presentation Accounts.Presentation Security.Presentation `
       ContentCore.Presentation ContentPlaces.Presentation ContentTours.Presentation `
       ContentBlogs.Presentation ContentSeo.Presentation Booking.Presentation `
       Finance.Presentation Social.Presentation Messaging.Presentation Analytics.Presentation
   ```
   Output: list of string literals. Anything NOT in form `permission:...` is OK (e.g. `"AdminPolicy"` if used). Compare with §8.2 STRING_POLICY rows expected 14.

3. Run grep for `ICurrentUser` injections in command handlers — sample 50 random handlers across all Application projects. Verify only those with documented ownership/IDOR/self-edit/creator-stamp use case have it. Any handler that just does `if (!currentUser.IsAuthenticated) return Result.Failure(...)` is a violation — these should rely on the endpoint's authorization metadata, not handler-level checks.

4. Update `Agents/endpoint-violations.csv` with the FRESH list:
   ```csv
   Project,File,Endpoint,ViolationType,Severity,Notes
   ContentPlaces.Presentation,PlacesEndpoints.cs,GET /places/{id}/businesses,MISSING_METADATA,High,No auth attribute at all
   ...
   ```

5. Generate `Agents/endpoint-violations-2027-02-28.csv` (date-stamped fresh copy) and commit alongside the older one (don't overwrite — historical context).

**Acceptance:**
- Updated CSV merged to `main` with row count == current violation count.
- Task 2 + Task 3 + Task 1 owners read the CSV and confirm scope MATCHES estimate (16h / 12h / 16h). Re-allocate hours if drift.

**Estimated hours:** 3h (Tech Lead).

---

## PW-2 — Verify Permission Catalog Coverage

**Goal:** confirm every endpoint we're about to fix has an EXISTING permission to attach. If any endpoint needs a NEW feature/action that doesn't exist in catalogs, we either (a) add it as a tiny PW-3, or (b) update the violation entry to use the closest existing permission.

**Steps:**
1. For each endpoint in the violation CSV, identify the intended `{Module}Features.X` + `AppAction.Y` pair.
2. Cross-check against the existing catalog file:
   - `Auth.Contracts/Authorization/AuthFeatures.cs` + `AuthPermissionCatalog.cs`
   - `Accounts.Contracts/...`
   - `Security.Contracts/...`
   - `ContentCore.Contracts/...`
   - `ContentPlaces.Contracts/...`
   - `ContentTours.Contracts/...`
3. For each missing pair, decide:
   - **Add to catalog (preferred)** if the action is conceptually new (e.g. `Auth.Sessions` feature + `Revoke` action might not exist yet).
   - **Map to existing closest match** if the action is just a synonym (e.g. `Profile.View` ≈ `Profile.Read`).

4. Produce `Agents/permission-coverage-gaps-2027-02-28.md` listing each new permission to add (with proposed feature/action name + justification) OR each remap decision.

**Acceptance:**
- All 28 §8.2 endpoints map cleanly to existing or newly-added permissions.
- Boot log will show updated counts in each module catalog (e.g. `Auth permissions inserted/verified: 18` if 2 added).
- No endpoint left with "we'll figure it out later" — every decision is in the gap doc.

**Estimated hours:** 4h (Tech Lead).

**Likely outcome (educated guess):** 90% of endpoints already have matching permissions; 1-3 new permissions needed across all catalogs. Add them in PW-2 itself — don't create a separate PW-4.

---

## PW-3 — Sanity Test Skeleton: "Every endpoint has auth metadata"

**Goal:** ship the regression-prevention test BEFORE the fixes, so we can confirm RED → GREEN as fixes land.

**Steps:**

1. Add a new integration test project (or extend existing) `tests/Authorization.IntegrationTests/` with one file:

```csharp
// tests/Authorization.IntegrationTests/EndpointAuthorizationMetadataTests.cs
[Collection("WebApi")]
public sealed class EndpointAuthorizationMetadataTests(WebApiFactory factory)
{
    [Fact]
    public void Every_registered_endpoint_has_either_MustHavePermission_or_AllowAnonymous()
    {
        using var scope = factory.Services.CreateScope();
        var endpointDataSource = scope.ServiceProvider
            .GetRequiredService<EndpointDataSource>();

        var offenders = new List<string>();
        foreach (var endpoint in endpointDataSource.Endpoints.OfType<RouteEndpoint>())
        {
            var pattern = endpoint.RoutePattern.RawText ?? "<no-route>";
            var allowAnon = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
            var hasMustHavePermission = endpoint.Metadata.GetMetadata<MustHavePermissionAttribute>() is not null;
            // Health checks + Swagger UI + internal infra endpoints excluded:
            if (pattern.StartsWith("/health") || pattern.StartsWith("/swagger") || pattern.StartsWith("/_framework"))
                continue;
            if (!allowAnon && !hasMustHavePermission)
                offenders.Add(pattern);
        }

        offenders.Should().BeEmpty(
            $"Every endpoint must have either [AllowAnonymous] or [MustHavePermission(...)]. Offenders:\n{string.Join("\n", offenders)}");
    }

    [Fact]
    public void No_endpoint_uses_string_based_RequireAuthorization_with_permission_prefix()
    {
        using var scope = factory.Services.CreateScope();
        var endpointDataSource = scope.ServiceProvider.GetRequiredService<EndpointDataSource>();
        var offenders = new List<string>();
        foreach (var endpoint in endpointDataSource.Endpoints.OfType<RouteEndpoint>())
        {
            var pattern = endpoint.RoutePattern.RawText ?? "<no-route>";
            // AuthorizeAttribute with a string policy:
            var authAttrs = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            foreach (var attr in authAttrs)
            {
                if (attr.Policy is { Length: > 0 } policy && policy.StartsWith("permission:", StringComparison.OrdinalIgnoreCase))
                    offenders.Add($"{pattern} -> {policy}");
            }
        }
        offenders.Should().BeEmpty(
            $"String-based 'permission:...' policy names are forbidden. Use MustHavePermissionAttribute. Offenders:\n{string.Join("\n", offenders)}");
    }
}
```

2. Run the test. EXPECT it to fail RED with current state (28 offenders + N string-policies). This confirms test wiring is correct.

3. Add a third test sanity check: every `MustHavePermissionAttribute` references a permission that the registered `IPermissionCatalog.GetAllPermissions()` actually contains. Prevents typos like `AppAction.Updaet`:

```csharp
[Fact]
public void Every_MustHavePermission_attribute_references_a_registered_permission()
{
    using var scope = factory.Services.CreateScope();
    var endpointDataSource = scope.ServiceProvider.GetRequiredService<EndpointDataSource>();
    var catalogs = scope.ServiceProvider.GetServices<IPermissionCatalog>();
    var knownPermissions = catalogs.SelectMany(c => c.GetAllPermissions())
        .Select(p => PermissionPolicyNames.Build(p.Feature, p.Action))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var offenders = new List<string>();
    foreach (var endpoint in endpointDataSource.Endpoints.OfType<RouteEndpoint>())
    {
        var pattern = endpoint.RoutePattern.RawText ?? "<no-route>";
        var meta = endpoint.Metadata.GetMetadata<MustHavePermissionAttribute>();
        if (meta is null) continue;
        var policy = PermissionPolicyNames.Build(meta.Feature, meta.Action);
        if (!knownPermissions.Contains(policy))
            offenders.Add($"{pattern} -> {policy}");
    }
    offenders.Should().BeEmpty(
        $"MustHavePermission references unknown permission. Offenders:\n{string.Join("\n", offenders)}");
}
```

4. Test project csproj references: `YallaJo.Api`, `xunit 2.9.3`, `FluentAssertions 7.0.0`, `Microsoft.AspNetCore.Mvc.Testing 9.0.15`.

**Acceptance:**
- 3 tests RED locally with current state.
- Tests merged to `main` in red (intentional — they're the to-do list).
- CI is told to allow these specific tests to fail with allow-list, OR mark them `[Trait("category","authorization-debt")]` and exclude from CI failure-on-red until sprint close.

**Estimated hours:** 5h (Tech Lead, includes harnessing WebApiFactory if not already present).

---

## Pre-Work Summary

| PW | Owner | Hours | Hard deadline |
|---|---|---|---|
| PW-1 Re-verify violation catalog | Tech Lead | 3h | Tue 2027-03-02 17:00 |
| PW-2 Verify permission catalog coverage | Tech Lead | 4h | Tue 2027-03-02 17:00 |
| PW-3 Sanity test skeleton (3 tests, red) | Tech Lead | 5h | Tue 2027-03-02 17:00 |
| **Total** | | **12h** | |

Once all PW merged, kickoff happens Wed 2027-03-03 09:00 AST and task owners fork into parallel work.

---

<a id="02-critical-rules"></a>

## 02-critical-rules

> Source: `Authorization-Cleanup/02-critical-rules.md`

# Authorization-Cleanup — Critical Rules

> Additive to **master `Phase1-Phase2-Completion-INDEX.md §4`**. This file documents the auth-specific patterns Tech Lead enforces during PR review.

---

## AC-R1 — Every Endpoint Has Explicit Authorization

The ONLY two valid endpoint-authorization patterns:

### ✅ Authenticated + permission-gated
```csharp
group.MapPost("/admin/businesses/{id:guid}/approve", ApproveBusinessAsync)
    .WithMetadata(new MustHavePermissionAttribute(
        ContentPlacesFeatures.Business,
        AppAction.Approve))
    .WithName("ApproveBusiness");
```

### ✅ Truly public
```csharp
group.MapPost("/api/v1/payments/webhook", PaymentsWebhookAsync)
    .AllowAnonymous()
    .WithName("PaymentsWebhook");
```

### ❌ Forbidden (any of these = PR rejected)
```csharp
// AUTH_ONLY — no permission check, just "must be logged in":
.RequireAuthorization()

// STRING_POLICY — magic-string policy name, can drift from catalog:
.RequireAuthorization("permission:contentplaces.places.update")

// MISSING_METADATA — no auth anything, silent default behavior:
group.MapGet("/places/{id}", GetPlaceAsync);  // No metadata at all
```

**Reason:** `MustHavePermissionAttribute` is the SINGLE source of truth that connects an endpoint to a `{Module}Features.X + AppAction.Y` pair. The pair is verified against the module's `IPermissionCatalog` at boot. Strings drift; attributes don't.

---

## AC-R2 — `ICurrentUser` Usage Rules

`ICurrentUser` should appear **ONLY** in handlers that need to:

1. **Self-edit** — comparing `currentUser.UserId` against the entity's `UserId` for "is this MY resource" checks.
2. **Ownership/IDOR** — same idea but on aggregates like `Business.OwnerUserId`.
3. **Creator-stamp** — recording who created a resource on the entity itself (`tour.CreatedByUserId = currentUser.UserId`).
4. **Audit log enrichment** — `auditLog.RecordedByUserId = currentUser.UserId`.

It should **NEVER** appear in handlers for:
- `if (!currentUser.IsAuthenticated) return Forbidden` — that's the endpoint's `MustHavePermission` job.
- `if (!currentUser.HasPermission("X")) return Forbidden` — same, push to endpoint.
- "Convenience" — never inject `ICurrentUser` just because you might need it.

### Migration Pattern for §8.1 Handlers

**Before (violation):**
```csharp
public sealed class ApproveBusinessCommandHandler(
    IBusinessRepository businessRepo,
    IContentPlacesUnitOfWork uow,
    ICurrentUser currentUser,  // ❌ injected for IsAuthenticated check
    ILogger<ApproveBusinessCommandHandler> logger)
{
    public async Task<Result> Handle(ApproveBusinessCommand command, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)  // ❌ should be at endpoint level
            return Result.Failure(new Error("Auth.Unauthenticated", "..."), Outcome.Forbidden);
        // ... business logic ...
    }
}
```

**After (correct):**
```csharp
public sealed class ApproveBusinessCommandHandler(
    IBusinessRepository businessRepo,
    IContentPlacesUnitOfWork uow,
    ILogger<ApproveBusinessCommandHandler> logger)  // no ICurrentUser
{
    public async Task<Result> Handle(ApproveBusinessCommand command, CancellationToken ct)
    {
        // No auth check here — endpoint already enforced MustHavePermission(Business, Approve).
        // ... business logic ...
    }
}
```

**Endpoint changes:**
```csharp
group.MapPost("/admin/businesses/{id:guid}/approve", ApproveBusinessAsync)
    .WithMetadata(new MustHavePermissionAttribute(
        ContentPlacesFeatures.Business,
        AppAction.Approve));  // ← THIS is what does the auth gate
```

### When the handler still NEEDS the current user (creator-stamp)

If the command needs to record who took the action (admin user ID), the endpoint passes it through the command, not via `ICurrentUser` inside the handler:

```csharp
private static async Task<IResult> ApproveBusinessAsync(
    Guid id,
    ICurrentUser currentUser,    // ← OK at endpoint to FORWARD to command
    IMediator mediator,
    CancellationToken ct)
{
    var command = new ApproveBusinessCommand(
        BusinessId: id,
        ApprovedByUserId: currentUser.UserId!.Value);
    var result = await mediator.Send(command, ct);
    return result.ToApiResult();
}
```

The handler signature now accepts `ApprovedByUserId` as a normal command property — clean, testable, no implicit ambient state.

---

## AC-R3 — Test Coverage Mandate

**Every fix is paired with a test.** The PW-3 metadata-presence tests catch missing attributes generically, but per-fix tests prove specific intent:

```csharp
public sealed class ApproveBusinessEndpointAuthorizationTests
{
    [Fact]
    public void Endpoint_requires_business_approve_permission()
    {
        var endpoint = EndpointInspector.GetEndpoint("ApproveBusiness");
        var attr = endpoint.Metadata.GetMetadata<MustHavePermissionAttribute>();
        attr.Should().NotBeNull();
        attr!.Feature.Should().Be(ContentPlacesFeatures.Business);
        attr.Action.Should().Be(AppAction.Approve);
    }
}
```

Helper `EndpointInspector` lives in `tests/Shared.Tests/Endpoints/EndpointInspector.cs` (added in this sprint).

---

## AC-R4 — `MustHavePermissionAttribute` Construction

Always pass **typed feature constants + typed AppAction enum**, never raw strings:

```csharp
// ✅ Good
new MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Approve)

// ❌ Bad (string-policy in disguise)
new MustHavePermissionAttribute("ContentPlaces.Business", "Approve")
```

The constants come from each module's `Contracts/Authorization/` folder.

---

## AC-R5 — Error Code Discipline

When a handler rejects an authorization-related condition (e.g. IDOR mismatch), use these error codes:

| Code | Outcome | When |
|---|---|---|
| `Auth.Unauthenticated` | 401 | NEVER from handler — only middleware |
| `Business.OwnerMismatch` | 403 | Handler IDOR check — caller is auth'd but not the owner |
| `Tour.OwnerMismatch` | 403 | Same pattern |
| `Booking.OwnerMismatch` | 403 | Same |
| `Profile.SelfOnly` | 403 | Handler accessing someone else's profile |

`Outcome.Forbidden` maps to 403; never use 401 from handler code (that's the JWT middleware's responsibility).

---

## AC-R6 — Middleware Order (relevant to TASK 4)

`SeoRedirectMiddleware` placement in `Program.cs` per `YallaJo.md` middleware-pipeline-layer ordering:

```csharp
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseRequestLocalization();
app.UseResponseCompression();
app.UseCors("YallaJoPolicy");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();         // ← layer 11
app.UseMiddleware<SeoRedirectMiddleware>();  // ← layer 12 — NEW (TASK 4)
// ... module endpoints ...
```

**Why after `UseAuthorization`:** redirects shouldn't bypass auth — if someone hits an old slug for a private resource, the redirect happens, then auth still gates the destination.

**Why before module endpoints:** redirects short-circuit before routing kicks in.

**Skip rule:** middleware skips when `context.Request.Path.StartsWithSegments("/api")` — only frontend slug paths get redirect lookup.

---

## AC-R7 — Cache Discipline for `SeoRedirectMiddleware`

- `HybridCache` key: `seo-redirect:{lowercase-path}` (case-insensitive lookups).
- TTL: 5 minutes sliding.
- Tag: `seo-redirects` (invalidated by `ContentSeo.SeoRedirects` upsert/delete handlers — already wired in ContentSeo sprint).
- Cache miss = DB lookup; cache hit = serve from memory.
- `HitCount` increment is async fire-and-forget via `Channel<RedirectHit>` (Singleton, capacity 1000, drainer BG service flushes every 10 seconds to `ContentSeo.SeoRedirects.HitCount`).

---

## AC-R8 — Pre-PR Self-Check

Before opening any PR in this sprint, run from the changed module's root:

```powershell
# 1. Build clean
dotnet build {ChangedProject}.csproj

# 2. Run the metadata-presence tests
dotnet test tests/Authorization.IntegrationTests/Authorization.IntegrationTests.csproj `
    --filter "FullyQualifiedName~EndpointAuthorizationMetadataTests"

# 3. Run module-specific tests
dotnet test tests/{ChangedModule}.Tests.Unit/{ChangedModule}.Tests.Unit.csproj
```

All three must be green. PR description includes:
1. Which violations from PW-1 CSV are being fixed (link line numbers in CSV).
2. Which tests now go GREEN (was RED before).
3. Any new permissions added to catalog (link to PW-2 gap doc decision).

---

<a id="03-task-icurrentuser-violations"></a>

## 03-task-icurrentuser-violations

> Source: `Authorization-Cleanup/03-task-icurrentuser-violations.md`

# TASK 1 — Fix §8.1 `ICurrentUser` Violations in ContentPlaces Handlers

> **Owner:** Mahmoud (Intermediate) — **Hours:** 16h — **Hard deadline:** Sun **2027-03-07 17:00**
> **Earliest start:** Wed 2027-03-03 09:00 (after PW-1..PW-3 merged)
> **Endpoints:** 0 (handlers only). **Files touched:** 8 handlers + ~16 test files + ~8 endpoint files (forwarding `currentUser.UserId` via command property).
> **Depends on:** PW-1 (fresh violation CSV), PW-2 (permission coverage verified), PW-3 (regression tests in place — RED initially).

This task removes `ICurrentUser` injection from 8 ContentPlaces command handlers and migrates each to either: (a) pure handlers where authorization is purely the endpoint's job, or (b) handlers receiving the actor's UserId as a command property forwarded by the endpoint.

---

## 1. The 8 Handlers

All under `ContentPlaces.Application/Businesses/Commands/`:

| # | Handler | Reason it has `ICurrentUser` today | Fix pattern |
|---|---|---|---|
| 1 | `ApproveBusinessCommandHandler` | Checks `IsAuthenticated` + records `ApprovedByUserId` | Remove injection. Endpoint forwards `currentUser.UserId` via `ApprovedByUserId` command property. |
| 2 | `RejectBusinessCommandHandler` | Same pattern | Same: command property `RejectedByUserId` |
| 3 | `SuspendBusinessCommandHandler` | Same pattern | Command property `SuspendedByUserId` |
| 4 | `ReinstateBusinessCommandHandler` | Same pattern | Command property `ReinstatedByUserId` |
| 5 | `AddBusinessStaffCommandHandler` | Provider-self check + creator stamp | Remove injection. Endpoint forwards `currentUser.UserId` as `ActingUserId`. Handler does IDOR check `if (business.OwnerUserId != command.ActingUserId) Forbidden`. |
| 6 | `AddBusinessAmenityCommandHandler` | Same pattern | Same: `ActingUserId` IDOR check |
| 7 | `RemoveBusinessAmenityCommandHandler` | Same pattern | Same |
| 8 | `SetBusinessHoursCommandHandler` | Same pattern | Same |

---

## 2. Worked Example — `ApproveBusinessCommandHandler`

### Before

```csharp
// ContentPlaces.Application/Businesses/Commands/ApproveBusinessCommandHandler.cs
public sealed class ApproveBusinessCommandHandler(
    IBusinessRepository businessRepo,
    IContentPlacesUnitOfWork uow,
    ICurrentUser currentUser,                 // ← VIOLATION
    HybridCache cache,
    ILogger<ApproveBusinessCommandHandler> logger)
    : IRequestHandler<ApproveBusinessCommand, Result>
{
    public async Task<Result> Handle(ApproveBusinessCommand command, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)     // ← VIOLATION
            return Result.Failure(new Error("Auth.Unauthenticated", "..."), Outcome.Forbidden);

        var business = await businessRepo.GetByIdAsync(command.BusinessId, ct);
        if (business is null) return Result.Failure(new Error("Business.NotFound", "..."), Outcome.NotFound);

        var result = business.Approve(currentUser.UserId!.Value);  // ← creator-stamp via ambient
        if (result.IsFailure) return result;

        await uow.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync($"business:{command.BusinessId}", ct);
        return Result.Success();
    }
}
```

### Command (Application/Businesses/Commands/ApproveBusinessCommand.cs)

```csharp
// Before
public sealed record ApproveBusinessCommand(Guid BusinessId) : IRequest<Result>;

// After (adds ApprovedByUserId set from endpoint)
public sealed record ApproveBusinessCommand(
    Guid BusinessId,
    Guid ApprovedByUserId) : IRequest<Result>;
```

### Validator (if present)

```csharp
public sealed class ApproveBusinessCommandValidator : AbstractValidator<ApproveBusinessCommand>
{
    public ApproveBusinessCommandValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.ApprovedByUserId).NotEmpty();  // ← new
    }
}
```

### After (handler — no `ICurrentUser`)

```csharp
public sealed class ApproveBusinessCommandHandler(
    IBusinessRepository businessRepo,
    IContentPlacesUnitOfWork uow,
    HybridCache cache,
    ILogger<ApproveBusinessCommandHandler> logger)
    : IRequestHandler<ApproveBusinessCommand, Result>
{
    public async Task<Result> Handle(ApproveBusinessCommand command, CancellationToken ct)
    {
        // No auth check here — endpoint already enforced MustHavePermission(Business, Approve).
        var business = await businessRepo.GetByIdAsync(command.BusinessId, ct);
        if (business is null)
            return Result.Failure(new Error("Business.NotFound", $"Business {command.BusinessId} not found"), Outcome.NotFound);

        var result = business.Approve(command.ApprovedByUserId);  // ← from command, not ambient
        if (result.IsFailure) return result;

        await uow.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync($"business:{command.BusinessId}", ct);
        logger.LogInformation("Business {BusinessId} approved by {UserId}", command.BusinessId, command.ApprovedByUserId);
        return Result.Success();
    }
}
```

### Endpoint (ContentPlaces.Presentation/Endpoints/BusinessAdminEndpoints.cs)

```csharp
// Before
group.MapPost("/admin/businesses/{id:guid}/approve", async (Guid id, IMediator mediator, CancellationToken ct) =>
{
    var command = new ApproveBusinessCommand(id);
    var result = await mediator.Send(command, ct);
    return result.ToApiResult();
})
.RequireAuthorization();  // ← ALSO violates §8.2 — fix in TASK 3? No — Mohammad fixes Auth/Accounts/Security/ContentCore/ContentPlaces in TASK 3.
                          //    But since we're already in this endpoint, ALSO add MustHavePermission here as part of this PR to keep one cohesive change.

// After
group.MapPost("/admin/businesses/{id:guid}/approve",
    async (Guid id, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
    {
        var actingUserId = currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user missing UserId");
        var command = new ApproveBusinessCommand(id, ApprovedByUserId: actingUserId);
        var result = await mediator.Send(command, ct);
        return result.ToApiResult();
    })
    .WithMetadata(new MustHavePermissionAttribute(
        ContentPlacesFeatures.Business,
        AppAction.Approve))   // ← explicit policy attribute
    .WithName("ApproveBusiness");
```

The `?? throw` is intentional — if `MustHavePermission` did its job, `UserId` will be non-null. If somehow null, fail loud rather than silently insert `Guid.Empty`.

---

## 3. Pattern for Provider-Owned Handlers (#5..#8)

For handlers that need **IDOR** (caller must own the business), the pattern is:

### Command (adds `ActingUserId`)

```csharp
public sealed record AddBusinessAmenityCommand(
    Guid BusinessId,
    string AmenityType,
    string? Description,
    Guid ActingUserId) : IRequest<Result>;
```

### Handler

```csharp
public sealed class AddBusinessAmenityCommandHandler(
    IBusinessRepository businessRepo,
    IContentPlacesUnitOfWork uow,
    HybridCache cache,
    ILogger<AddBusinessAmenityCommandHandler> logger)
    : IRequestHandler<AddBusinessAmenityCommand, Result>
{
    public async Task<Result> Handle(AddBusinessAmenityCommand command, CancellationToken ct)
    {
        var business = await businessRepo.GetByIdWithOwnerAsync(command.BusinessId, ct);
        if (business is null)
            return Result.Failure(new Error("Business.NotFound", "..."), Outcome.NotFound);

        // IDOR check — caller must own the business (or admin with override permission)
        if (business.OwnerUserId != command.ActingUserId)
            return Result.Failure(new Error("Business.OwnerMismatch", $"User {command.ActingUserId} is not the owner of business {command.BusinessId}"), Outcome.Forbidden);

        var result = business.AddAmenity(command.AmenityType, command.Description);
        if (result.IsFailure) return result;

        await uow.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync($"business:{command.BusinessId}:amenities", ct);
        return Result.Success();
    }
}
```

### Endpoint

```csharp
group.MapPost("/businesses/{id:guid}/amenities",
    async (Guid id, AddBusinessAmenityRequest request, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
    {
        var actingUserId = currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user missing UserId");
        var command = new AddBusinessAmenityCommand(
            BusinessId: id,
            AmenityType: request.AmenityType,
            Description: request.Description,
            ActingUserId: actingUserId);
        var result = await mediator.Send(command, ct);
        return result.ToApiResult();
    })
    .WithMetadata(new MustHavePermissionAttribute(
        ContentPlacesFeatures.Business,
        AppAction.Update));
```

**Admin override:** if admins should also be able to add amenities to any business (likely yes per PDF 2), the handler IDOR check needs to allow that. Two options:

1. **Add a Boolean `IsAdmin` to the command** (set from endpoint via `currentUser.IsInRole("Admin")`) and short-circuit IDOR if true.
2. **Use two separate endpoints**: `/admin/businesses/{id}/amenities` with `MustHavePermission(AdminBusinessManagement, AppAction.Update)` skipping IDOR + `/businesses/{id}/amenities` for owners with IDOR. Cleaner separation.

**Decision:** prefer option 2 unless it's already a single endpoint shared in current code (then option 1). PR description documents decision.

---

## 4. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Update `ApproveBusinessCommand` + handler + endpoint + test fixture | 2 | 2027-03-03 |
| 2 | Update `RejectBusinessCommandHandler` (parallel pattern) | 1.5 | 2027-03-04 |
| 3 | Update `SuspendBusinessCommandHandler` + `ReinstateBusinessCommandHandler` | 2 | 2027-03-04 |
| 4 | Update `AddBusinessStaffCommandHandler` (IDOR pattern + tests) | 2 | 2027-03-05 |
| 5 | Update `AddBusinessAmenityCommandHandler` + `RemoveBusinessAmenityCommandHandler` | 2 | 2027-03-05 |
| 6 | Update `SetBusinessHoursCommandHandler` | 1.5 | 2027-03-06 |
| 7 | Per-handler unit tests (16 — one per handler covering happy + IDOR-fail) | 3 | 2027-03-06 |
| 8 | Per-endpoint integration tests (8 — `MustHavePermission` attribute verification + 401/403/200 wiring) | 1.5 | 2027-03-07 |
| 9 | PR + review fixes | 0.5 | 2027-03-07 |
| **Total** | | **16h** | **Sun 2027-03-07** |

---

## 5. Acceptance

1. **Zero `ICurrentUser` injections** in 8 listed handlers — `rg "ICurrentUser" ContentPlaces.Application/Businesses/Commands/` returns 0 lines.
2. **8 endpoints have `MustHavePermissionAttribute`** — PW-3 metadata test no longer flags them.
3. **16+ unit tests pass** — happy path + IDOR-fail per handler.
4. **8 integration tests pass** — endpoint-level auth metadata verification.
5. **No regression** — full `dotnet test ContentPlaces.Tests.Unit` green.
6. **PR description** lists each violation row from PW-1 CSV with line-number link and confirms it's resolved.

---

<a id="04-task-endpoint-auth-violations"></a>

## 04-task-endpoint-auth-violations

> Source: `Authorization-Cleanup/04-task-endpoint-auth-violations.md`

# TASK 2 + TASK 3 — Fix §8.2 Endpoint Authorization Violations

This file covers the two parallel sub-tasks fixing the 28 endpoint-level violations across 5 Presentation projects.

---

## TASK 2 — AUTH_ONLY violations (14 endpoints)

> **Owner:** Fadwa (Beginner) — **Hours:** 12h — **Hard deadline:** Sun **2027-03-07 17:00**
> **Earliest start:** Wed 2027-03-03 09:00
> **Scope:** Auth.Presentation (7) + Accounts.Presentation (5) + Security.Presentation (2)
> **Pattern:** Replace `.RequireAuthorization()` with `.WithMetadata(new MustHavePermissionAttribute(...))`.

### 1. Auth.Presentation (7 endpoints)

| # | Endpoint | Replace with |
|---|---|---|
| 1 | POST `/auth/change-password` | `MustHavePermission(AuthFeatures.Account, AppAction.Update)` |
| 2 | POST `/auth/logout` | `MustHavePermission(AuthFeatures.Session, AppAction.Delete)` |
| 3 | POST `/auth/logout-all` | `MustHavePermission(AuthFeatures.Session, AppAction.Delete)` |
| 4 | GET `/auth/sessions` | `MustHavePermission(AuthFeatures.Session, AppAction.Read)` |
| 5 | DELETE `/auth/sessions/{id}` | `MustHavePermission(AuthFeatures.Session, AppAction.Delete)` |
| 6 | POST `/auth/devices/{deviceId}/revoke` | `MustHavePermission(AuthFeatures.Device, AppAction.Delete)` |
| 7 | GET `/auth/profile-summary` (if exists) | `MustHavePermission(AuthFeatures.Account, AppAction.Read)` |

**If `AuthFeatures.Session` or `AuthFeatures.Device` don't exist yet, PW-2 should have flagged them. Add to `AuthFeatures` + `AuthPermissionCatalog` as part of this PR.**

### 2. Accounts.Presentation (5 endpoints)

| # | Endpoint | Replace with |
|---|---|---|
| 1 | GET `/profile` | `MustHavePermission(AccountsFeatures.Profile, AppAction.Read)` |
| 2 | PUT `/profile` | `MustHavePermission(AccountsFeatures.Profile, AppAction.Update)` |
| 3 | POST `/profile/avatar` | `MustHavePermission(AccountsFeatures.Profile, AppAction.Update)` |
| 4 | POST `/provider/apply` | `MustHavePermission(AccountsFeatures.ProviderApplication, AppAction.Create)` |
| 5 | GET `/provider/status` | `MustHavePermission(AccountsFeatures.ProviderApplication, AppAction.Read)` |

### 3. Security.Presentation (2 endpoints)

| # | Endpoint | Replace with |
|---|---|---|
| 1 | GET `/admin/roles` | `MustHavePermission(SecurityFeatures.Role, AppAction.Read)` |
| 2 | GET `/admin/permissions` | `MustHavePermission(SecurityFeatures.Permission, AppAction.Read)` |

### 4. Refactor Pattern (per endpoint)

```csharp
// Before
group.MapGet("/profile", GetProfileAsync).RequireAuthorization();

// After
group.MapGet("/profile", GetProfileAsync)
    .WithMetadata(new MustHavePermissionAttribute(
        AccountsFeatures.Profile,
        AppAction.Read));
```

If the handler also injected `ICurrentUser` for self-only access, that injection stays — it's a valid ownership/self-edit use case (AC-R2 rule).

### 5. TASK 2 WBS

| # | Step | Hours |
|---|---|---|
| 1 | Auth.Presentation — 7 endpoints + tests | 4 |
| 2 | Accounts.Presentation — 5 endpoints + tests | 3 |
| 3 | Security.Presentation — 2 endpoints + tests | 1.5 |
| 4 | Add any missing AuthFeatures/SecurityFeatures items (PW-2 outcome) | 1 |
| 5 | Per-endpoint integration tests (`MustHavePermission` attribute verification) | 2 |
| 6 | PR + review fixes | 0.5 |
| **Total** | | **12h** |

### 6. TASK 2 Acceptance

- All 14 endpoints listed above have `MustHavePermissionAttribute` attached.
- PW-3 sanity test count of `AUTH_ONLY` violations drops by 14 (this task's full scope).
- All endpoint integration tests pass.

---

## TASK 3 — STRING_POLICY + MISSING_METADATA violations (14 endpoints)

> **Owner:** Mohammad (Intermediate) — **Hours:** 16h — **Hard deadline:** Wed **2027-03-10 17:00**
> **Earliest start:** Wed 2027-03-03 09:00 (parallel with TASK 1+2)
> **Scope:** ContentCore.Presentation (9 STRING_POLICY) + ContentPlaces.Presentation (5 STRING_POLICY + 4 MISSING_METADATA)
> **Pattern:** Replace string-based policies with typed `MustHavePermissionAttribute`. Add explicit metadata to silent endpoints.

### 1. ContentCore.Presentation (9 STRING_POLICY)

Each endpoint uses `.RequireAuthorization("permission:contentcore.{feature}.{action}")` — replace with typed attribute. PW-1 CSV has exact line numbers.

| # | Endpoint (typical) | Replacement |
|---|---|---|
| 1 | POST `/categories` | `MustHavePermission(ContentCoreFeatures.Categories, AppAction.Create)` |
| 2 | PUT `/categories/{id}` | `MustHavePermission(ContentCoreFeatures.Categories, AppAction.Update)` |
| 3 | DELETE `/categories/{id}` | `MustHavePermission(ContentCoreFeatures.Categories, AppAction.Delete)` |
| 4 | PUT `/categories/reorder` | `MustHavePermission(ContentCoreFeatures.Categories, AppAction.Update)` |
| 5 | POST `/tags` | `MustHavePermission(ContentCoreFeatures.Tags, AppAction.Create)` |
| 6 | PUT `/tags/{id}` | `MustHavePermission(ContentCoreFeatures.Tags, AppAction.Update)` |
| 7 | POST `/specializations` | `MustHavePermission(ContentCoreFeatures.Specializations, AppAction.Create)` |
| 8 | PUT `/specializations/{id}` | `MustHavePermission(ContentCoreFeatures.Specializations, AppAction.Update)` |
| 9 | POST `/languages` | `MustHavePermission(ContentCoreFeatures.Languages, AppAction.Create)` |

**Exact list from PW-1 CSV is the source of truth.** Adjust if newer endpoints appeared in interim sprints.

### 2. ContentPlaces.Presentation (5 STRING_POLICY)

Same pattern. Likely candidates:

| # | Endpoint (typical) | Replacement |
|---|---|---|
| 1 | POST `/places` | `MustHavePermission(ContentPlacesFeatures.Place, AppAction.Create)` |
| 2 | PUT `/places/{id}` | `MustHavePermission(ContentPlacesFeatures.Place, AppAction.Update)` |
| 3 | DELETE `/places/{id}` | `MustHavePermission(ContentPlacesFeatures.Place, AppAction.Delete)` |
| 4 | POST `/businesses` | `MustHavePermission(ContentPlacesFeatures.Business, AppAction.Create)` |
| 5 | PUT `/businesses/{id}` | `MustHavePermission(ContentPlacesFeatures.Business, AppAction.Update)` |

### 3. ContentPlaces.Presentation (4 MISSING_METADATA — MOST DANGEROUS)

These endpoints have NO authorization metadata. Without a fix, default behavior depends on global policy — typically silently 401 or silently allow-all. Each must explicitly state intent:

| # | Endpoint (typical) | Intended | Add |
|---|---|---|---|
| 1 | GET `/places/{id}/businesses` | PUBLIC | `.AllowAnonymous()` |
| 2 | GET `/businesses/{id}/hours` | PUBLIC | `.AllowAnonymous()` |
| 3 | GET `/places/{id}/staff` | PROVIDER OWNER | `MustHavePermission(ContentPlacesFeatures.Business, AppAction.Read)` + handler IDOR via `Business.OwnerUserId` |
| 4 | GET `/businesses/{id}/amenities/admin-view` | ADMIN | `MustHavePermission(ContentPlacesFeatures.AdminBusinessManagement, AppAction.Read)` |

**Decision authority:** business intent per PDF 2. Tech Lead arbitrates ambiguous cases.

### 4. Refactor Pattern (STRING_POLICY)

```csharp
// Before
group.MapPost("/categories", CreateCategoryAsync)
    .RequireAuthorization("permission:contentcore.categories.create");

// After
group.MapPost("/categories", CreateCategoryAsync)
    .WithMetadata(new MustHavePermissionAttribute(
        ContentCoreFeatures.Categories,
        AppAction.Create));
```

### 5. Refactor Pattern (MISSING_METADATA — public)

```csharp
// Before
group.MapGet("/places/{id}/businesses", GetPlaceBusinessesAsync);
// (no metadata — silent default)

// After
group.MapGet("/places/{id}/businesses", GetPlaceBusinessesAsync)
    .AllowAnonymous();
```

### 6. TASK 3 WBS

| # | Step | Hours |
|---|---|---|
| 1 | ContentCore.Presentation — 9 endpoints + tests | 6 |
| 2 | ContentPlaces.Presentation STRING_POLICY — 5 endpoints + tests | 3 |
| 3 | ContentPlaces.Presentation MISSING_METADATA — 4 endpoints, intent decisions, + tests | 3 |
| 4 | Per-endpoint integration tests (`MustHavePermission` attribute verification) | 3 |
| 5 | PR + review fixes | 1 |
| **Total** | | **16h** |

### 7. TASK 3 Acceptance

- All 14 endpoints listed above have either `MustHavePermissionAttribute` OR `.AllowAnonymous()`.
- PW-3 sanity test STRING_POLICY count drops by 14. MISSING_METADATA count drops by 4.
- All endpoint integration tests pass.
- Tech Lead has signed off on the 4 MISSING_METADATA intent decisions in PR description.

---

## Combined Coordination Notes (TASK 2 + TASK 3)

- **Sequential, not parallel commits to same file:** if Mohammad (TASK 3) touches `ContentPlaces.Presentation` while Fadwa (TASK 2) touches Auth/Accounts/Security, no merge conflicts. If schedules slip and both end up in ContentPlaces, coordinate via daily standup.
- **TASK 1 (Mahmoud) also touches ContentPlaces endpoints** (he changes the endpoints for the 8 handlers). Mohammad and Mahmoud agree split: Mahmoud owns the 8 business-admin endpoints (he's already editing them); Mohammad owns the remaining ContentPlaces endpoints. Cross-link in standup.
- **No new migrations** in this sprint.
- **No new permissions** unless PW-2 flagged some.

---

<a id="05-task-seoredirect-middleware"></a>

## 05-task-seoredirect-middleware

> Source: `Authorization-Cleanup/05-task-seoredirect-middleware.md`

# TASK 4 — `SeoRedirectMiddleware` (BONUS)

> **Owner:** Mohammad (Intermediate) — **Hours:** 12h — **Hard deadline:** Wed **2027-03-10 17:00**
> **Earliest start:** Mon 2027-03-08 09:00 (after TASK 3 ContentCore + ContentPlaces work lands)
> **Endpoints:** 0 (middleware) — **Files:** new middleware + new ContentSeo abstraction + wiring + tests
> **Depends on:** ContentSeo module (Wave 4 sprint, already closed) — `ContentSeo.SeoRedirects` table exists.

This bonus task ships the long-deferred `SeoRedirectMiddleware` that consumes `ContentSeo.SeoRedirects` table and serves 301/302 redirects on incoming frontend slug requests. It plugs into the middleware pipeline at position 12 (after `UseAuthorization`, before module endpoints — per `YallaJo.md` middleware-order spec).

---

## 1. Interface Design

`ContentSeo.Contracts/Services/ISeoRedirectLookupService.cs`:

```csharp
namespace ContentSeo.Contracts.Services;

public interface ISeoRedirectLookupService
{
    /// <summary>
    /// Looks up an active redirect for the given canonical lowercase path.
    /// Returns null if no redirect exists.
    /// </summary>
    Task<SeoRedirectResult?> ResolveAsync(string sourcePath, CancellationToken ct);

    /// <summary>
    /// Records a non-blocking hit (fire-and-forget pattern).
    /// </summary>
    void RecordHit(Guid redirectId);
}

public sealed record SeoRedirectResult(
    Guid RedirectId,
    string SourcePath,
    string TargetPath,
    int StatusCode);  // 301 (permanent) or 302 (temporary)
```

**Implementation:** `ContentSeo.Infrastructure/Services/SeoRedirectLookupService.cs` — uses `HybridCache` with key `seo-redirect:{lowercase-path}` 5min sliding + tag `seo-redirects` (invalidation already wired from ContentSeo sprint). DB query: `SELECT TOP 1 Id, SourcePath, TargetPath, StatusCode FROM ContentSeo.SeoRedirects WHERE LOWER(SourcePath) = @path AND IsActive = 1`.

**Hit recording:** uses `Channel<RedirectHit>` Singleton (capacity 1000, DropWrite) drained by `SeoRedirectHitFlushService` BG service every 10 sec — bulk UPDATE statement increments `HitCount` for each. Same pattern as Analytics A-R1 — non-blocking, lossy under storm.

DI registration (ContentSeo.Infrastructure):
```csharp
services.AddSingleton<ISeoRedirectHitQueue, SeoRedirectHitQueue>();  // Channel wrapper
services.AddScoped<ISeoRedirectLookupService, SeoRedirectLookupService>();
services.AddHostedService<SeoRedirectHitFlushService>();
```

---

## 2. Middleware (`YallaJo.Api/Middleware/SeoRedirectMiddleware.cs`)

```csharp
namespace YallaJo.Api.Middleware;

internal sealed class SeoRedirectMiddleware(
    RequestDelegate next,
    ILogger<SeoRedirectMiddleware> logger)
{
    private static readonly PathString ApiPrefix = new("/api");
    private static readonly PathString HealthPrefix = new("/health");
    private static readonly PathString SwaggerPrefix = new("/swagger");
    private static readonly PathString HubsPrefix = new("/hubs");
    private static readonly PathString FrameworkPrefix = new("/_framework");

    public async Task InvokeAsync(
        HttpContext context,
        ISeoRedirectLookupService lookup)
    {
        // Skip infrastructure / API / hubs paths — these never have SEO redirects.
        var path = context.Request.Path;
        if (path.StartsWithSegments(ApiPrefix)
            || path.StartsWithSegments(HealthPrefix)
            || path.StartsWithSegments(SwaggerPrefix)
            || path.StartsWithSegments(HubsPrefix)
            || path.StartsWithSegments(FrameworkPrefix))
        {
            await next(context);
            return;
        }

        var normalizedPath = path.Value!.ToLowerInvariant();
        var redirect = await lookup.ResolveAsync(normalizedPath, context.RequestAborted);
        if (redirect is null)
        {
            await next(context);
            return;
        }

        lookup.RecordHit(redirect.RedirectId);  // fire-and-forget
        context.Response.Headers.Location = redirect.TargetPath
            + context.Request.QueryString.Value;  // preserve query string
        context.Response.StatusCode = redirect.StatusCode;  // 301 or 302
        logger.LogInformation(
            "SEO redirect {Source} -> {Target} ({Status})",
            normalizedPath, redirect.TargetPath, redirect.StatusCode);
    }
}
```

**Why scoped DI parameter on `InvokeAsync`:** middleware itself is a singleton (via `RequestDelegate`); injecting `ISeoRedirectLookupService` via `InvokeAsync` resolves a per-request scope, which is what we want.

**Why `lowercase` normalization:** PDF 2 §8 + ContentSeo sprint spec — slugs case-insensitive.

**Why preserve query string:** SEO landing pages often have `?utm_source=...` tracking params; losing them breaks attribution.

**Why no early return on the redirect path itself collision:** circular redirects are prevented by ContentSeo sprint's redirect-flatten rule (PDF 2 §8 — "if A→B exists and B→C added, system updates A→C directly"). So we never need to follow chains here.

---

## 3. Wire Into Program.cs

```csharp
// YallaJo.Api/Program.cs (after app.UseAuthorization(), before module endpoint mapping)
app.UseAuthorization();

// NEW middleware:
app.UseMiddleware<SeoRedirectMiddleware>();

// Then module endpoints:
app.MapAuthEndpoints();
app.MapAccountsEndpoints();
// ... etc
```

**Why after `UseAuthorization`:** redirects shouldn't bypass auth. If someone hits `/old-private-resource-url`, the redirect happens, then the destination goes through auth check.

**Why before module endpoints:** if a redirect matches, we short-circuit before any module endpoint handler runs.

---

## 4. Hit Recorder BG Service

`ContentSeo.Infrastructure/BackgroundServices/SeoRedirectHitFlushService.cs`:

```csharp
internal sealed class SeoRedirectHitFlushService(
    IServiceProvider sp,
    ISeoRedirectHitQueue queue,
    ILogger<SeoRedirectHitFlushService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var hits = queue.DrainAll();  // returns IReadOnlyList<RedirectHit>
                if (hits.Count == 0) continue;
                using var scope = sp.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ContentSeoDbContext>();
                // Group by RedirectId, count each:
                var counts = hits
                    .GroupBy(h => h.RedirectId)
                    .Select(g => new { RedirectId = g.Key, Count = g.Count() });
                foreach (var c in counts)
                {
                    await ctx.SeoRedirects
                        .Where(r => r.Id == c.RedirectId)
                        .ExecuteUpdateAsync(
                            s => s.SetProperty(r => r.HitCount, r => r.HitCount + c.Count),
                            stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "SeoRedirectHitFlushService tick failed");
        }
    }
}
```

Uses EF Core 7+ `ExecuteUpdateAsync` for bulk SQL UPDATE without entity-tracking overhead.

---

## 5. WBS

| # | Step | Hours |
|---|---|---|
| 1 | `ISeoRedirectLookupService` + `SeoRedirectResult` in ContentSeo.Contracts | 0.5 |
| 2 | `SeoRedirectLookupService` impl with HybridCache | 2 |
| 3 | `ISeoRedirectHitQueue` + `SeoRedirectHitQueue` (Channel wrapper) | 1 |
| 4 | `SeoRedirectHitFlushService` BG | 1.5 |
| 5 | `SeoRedirectMiddleware` impl + path-skip rules | 2 |
| 6 | Program.cs wiring (place at correct pipeline position) | 0.5 |
| 7 | Unit test — lookup service (cache hit + miss + invalidation) | 1.5 |
| 8 | Integration test — middleware (skip /api/*, 301 happy path, 302 happy path, query string preservation, no-match passthrough) | 2 |
| 9 | Load test — 1000 RPS for 60 sec, hit counts accurate to within 1% | 0.5 |
| 10 | PR + review | 0.5 |
| **Total** | | **12h** |

---

## 6. Acceptance

1. **Functional:**
   - GET `/old-blog-slug` (with row in `ContentSeo.SeoRedirects` mapping to `/new-blog-slug` 301) returns `301 Location: /new-blog-slug` with original query string preserved.
   - GET `/api/v1/places` (API path) does NOT trigger middleware DB lookup.
   - GET `/some-random-path` (no redirect row) returns whatever the next middleware/endpoint returns (likely 404).

2. **Performance:**
   - p95 latency overhead from middleware on cache hit: < 5ms.
   - p95 latency overhead on cache miss + DB lookup: < 30ms.
   - 1000 RPS for 60 sec → HitCount reflected in DB within 15 sec of test end (10 sec flush + 5 sec slack).

3. **Tests:**
   - 5 unit tests for `SeoRedirectLookupService` (cache miss → DB hit → cache populated; cache hit → no DB; null → null; lowercase normalization; tag invalidation).
   - 6 integration tests for middleware (skip patterns × 4, 301 happy, 302 happy, query string).
   - 1 BG service test — drain queue, verify SQL UPDATE issued.

4. **Documentation:**
   - `agent-context.md §11.2 Middleware` table row added: `SeoRedirectMiddleware` 5-min cache, 301/302 from `ContentSeo.SeoRedirects`, skip `/api/*`.
   - YallaJo.md middleware-order doc reaffirmed at position 12.
   - No new ADR needed (this is implementation of existing decision from ContentSeo sprint).

---

<a id="06-cross-cutting"></a>

## 06-cross-cutting

> Source: `Authorization-Cleanup/06-cross-cutting.md`

# Authorization-Cleanup — Cross-Cutting Concerns

> Compact version (no new aggregates, DI registrations, migrations, etc.). Most cross-cutting from feature modules doesn't apply.

---

## 1. DI Registration Changes (cumulative across tasks)

| Registration | Where | Reason |
|---|---|---|
| `ISeoRedirectLookupService → SeoRedirectLookupService` | `ContentSeo.Infrastructure/DependencyInjection.cs` | TASK 4 — lookup service |
| `ISeoRedirectHitQueue → SeoRedirectHitQueue` | `ContentSeo.Infrastructure/DependencyInjection.cs` | TASK 4 — Channel wrapper (Singleton) |
| `AddHostedService<SeoRedirectHitFlushService>()` | `ContentSeo.Infrastructure/DependencyInjection.cs` | TASK 4 — flush BG service |

**No other DI changes.** Handler signature changes in TASK 1 don't require DI updates (MediatR auto-discovers handlers).

**Common mistakes:**
- ❌ Registering `ISeoRedirectHitQueue` as Scoped — Channel must be Singleton to share writer across all requests.
- ❌ Forgetting `AddHostedService<SeoRedirectHitFlushService>()` — hits are queued forever, never written to DB.
- ❌ Wiring middleware before `UseAuthorization` — defeats the auth check on destination URL.

---

## 2. Permission Catalog Verification

After PW-2, the expected boot log should be unchanged from Analytics sprint:

```text
[INFO] PermissionSeeder discovered 11 catalogs: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, ContentBlogs, ContentSeo, Booking, Finance, Social, Messaging, Analytics
[INFO] PermissionSeeder inserted/verified <N> permissions (no delta unless PW-2 added new ones)
```

If PW-2 added permissions, expected deltas:
- `AuthFeatures.Session` + `AuthFeatures.Device` (if not present): +6 permissions (3 actions × 2 features)
- `AccountsFeatures.ProviderApplication` (if not present): +3 permissions

**Verification SQL:**
```sql
SELECT Feature, Action, COUNT(*) FROM security.Permissions
WHERE Feature LIKE 'Auth.%' OR Feature LIKE 'Accounts.%'
GROUP BY Feature, Action
ORDER BY Feature, Action;
```

---

## 3. Build Lock Workaround

Same as every prior sprint. Build only changed projects:

```powershell
# TASK 1 (Mahmoud) — ContentPlaces only:
dotnet build ContentPlaces/ContentPlaces.Application/ContentPlaces.Application.csproj
dotnet build ContentPlaces/ContentPlaces.Presentation/ContentPlaces.Presentation.csproj
dotnet build tests/ContentPlaces.Tests.Unit/ContentPlaces.Tests.Unit.csproj

# TASK 2 (Fadwa) — Auth + Accounts + Security:
dotnet build Auth/Auth.Presentation/Auth.Presentation.csproj
dotnet build Accounts/Accounts.Presentation/Accounts.Presentation.csproj
dotnet build Security/Security.Presentation/Security.Presentation.csproj

# TASK 3 (Mohammad) — ContentCore + ContentPlaces:
dotnet build ContentCore/ContentCore.Presentation/ContentCore.Presentation.csproj
dotnet build ContentPlaces/ContentPlaces.Presentation/ContentPlaces.Presentation.csproj

# TASK 4 (Mohammad) — ContentSeo + YallaJo.Api:
dotnet build ContentSeo/ContentSeo.Contracts/ContentSeo.Contracts.csproj
dotnet build ContentSeo/ContentSeo.Infrastructure/ContentSeo.Infrastructure.csproj
dotnet build YallaJo.Api/YallaJo.Api.csproj

# All:
dotnet test tests/Authorization.IntegrationTests/Authorization.IntegrationTests.csproj
```

---

## 4. Migrations

**None.** No new entities, no schema changes. The only DB-touching change is TASK 4's HitCount bulk UPDATE, but that uses existing column.

---

## 5. Inbox/Outbox Hygiene

**No new integration events** in this sprint. No outbox or inbox handlers added. Existing event flow unchanged.

---

## 6. Test Project Topology

- **Existing per-module tests** (`ContentPlaces.Tests.Unit`, etc.) extended with per-handler tests.
- **NEW shared test project** `tests/Authorization.IntegrationTests/` houses the cross-cutting metadata sanity tests from PW-3:
  - `EndpointAuthorizationMetadataTests` (3 facts in PW-3)
  - `EndpointInspector` helper class
- **NEW shared test helper** `tests/Shared.Tests/Endpoints/EndpointInspector.cs` — resolves `EndpointDataSource` and exposes lookup-by-name.

---

## 7. Performance Budget (TASK 4 only — other tasks don't change runtime perf)

| Endpoint / Operation | Baseline | After TASK 4 |
|---|---|---|
| GET `/api/v1/places` | (unchanged) | (unchanged) — middleware skips `/api/*` |
| GET `/some-frontend-slug` with cache hit | n/a (no middleware before) | < 5ms added |
| GET `/some-frontend-slug` with cache miss | n/a | < 30ms added (DB lookup) |
| GET `/old-slug` returning 301 | n/a | < 30ms total |
| HitCount accuracy under 1000 RPS load | n/a | accurate to ±1% within 15 sec |

---

## 8. Cross-Module Coupling Risks

| Risk | Mitigation |
|---|---|
| TASK 1's command signature changes break ContentPlaces tests | Tests updated in-PR per handler |
| TASK 3 fixes ContentPlaces endpoints that TASK 1 also fixes | Coordination: Mahmoud owns 8 business-admin endpoints, Mohammad owns the rest |
| Newly-added permissions (PW-2) aren't seeded to existing dev DBs | Document in PR: devs run `dotnet ef database update` for Security module after pulling |
| `SeoRedirectMiddleware` accidentally catches `/hubs/*` SignalR paths | Explicit skip in path-prefix list (TASK 4 §2) |
| Channel back-pressure on `ISeoRedirectHitQueue` under storm | `BoundedChannelFullMode.DropWrite` + counter — acceptable lossy HitCount per A-R1 pattern |

---

## 9. Documentation Hygiene Carry-Forward

- `agent-context.md §8.1` "ICurrentUser violations" section updated from 8 violations → ✅ All resolved.
- `agent-context.md §8.2` "Endpoint authorization violations" section updated from 28 violations → ✅ All resolved.
- `agent-context.md §11.2 Middleware` table row added for `SeoRedirectMiddleware`.
- `Agents/decisions/closed/Authorization-Cleanup/` houses this folder post-sprint.
- `Agents/error-log.md` — add any drift gotchas hit during sprint (likely: permission-string-typo in MustHavePermission lookup).
- `Phase1-Phase2-Completion-INDEX.md` §1 row gets 🟢 + closed link.

---

## 10. Sprint-End Audit Command

Final acceptance gate runs this PowerShell snippet:

```powershell
# Count remaining violations after sprint:
$bareAuth = ast-grep --pattern '.RequireAuthorization()' --lang csharp `
    Auth.Presentation Accounts.Presentation Security.Presentation `
    ContentCore.Presentation ContentPlaces.Presentation ContentTours.Presentation `
    ContentBlogs.Presentation ContentSeo.Presentation Booking.Presentation `
    Finance.Presentation Social.Presentation Messaging.Presentation Analytics.Presentation `
    | Measure-Object -Line

$stringPolicy = ast-grep --pattern '.RequireAuthorization("$_")' --lang csharp `
    Auth.Presentation Accounts.Presentation Security.Presentation `
    ContentCore.Presentation ContentPlaces.Presentation ContentTours.Presentation `
    ContentBlogs.Presentation ContentSeo.Presentation Booking.Presentation `
    Finance.Presentation Social.Presentation Messaging.Presentation Analytics.Presentation `
    | Measure-Object -Line

$icurrentInHandlers = rg "ICurrentUser" --type cs -g "**/Application/**/Commands/*Handler.cs" `
    | Measure-Object -Line

Write-Host "Bare RequireAuthorization: $($bareAuth.Lines)"
Write-Host "String-policy RequireAuthorization: $($stringPolicy.Lines)"
Write-Host "ICurrentUser in command handlers: $($icurrentInHandlers.Lines) (audit each)"
```

Target on sprint close:
- Bare `RequireAuthorization`: **0**
- String-policy: **0**
- ICurrentUser in command handlers: low single digits (only the documented ownership/IDOR/creator-stamp uses)

---

<a id="99-acceptance-gate"></a>

## 99-acceptance-gate

> Source: `Authorization-Cleanup/99-acceptance-gate.md`

# Authorization-Cleanup — Final Acceptance Gate

> **Tech Lead signs off before declaring the sprint closed (Thu 2027-03-11 17:00).** Folder does NOT move to `Agents/decisions/closed/Authorization-Cleanup/` until every box ticked.

---

## 1. Code Quality (PR & build)

- [ ] All 4 task PRs (TASK 1..TASK 4) merged into `main`.
- [ ] `dotnet build` green for every project changed:
  - Auth.Presentation, Accounts.Presentation, Security.Presentation
  - ContentCore.Presentation, ContentPlaces.Presentation, ContentPlaces.Application
  - ContentSeo.Contracts, ContentSeo.Infrastructure
  - YallaJo.Api
  - tests/Authorization.IntegrationTests, tests/ContentPlaces.Tests.Unit, tests/Shared.Tests
- [ ] **Zero TODOs** in committed code: `rg "TODO|FIXME|HACK" Auth.Presentation Accounts.Presentation Security.Presentation ContentCore.Presentation ContentPlaces.Presentation ContentSeo.Contracts ContentSeo.Infrastructure YallaJo.Api/Middleware` → 0 matches.
- [ ] **No `RequireAuthorization()` bare** anywhere in Presentation projects (per §10 audit command).
- [ ] **No `RequireAuthorization("permission:...")` string policies** anywhere.
- [ ] **PW-3 sanity tests all GREEN** — was RED on sprint kickoff:
  - `Every_registered_endpoint_has_either_MustHavePermission_or_AllowAnonymous` — GREEN
  - `No_endpoint_uses_string_based_RequireAuthorization_with_permission_prefix` — GREEN
  - `Every_MustHavePermission_attribute_references_a_registered_permission` — GREEN
- [ ] **At least 30 unit + 15 integration tests added/updated** across all 4 tasks (8 handler tests × 2 cases + 28 endpoint metadata tests + 5 lookup-service + 6 middleware = ~50+).
- [ ] **TASK 4 load test runbook** stored in `Agents/decisions/closed/Authorization-Cleanup/_loadtest-runbook.md` — 1000 RPS sustained 60s; HitCount accurate to ±1% within 15s.

---

## 2. Endpoint Re-audit (manual run)

Reviewer (Tech Lead) runs PW-1 ast-grep commands again **after all PRs merged**. Expected counts:

| Metric | Pre-sprint | Post-sprint target |
|---|---|---|
| Bare `.RequireAuthorization()` calls | 14 | **0** |
| `.RequireAuthorization("permission:...")` string-policies | 14 | **0** |
| Endpoints with NO auth metadata (§8.2 MISSING_METADATA) | 4 | **0** |
| `ICurrentUser` injections in handler files matching IsAuthenticated checks | 8 | **0** |
| Total §8.x violations | 36 | **0** |

Any non-zero post-sprint count blocks sign-off. Reviewer files bug, owner fixes within 24h.

---

## 3. Functional Smoke Test (manual via Postman / HTTP REPL)

| # | Method | Path | Expected | Notes |
|---|---|---|---|---|
| 1 | GET | `/auth/sessions` (with valid JWT, role lacks `AuthFeatures.Session.Read`) | 403 | TASK 2 — proves MustHavePermission gating |
| 2 | GET | `/auth/sessions` (with valid JWT, role has `AuthFeatures.Session.Read`) | 200 + list | TASK 2 |
| 3 | DELETE | `/auth/sessions/{id}` (with valid JWT) | 204 | TASK 2 |
| 4 | POST | `/auth/change-password` (without JWT) | 401 | TASK 2 — JWT middleware rejects before MustHavePermission |
| 5 | GET | `/profile` (with valid JWT) | 200 | TASK 2 |
| 6 | POST | `/categories` (with admin JWT) | 201 | TASK 3 ContentCore |
| 7 | POST | `/categories` (with regular-user JWT) | 403 | TASK 3 |
| 8 | GET | `/places/{id}/businesses` (no JWT) | 200 + list | TASK 3 (MISSING_METADATA → `.AllowAnonymous()`) |
| 9 | POST | `/admin/businesses/{id}/approve` (admin JWT) | 200, Business.Status = Approved, ApprovedByUserId stamped | TASK 1 ApproveBusiness |
| 10 | POST | `/admin/businesses/{id}/approve` (provider JWT, lacks Business.Approve) | 403 | TASK 1 |
| 11 | POST | `/businesses/{id}/amenities` (provider JWT, owns business) | 201, amenity added | TASK 1 AddBusinessAmenity happy |
| 12 | POST | `/businesses/{id}/amenities` (different-provider JWT, doesn't own) | 403 `Business.OwnerMismatch` | TASK 1 IDOR enforcement |
| 13 | GET | `/old-slug-from-blog` (TASK 4 SEO redirect row in DB → `/new-slug`) | 301 Location: /new-slug?{originalQuery} | TASK 4 happy path |
| 14 | GET | `/api/v1/places` (TASK 4 middleware skip) | 200 + list (no redirect lookup hit) | TASK 4 skip pattern |

**Any RED row blocks sign-off.**

---

## 4. Performance Sanity

Quick check (1 min k6 run per scenario):

| Endpoint | Scenario | p95 target |
|---|---|---|
| `GET /api/v1/places` | 100 req/sec for 60s | unchanged from baseline (middleware skips `/api/*`) |
| `GET /some-frontend-slug` (cache hit) | 100 req/sec for 60s | < 50ms (includes 5ms middleware overhead) |
| `GET /old-slug` (returns 301) | 100 req/sec for 60s | < 50ms |
| `POST /admin/businesses/{id}/approve` | 10 req/sec for 60s | < 250ms (unchanged from TASK 1 refactor) |

---

## 5. Documentation Hygiene

- [ ] **`agent-context.md §8.1` updated**: Replace violation list with `✅ All resolved in Authorization-Cleanup sprint 2027-03-12 — see Agents/decisions/closed/Authorization-Cleanup/`. Keep historical violation count for context.
- [ ] **`agent-context.md §8.2` updated**: same.
- [ ] **`agent-context.md §11.1 module status`**: Update each affected module's row to reflect cleanup (e.g. Auth ✅ Complete + cleanup PR link).
- [ ] **`agent-context.md §11.2 Middleware` table**: Add row for `SeoRedirectMiddleware`.
- [ ] **`Agents/permissions-inventory.md`**: Reflect any newly-added permissions from PW-2.
- [ ] **`Agents/error-log.md`**: Append any drift gotchas (e.g. permission-string-typo in `MustHavePermission(SomeFeature, Update)` where `Update` was misspelled).
- [ ] **Folder move**:
  ```powershell
  Move-Item -LiteralPath "Agents\tasks\Authorization-Cleanup" -Destination "Agents\decisions\closed\Authorization-Cleanup"
  ```
- [ ] **Master `Phase1-Phase2-Completion-INDEX.md` §1 row** updated to 🟢 status + link to closed/ path.
- [ ] **AGENTS.md (repo root)** — add line for `Middleware/SeoRedirectMiddleware.cs` ownership.

---

## 6. Phase 1 + Phase 2 Completion Declaration

This is the **last sprint** in the Phase 1 + Phase 2 program. After sign-off:

- [ ] **`agent-context.md §11.1`** module status overview — all 14 modules ✅ for Phase 1 + Phase 2 scope.
- [ ] **`Phase1-Phase2-Completion-INDEX.md`** marks the program complete:
  - All 6 sprint folders moved to `Agents/decisions/closed/`
  - Index header banner updated: "🎉 Phase 1 + Phase 2 COMPLETE on 2027-03-12 — see closed/ for sprint history. Phase 3 kickoff: TBD."
- [ ] **Tech Lead** schedules Phase 3 kickoff meeting (Phase 3 = Wave 7-8: Packaging, Subscriptions, Loyalty, Referrals, Disputes, Accessibility reviews).
- [ ] **Project demo** (program-wide) to stakeholders Mon 2027-03-15 — walkthrough of all 14 modules end-to-end with sample user/provider/admin journeys.

---

## 7. Sprint Retro (Fri 2027-03-12 11:00 AST)

15-min demo by Mohammad:
1. Show RED → GREEN transition of PW-3 sanity tests.
2. Show `/old-slug` → 301 → `/new-slug` redirect live in browser.
3. Show ContentPlaces handler with `ICurrentUser` removed + IDOR check working (one valid, one rejected).
4. Pull up master INDEX with all 6 sprint folders 🟢.

Retro doc lives at `Agents/decisions/closed/Authorization-Cleanup/_retro.md`:
- What went well (≥ 3 items — likely: PW-3 test-first approach caught regressions early)
- What hurt (≥ 3 items)
- Phase 1 + Phase 2 program-level retro (≥ 5 items): biggest wins, biggest pains, lessons for Phase 3 sprint structure

---

## 8. Sign-Off Block

| Role | Name | Date | Signature |
|---|---|---|---|
| TASK 1 owner | Mahmoud | _____ | _____ |
| TASK 2 owner | Fadwa | _____ | _____ |
| TASK 3 owner | Mohammad | _____ | _____ |
| TASK 4 owner | Mohammad | _____ | _____ |
| Tech Lead | _____ | _____ | _____ |
| Product (sign-off on Phase 1+2 complete) | _____ | _____ | _____ |

**Once all signatures collected:**
1. Folder moves to closed/.
2. INDEX updated to mark program complete.
3. agent-context.md §8 marked resolved.
4. Phase 3 kickoff scheduled.
5. 🎉 **YallaJo Phase 1 + Phase 2 delivery complete.** 🎉

---

