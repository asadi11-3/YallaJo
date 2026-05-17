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
