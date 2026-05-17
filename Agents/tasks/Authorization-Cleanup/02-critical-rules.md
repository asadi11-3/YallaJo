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
