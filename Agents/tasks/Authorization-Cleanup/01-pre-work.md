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
