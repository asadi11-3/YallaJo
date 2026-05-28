# Master Execution Plan — All 12 Plan Docs → 10/10

> **Created**: 2025-01-27 (Prometheus planning agent)
> **Type**: Concrete execution playbook
> **Companion to**: `Agents/Plans/Master-RoadmapTo10.md` (strategic roadmap)
> **Goal**: Phase-by-phase actionable tasks to raise every audited plan to 10/10
> **Format**: Each phase is independently runnable with explicit file paths, code patterns, acceptance criteria, and verification commands
> **Audience**: Sisyphus implementation worker (executed via `task()` delegation)
> **Estimated total effort**: 60-80 hours

---

## How To Use This Document

1. **Pick a phase from the tracking table** (bottom of doc) — Phases are ordered by dependency
2. **Read the phase header** — Contains effort, dependencies, files touched
3. **Read referenced fix plan** — Each phase points to `Agents/Plans/{Module}-FixPlan.md` for exact rule details
4. **Follow the steps in order** — Each step is atomic
5. **Run the verification command** — Build must pass before marking phase done
6. **Update tracking table** — Mark phase complete

> Each phase is **git-atomic**: commit at phase boundary, easy rollback.

---

## Current Score Snapshot

| # | Plan | Now | Target | Gap | Wave |
|---|------|-----|--------|-----|------|
| 1 | ContentCore-Workflow.md | 9.2 | 10.0 | 0.8 | W4-A |
| 2 | Role-System.md | 9.5 | 10.0 | 0.5 | W4-F |
| 3 | Platform-Onboarding-Workflow.md | 9.0 | 10.0 | 1.0 | W4-E |
| 4 | BlogCreatorPost-Merger.md | 9.3 | 10.0 | 0.7 | W4-D |
| 5 | ContentPlaces-Workflow.md | 9.5 | 10.0 | 0.5 | W4-C |
| 6 | TourGuide-Flow.md | 9.0 | 10.0 | 1.0 | W3-C |
| 7 | Booking-Workflow.md | 8.5 | 10.0 | 1.5 | W3-A |
| 8 | Analytics-Workflow.md | 6.8 | 10.0 | 3.2 | W2-A |
| 9 | ContentSeo-Workflow.md | 8.8 | 10.0 | 1.2 | W4-B |
| 10 | Finance-Workflow.md | 7.0 | 10.0 | 3.0 | W2-B |
| 11 | Messaging-Workflow.md | 7.5 | 10.0 | 2.5 | W2-C |
| 12 | Social-Workflow.md | 7.8 | 10.0 | 2.2 | W3-B |

---

## Global Pre-Flight (Run Once Before Wave 1)

```powershell
cd C:\Users\admin1\source\repos\YallaJo

# 1. Verify clean working directory
git status

# 2. Baseline build (must pass)
dotnet build YallaJo.sln --no-restore
# Expect: 0 errors

# 3. Capture warning baseline
dotnet build YallaJo.sln --no-restore 2>&1 | Select-String "Warning" | Measure-Object | Select-Object Count
# Record number — used to detect regression
```

**Tool note for Sisyphus workers**: Build via `lean-ctx_ctx_shell` with cwd `C:\Users\admin1\source\repos\YallaJo`. The Bash tool fails because the `lean-ctx.cmd` path uses Unix-style `/c/Users/...` which PowerShell 5.1 rejects.

---

# WAVE 1 — Cross-Cutting (10-14 hours)

> **Why first**: These touch multiple modules. Fixing them once boosts ALL plans by 0.3-0.5 points each.

---

## Phase W1-A: Add Missing Command Validators (3-4 hours)

**Boost**: Messaging +0.5, Social +0.3, Booking +0.2, Analytics +0.2
**Dependencies**: None
**Output**: 19 new validator files
**Sisyphus delegation**: Single `task()` call, parallelizable per-module

### Pattern (use this template for every validator)

Reference: `ContentTours.Application/Commands/.../ApplyForTourCommandValidator.cs`

```csharp
using FluentValidation;

namespace Messaging.Application.Commands.Tickets.CreateSupportTicket;

internal sealed class CreateSupportTicketCommandValidator
    : AbstractValidator<CreateSupportTicketCommand>
{
    public CreateSupportTicketCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Priority).IsInEnum();
    }
}
```

### Tasks

| # | Module | Command | New File Path |
|---|--------|---------|---------------|
| 1 | Messaging | CreateSupportTicketCommand | `Messaging.Application/Commands/Tickets/CreateSupportTicket/CreateSupportTicketCommandValidator.cs` |
| 2 | Messaging | PostTicketMessageCommand | `Messaging.Application/Commands/Tickets/PostTicketMessage/PostTicketMessageCommandValidator.cs` |
| 3 | Messaging | CloseTicketCommand | `Messaging.Application/Commands/Tickets/CloseTicket/CloseTicketCommandValidator.cs` |
| 4 | Messaging | AssignTicketCommand | `Messaging.Application/Commands/Tickets/AssignTicket/AssignTicketCommandValidator.cs` |
| 5 | Messaging | EscalateTicketCommand | `Messaging.Application/Commands/Tickets/EscalateTicket/EscalateTicketCommandValidator.cs` |
| 6 | Messaging | MarkNotificationReadCommand | `Messaging.Application/Commands/Notifications/MarkRead/MarkNotificationReadCommandValidator.cs` |
| 7 | Messaging | BatchMarkReadCommand | same folder pattern |
| 8 | Messaging | BatchDeleteNotificationsCommand | same |
| 9 | Messaging | RegisterDeviceTokenCommand | `Messaging.Application/Commands/Devices/Register/RegisterDeviceTokenCommandValidator.cs` |
| 10 | Messaging | DeleteDeviceTokenCommand | same folder |
| 11 | Messaging | UpdateNotificationPreferenceCommand | `Messaging.Application/Commands/Preferences/Update/UpdateNotificationPreferenceCommandValidator.cs` |
| 12 | Messaging | CreateNotificationTemplateCommand | `Messaging.Application/Commands/Templates/Create/CreateNotificationTemplateCommandValidator.cs` |
| 13 | Messaging | UpdateNotificationTemplateCommand | same folder |
| 14 | Messaging | DeleteNotificationTemplateCommand | same |
| 15 | Social | (verify CreateReviewCommandValidator exists; add if missing) | `Social.Application/Commands/.../CreateReviewCommandValidator.cs` |
| 16 | Social | UpdateReviewCommand | similar |
| 17 | Social | DeleteReviewCommand | similar |
| 18 | Social | SubmitReportCommand | similar |
| 19 | Social | ResolveReportCommand | similar |

**Exact rule details**: Read `Agents/Plans/Messaging-FixPlan.md` Fix 1 table for Messaging validators. Read `Agents/Plans/Social-FixPlan.md` for Social.

### Verification

```powershell
dotnet build YallaJo.sln --no-restore
# Must: 0 errors
```

### Acceptance criteria

- [ ] 14 new Messaging validator files created
- [ ] 5 new Social validator files (or confirmed existing)
- [ ] Build passes with 0 errors
- [ ] Warning count ≤ baseline

---

## Phase W1-B: HybridCache Adoption (3-4 hours)

**Boost**: Messaging +0.3, Social +0.3, Analytics +0.2
**Dependencies**: None
**Output**: 2 new cache-key files + ~9 query handler modifications

### Pattern

Reference: any `*QueryHandler.cs` in `ContentTours.Application/Queries/.../GetTourGuideByIdQueryHandler.cs`

```csharp
var result = await cache.GetOrCreateAsync(
    MessagingCacheKeys.Notifications(userId, page, pageSize),
    async (innerCt) => { /* DB query */ },
    new HybridCacheEntryOptions { Expiration = TimeSpan.FromSeconds(30) },
    tags: new[] { MessagingCacheKeys.TagForNotifications(userId) },
    cancellationToken: ct);
```

### Step B1: Create cache-key files

**File 1**: `Messaging.Application/Caching/MessagingCacheKeys.cs` (NEW)

```csharp
namespace Messaging.Application.Caching;

public static class MessagingCacheKeys
{
    public static string Notifications(Guid userId, int page, int pageSize)
        => $"messaging:notifications:{userId}:{page}:{pageSize}";
    public static string TagForNotifications(Guid userId)
        => $"notifications:{userId}";

    public static string UnreadCount(Guid userId)
        => $"messaging:unread-count:{userId}";

    public static string Preferences(Guid userId)
        => $"messaging:preferences:{userId}";
    public static string TagForPreferences(Guid userId)
        => $"preferences:{userId}";

    public static string Tickets(Guid userId, string status, int page)
        => $"messaging:tickets:{userId}:{status}:{page}";
    public static string TagForTickets(Guid userId)
        => $"tickets:{userId}";
}
```

**File 2**: `Social.Application/Caching/SocialCacheKeys.cs` (NEW)

```csharp
namespace Social.Application.Caching;

public static class SocialCacheKeys
{
    public static string Reviews(string entityType, Guid entityId, int page)
        => $"social:reviews:{entityType}:{entityId}:{page}";
    public static string TagForEntity(string entityType, Guid entityId)
        => $"social:entity:{entityType}:{entityId}";

    public static string Favorites(Guid userId, int page)
        => $"social:favorites:{userId}:{page}";
    public static string TagForFavorites(Guid userId)
        => $"social:favorites:{userId}";

    public static string Summary(string entityType, Guid entityId)
        => $"social:summary:{entityType}:{entityId}";
}
```

### Step B2: Wrap query handlers (9 modifications)

| Module | Query Handler File | Cache Key Call | TTL | Tag |
|--------|-------------------|----------------|-----|-----|
| Messaging | `GetMyNotificationsQueryHandler.cs` | `Notifications(uid,page,size)` | 30s | `TagForNotifications(uid)` |
| Messaging | `GetUnreadCountQueryHandler.cs` | `UnreadCount(uid)` | 15s | `TagForNotifications(uid)` |
| Messaging | `GetMyPreferencesQueryHandler.cs` | `Preferences(uid)` | 5min | `TagForPreferences(uid)` |
| Messaging | `GetSupportTicketsQueryHandler.cs` | `Tickets(uid,status,page)` | 30s | `TagForTickets(uid)` |
| Social | `GetReviewsForEntityQueryHandler.cs` | `Reviews(type,id,page)` | 60s | `TagForEntity(type,id)` |
| Social | `GetMyFavoritesQueryHandler.cs` | `Favorites(uid,page)` | 30s | `TagForFavorites(uid)` |
| Social | `GetReviewSummaryQueryHandler.cs` | `Summary(type,id)` | 5min | `TagForEntity(type,id)` |
| Analytics | `GetMyDashboardQueryHandler.cs` | (create AnalyticsCacheKeys if needed) | 60s | per-user tag |
| Analytics | `GetProviderMetricsQueryHandler.cs` | similar | 5min | per-provider tag |

### Step B3: Add invalidation in write handlers

For each write handler that mutates cached data, append:

```csharp
await cache.RemoveByTagAsync(MessagingCacheKeys.TagForNotifications(userId), ct);
```

Examples:
- `MarkNotificationReadCommandHandler` → invalidate `TagForNotifications(userId)`
- `CreateReviewCommandHandler` → invalidate `TagForEntity(entityType, entityId)`
- `FavoriteAddedCommandHandler` → invalidate `TagForFavorites(userId)`

### Verification

```powershell
dotnet build YallaJo.sln --no-restore

# Verify cache key usage
Select-String -Path "Messaging.Application\**\*.cs" -Pattern "MessagingCacheKeys" -Recurse | Measure-Object
# Expect: ≥ 8 matches (4 query handlers × 2 calls + invalidations)
```

### Acceptance criteria

- [ ] 2 new cache-key files created
- [ ] ≥9 query handlers use `GetOrCreateAsync`
- [ ] Write handlers invoke `RemoveByTagAsync` where appropriate
- [ ] Build passes

---

## Phase W1-C: Auth Gate Cleanup (2-3 hours)

**Boost**: Analytics +0.5, Social +0.4, Booking +0.1
**Dependencies**: None
**Output**: ~30 endpoint modifications

### Goal

Eliminate inappropriate `.AllowAnonymous()` on mutations. Public reads OK but should be rate-limited.

### Step C1: Audit Analytics

```powershell
# Find anonymous endpoints
ast_grep --pattern '.AllowAnonymous()' --lang csharp Analytics.Presentation
```

Decision matrix per match:
- **POST/PUT/PATCH/DELETE** → MUST be `.RequireAuthorization()` + `MustHavePermission(...)`
- **GET (truly public aggregated)** → keep anonymous
- **GET (user-specific data)** → MUST require auth

Reference: `Agents/Plans/Analytics-FixPlan.md` Fix 2.

### Step C2: Audit Social

Same process for `Social.Presentation`. Target: 18 endpoints need auth.

Reference: `Agents/Plans/Social-FixPlan.md` Fix 3.

### Step C3: Spot-check Booking

Verify `Booking.Presentation/Endpoints/*.cs` — should be mostly fine, look for outliers.

### Verification

```powershell
# Count remaining anonymous mutations (should be 0)
ast_grep --pattern 'MapPost($_, $_).$$$.AllowAnonymous()' --lang csharp Analytics.Presentation
ast_grep --pattern 'MapPost($_, $_).$$$.AllowAnonymous()' --lang csharp Social.Presentation
ast_grep --pattern 'MapPut($_, $_).$$$.AllowAnonymous()' --lang csharp Analytics.Presentation
ast_grep --pattern 'MapPut($_, $_).$$$.AllowAnonymous()' --lang csharp Social.Presentation
ast_grep --pattern 'MapDelete($_, $_).$$$.AllowAnonymous()' --lang csharp Analytics.Presentation
ast_grep --pattern 'MapDelete($_, $_).$$$.AllowAnonymous()' --lang csharp Social.Presentation
# All: expect 0 (or only documented public-write endpoints)

dotnet build YallaJo.sln --no-restore
```

### Acceptance criteria

- [ ] Zero anonymous mutations in Analytics
- [ ] Zero anonymous mutations in Social
- [ ] All remaining `AllowAnonymous` documented as intentional via code comment
- [ ] Build passes

---

## Phase W1-D: CQRS Bypass Removal (2-3 hours)

**Boost**: Analytics +0.7 (single biggest boost)
**Dependencies**: None (but pairs well with W1-C)
**Output**: ~15 new query handlers + endpoint refactoring

### Goal

Every Analytics endpoint must send commands/queries via `ISender`. No direct `DbContext` access in Presentation.

### Step D1: Identify bypasses

```powershell
# Look for AnalyticsDbContext injected into endpoint method signatures
Select-String -Path "Analytics.Presentation\**\*.cs" -Pattern "AnalyticsDbContext\s+\w+" -Recurse
```

### Step D2: Refactor each bypass

For each match:
1. Create matching query in `Analytics.Application/Queries/Xxx/XxxQuery.cs`
2. Move DbContext logic to handler in `XxxQueryHandler.cs`
3. Endpoint sends via `ISender`

Reference: `Agents/Plans/Analytics-FixPlan.md` Fix 1 with file-by-file mapping.

### Verification

```powershell
# Should be zero direct DbContext injection in Presentation
Select-String -Path "Analytics.Presentation\**\*.cs" -Pattern "DbContext\s+\w+\s*[,)]" -Recurse
# Expect: 0 matches

dotnet build YallaJo.sln --no-restore
```

### Acceptance criteria

- [ ] ~15 new query handlers added
- [ ] All Analytics endpoints use `ISender`
- [ ] Build passes
- [ ] Performance benchmarks within ±10% of baseline (if measurable)

---

## Phase W1-E: Doc Implementation Notes Batch (1-2 hours)

**Boost**: +0.2 to every plan (12 plans × 0.2 = +2.4 total)
**Dependencies**: Run AFTER all W2 + W3 + W4 so counts are final

### Template

Append to each plan doc in `Agents/Plans/{Module}-Workflow.md`:

```markdown
---

## Implementation Notes (Final State — YYYY-MM-DD)

> **Status**: Implemented and audited. Score: X.X/10
> **Audit Report**: `Agents/Plans/{Module}-Audit-Report.md`

### Actual Counts (verified by `Select-String` grep against codebase)
- Domain entities: N
- Application commands: N (N validators = N% coverage)
- Application queries: N
- Repositories: N
- EF Core configurations: N
- Migrations: N (latest: `<migration_name>`)
- Endpoints: N across N files
- Integration events: N (N inbound, N outbound)
- Background services: N
- Permissions: N actions across N features

### Cross-Module Touchpoints
- **Consumes** events from: [list modules]
- **Publishes** events to: [list modules]
- **Shared contracts**: [list interfaces in this module's Contracts project]

### Deferred / Out-of-Scope
- [Item 1] — Reason
- [Item 2] — Reason

### Notable Implementation Decisions
1. [Decision 1 with rationale]
2. [Decision 2 with rationale]
```

### Tasks

12 plan docs (all in `Agents/Plans/`):
1. ContentCore-Workflow.md
2. Role-System.md
3. Platform-Onboarding-Workflow.md
4. BlogCreatorPost-Merger.md
5. ContentPlaces-Workflow.md
6. TourGuide-Flow.md
7. Booking-Workflow.md
8. Analytics-Workflow.md
9. ContentSeo-Workflow.md
10. Finance-Workflow.md
11. Messaging-Workflow.md
12. Social-Workflow.md

### Verification

For each plan, verify counts match codebase:

```powershell
# Example for ContentCore:
$entities = Get-ChildItem -Path "ContentCore.Domain\Entities" -Filter "*.cs" -Recurse | Measure-Object | Select -ExpandProperty Count
$validators = Select-String -Path "ContentCore.Application\**\*Validator.cs" -Pattern "AbstractValidator" -Recurse | Measure-Object | Select -ExpandProperty Count
$endpoints = Select-String -Path "ContentCore.Presentation\**\*Endpoints.cs" -Pattern "Map(Get|Post|Put|Delete|Patch)" -Recurse | Measure-Object | Select -ExpandProperty Count
Write-Output "Entities: $entities, Validators: $validators, Endpoints: $endpoints"
```

### Acceptance criteria

- [ ] All 12 plans have "Implementation Notes" section
- [ ] All counts match codebase (verified)
- [ ] Status header on every plan: "Implemented (audited YYYY-MM-DD, score X.X/10)"

---

# WAVE 2 — High-Gap Modules (20-25 hours)

## Phase W2-A: Analytics 6.8 → 10.0 (8-10 hours)

**Source plan**: `Agents/Plans/Analytics-FixPlan.md`
**Prerequisites**: W1-A, W1-B, W1-C, W1-D
**Boost**: Analytics +3.2

### Module-specific sub-phases

| Sub-phase | Fix | Effort | Key Files |
|-----------|-----|--------|-----------|
| W2-A.1 | Collaborative filtering algorithm | 3h | `Analytics.Application/Services/IRecommendationEngine.cs` + impl |
| W2-A.2 | Blended scoring (engagement+recency+popularity) | 2h | `Analytics.Application/Services/BlendedScoreCalculator.cs` |
| W2-A.3 | Guide dashboard endpoints | 2h | `Analytics.Presentation/Endpoints/GuideDashboardEndpoints.cs` + 4 queries |
| W2-A.4 | GDPR export endpoint | 1h | `Analytics.Application/Commands/Gdpr/ExportUserData/` + endpoint |
| W2-A.5 | Audit trail spoofing fix | 1h | Audit all entity commands; replace `request.UserId` with `ICurrentUser.UserId` |
| W2-A.6 | DashboardCache add EntityType + Granularity columns | 1h | Entity + EF config + migration |
| W2-A.7 | Plan doc update | 30min | `Analytics-Workflow.md` |

**Read these before starting**: `Agents/Plans/Analytics-Audit-Report.md`, `Agents/Plans/Analytics-FixPlan.md`

### Verification

```powershell
dotnet build YallaJo.sln --no-restore

# Re-audit Analytics
# Dispatch explore agent to verify plan vs code
```

### Acceptance criteria (10/10)

- [ ] Plan accuracy: 0 stale counts
- [ ] All planned features built (no stubs)
- [ ] Every command has validator (carried from W1-A)
- [ ] Every high-traffic query cached (carried from W1-B)
- [ ] No CQRS bypasses (carried from W1-D)
- [ ] No anonymous mutations (carried from W1-C)
- [ ] Audit trail uses `ICurrentUser` exclusively
- [ ] Implementation Notes section added (in W1-E)

---

## Phase W2-B: Finance 7.0 → 10.0 (6-8 hours)

**Source plan**: `Agents/Plans/Finance-FixPlan.md`
**Prerequisites**: W1-A (for 8 missing validators)
**Boost**: Finance +3.0

### Module-specific sub-phases

| Sub-phase | Fix | Effort | Key Files |
|-----------|-----|--------|-----------|
| W2-B.1 | Build CreditNote entity + repo + handlers + endpoints | 2h | `Finance.Domain/Entities/CreditNote.cs` + EF config + 3 commands + queries + endpoint |
| W2-B.2 | Build GuideEarning aggregate + dashboard | 3h | New entity + repo + 4 queries + `Finance.Presentation/Endpoints/GuideEarningsEndpoints.cs` |
| W2-B.3 | Build admin dashboard endpoints | 2h | 3 admin queries + `Finance.Presentation/Endpoints/AdminFinanceDashboardEndpoints.cs` |
| W2-B.4 | Complete agency commission split logic | 1h | Modify `Finance.Application/Commands/Commission/Calculate/CalculateCommissionCommandHandler.cs` |
| W2-B.5 | Add `BankTransfer` to ProviderPaymentMethodType (or update doc to reflect actual values) | 30min | Enum or doc |
| W2-B.6 | Validators for 8 commands | 1h | 8 new validator files |
| W2-B.7 | Plan doc update | 30min | `Finance-Workflow.md` |

**Read these before starting**: `Agents/Plans/Finance-Audit-Report.md`, `Agents/Plans/Finance-FixPlan.md`

### Verification

```powershell
dotnet build YallaJo.sln --no-restore

# Preview migration before applying
dotnet ef migrations script --startup-project YallaJo.WebApi --project Finance.Infrastructure --output finance-migration-preview.sql --idempotent
```

### Acceptance criteria (10/10)

- [ ] CreditNote entity + endpoints functional end-to-end
- [ ] GuideEarning dashboard returns data for test guide
- [ ] Admin dashboard shows pending payouts, disputed payments, queue stats
- [ ] All 8 missing validators added
- [ ] Plan doc reflects 33 endpoints + 21 entities + 8 permissions
- [ ] Implementation Notes section added (in W1-E)

---

## Phase W2-C: Messaging 7.5 → 10.0 (6-7 hours)

**Source plan**: `Agents/Plans/Messaging-FixPlan.md`
**Prerequisites**: W1-A (14 validators), W1-B (HybridCache)
**Boost**: Messaging +2.5

### Module-specific sub-phases

| Sub-phase | Fix | Effort | Key Files |
|-----------|-----|--------|-----------|
| W2-C.1 | ISmsSender interface + NoOpSmsSender | 1h | `Messaging.Application/Interfaces/ISmsSender.cs` + `Messaging.Infrastructure/Channels/NoOpSmsSender.cs` + DI |
| W2-C.2 | SLA monitoring background service | 1.5h | `Messaging.Infrastructure/BackgroundServices/SlaMonitoringService.cs` + `Messaging.Contracts/IntegrationEvents/TicketSlaBreachedIntegrationEvent.cs` + repo method |
| W2-C.3 | Notification digest service | 1.5h | `Messaging.Infrastructure/BackgroundServices/NotificationDigestService.cs` + options class |
| W2-C.4 | TourGuide notification types | 30min | Extend `NotificationType` enum |
| W2-C.5 | Plan doc update | 30min | `Messaging-Workflow.md` |

**Read these before starting**: `Agents/Plans/Messaging-Audit-Report.md`, `Agents/Plans/Messaging-FixPlan.md`

### Verification

```powershell
dotnet build YallaJo.sln --no-restore

# Verify new services registered
Select-String -Path "Messaging.Infrastructure\DependencyInjection.cs" -Pattern "AddHostedService.*Sla|AddHostedService.*Digest"
```

### Acceptance criteria (10/10)

- [ ] All 14 validators in place (from W1-A)
- [ ] HybridCache on 4 query handlers (from W1-B)
- [ ] ISmsSender registered with NoOp default
- [ ] SLA monitoring service running on startup
- [ ] Digest service registered
- [ ] TourGuide notification types in enum
- [ ] Implementation Notes section added (in W1-E)

---

# WAVE 3 — Medium-Gap Modules (10-14 hours)

## Phase W3-A: Booking 8.5 → 10.0 (4-5 hours)

**Source plan**: `Agents/Plans/Booking-FixPlan.md` (10 fixes — 9 already applied, only 1 stub-replacement remains + 2 inbound handlers)
**Boost**: Booking +1.5

### Module-specific sub-phases

| Sub-phase | Fix | Effort | Files |
|-----------|-----|--------|-------|
| W3-A.1 | Replace 5 stub services with real implementations | 3h | StubBookingTourSnapshotReader → real; same for Pricing, Provider, Commission, Discount |
| W3-A.2 | Add TourArchivedIntegrationEventHandler | 30min | `Booking.Infrastructure/EventHandlers/TourArchivedIntegrationEventHandler.cs` |
| W3-A.3 | Add GuideOfferingSuspendedIntegrationEventHandler | 30min | Same folder. **NOTE**: Requires `GuideOfferingSuspendedIntegrationEvent` to be added to `ContentTours.Contracts` first |
| W3-A.4 | Remove duplicate TourGuide entity in Booking.Domain | 30min | Delete `Booking.Domain/Entities/TourGuide.cs` + retarget any references |
| W3-A.5 | Plan doc update | 30min | `Booking-Workflow.md` |

**Read these before starting**: `Agents/Plans/Booking-Audit-Report.md`, `Agents/Plans/Booking-FixPlan.md`

### Verification

```powershell
dotnet build YallaJo.sln --no-restore

# Verify no stubs remain
Select-String -Path "Booking.Infrastructure\**\*Stub*.cs" -Pattern "class Stub" -Recurse
# Expect: 0 matches
```

### Acceptance criteria (10/10)

- [ ] Zero stub services in production paths
- [ ] Both inbound event handlers wired
- [ ] No duplicate TourGuide entity in Booking.Domain
- [ ] Implementation Notes section added (in W1-E)

---

## Phase W3-B: Social 7.8 → 10.0 (4-5 hours)

**Source plan**: `Agents/Plans/Social-FixPlan.md`
**Prerequisites**: W1-A (5 validators), W1-B (HybridCache), W1-C (auth gates)
**Boost**: Social +2.2

### Module-specific sub-phases

| Sub-phase | Fix | Effort | Key Files |
|-----------|-----|--------|-----------|
| W3-B.1 | Public review listing endpoint | 30min | New `GetPublicReviewsQuery` + handler + endpoint |
| W3-B.2 | Complete Warn/Ban workflow | 2h | 3 commands (Warn, Ban, Unban) + handlers + `UserWarnedIntegrationEvent` |
| W3-B.3 | Wire ReviewHelpfulVote to public endpoint | 30min | New endpoint (entity exists, no public route yet) |
| W3-B.4 | Permission catalog comment fix | 15min | `SocialPermissionCatalog.cs` — update comment to reflect actual 21 permissions |
| W3-B.5 | Plan doc update | 30min | `Social-Workflow.md` |

**Read these before starting**: `Agents/Plans/Social-Audit-Report.md`, `Agents/Plans/Social-FixPlan.md`

### Acceptance criteria (10/10)

- [ ] Public reviews endpoint with pagination + HybridCache
- [ ] Warn/Ban commands functional end-to-end
- [ ] ReviewHelpfulVote exposed via endpoint
- [ ] Permission catalog comment accurate
- [ ] Implementation Notes section added (in W1-E)

---

## Phase W3-C: TourGuide-Flow 9.0 → 10.0 (2-3 hours)

**Source plan**: `Agents/Plans/TourGuide-Flow-FixPlan.md`
**Boost**: TourGuide +1.0

### Module-specific sub-phases

| Sub-phase | Fix | Effort | Files |
|-----------|-----|--------|-------|
| W3-C.1 | Delete `TourTourGuide` entity + repo | 30min | `ContentTours.Domain/Entities/TourTourGuide.cs` + `ITourTourGuideRepository.cs` + impl |
| W3-C.2 | Retarget AssignTourGuideCommandHandler → GuideTourOffering | 1h | `ContentTours.Application/Commands/.../AssignTourGuide/AssignTourGuideCommandHandler.cs` |
| W3-C.3 | Retarget UnassignTourGuideCommandHandler → GuideTourOffering | 30min | Same folder pattern |
| W3-C.4 | Retarget GetTourGuidesQueryHandler → GuideTourOffering | 30min | `ContentTours.Application/Queries/.../GetTourGuidesQueryHandler.cs` |
| W3-C.5 | Generate EF migration to drop TourTourGuide table | 15min | `dotnet ef migrations add DropTourTourGuide --project ContentTours.Infrastructure --startup-project YallaJo.WebApi` |
| W3-C.6 | Plan doc refresh with out-of-scope endpoints | 30min | `TourGuide-Flow.md` |

### Verification

```powershell
dotnet build YallaJo.sln --no-restore

# Verify zero references
Select-String -Path "**\*.cs" -Pattern "TourTourGuide" -Recurse -Path C:\Users\admin1\source\repos\YallaJo
# Expect: 0 matches (except in migration files)
```

### Acceptance criteria (10/10)

- [ ] Zero references to `TourTourGuide` in non-migration code
- [ ] AssignTourGuide / UnassignTourGuide / GetTourGuides work via GuideTourOffering
- [ ] EF migration generated (apply manually with `dotnet ef database update` when ready)
- [ ] Implementation Notes section added (in W1-E)

---

# WAVE 4 — Low-Polish Modules (6-8 hours)

> All 6 phases independent — run in parallel via separate `task()` calls.

## Phase W4-A: ContentCore 9.2 → 10.0 (1-2 hours)

**Source plan**: `Agents/Plans/ContentCore-FixPlan.md` (12 fixes — all code-side applied)

| Sub-phase | Fix | Effort |
|-----------|-----|--------|
| W4-A.1 | EF migration for: `Attachment.IsMarkedForDeletion`, `TranslationCache.HasMaxLength(4000)`, `Category.ParentCategoryId` index | 1h |
| W4-A.2 | Category cycle detection (multi-level, not just self-parent) | 30min |
| W4-A.3 | OpenAPI response code annotations on Attachment/EntityLink/Translation/Specialization routes | 30min |

### Verification

```powershell
dotnet ef migrations add ContentCoreSchemaPolish --project ContentCore.Infrastructure --startup-project YallaJo.WebApi
dotnet build YallaJo.sln --no-restore
```

---

## Phase W4-B: ContentSeo 8.8 → 10.0 (1 hour)

**Source plan**: `Agents/Plans/ContentSeo-FixPlan.md` (7 LOW fixes — mostly doc)

| Sub-phase | Fix | Effort |
|-----------|-----|--------|
| W4-B.1 | Remove 4 dead permissions from `ContentSeo.Contracts/Authorization/ContentSeoPermissionCatalog.cs` | 15min |
| W4-B.2 | Doc numerical corrections (23 endpoints, 21 handlers, 4 dead removed) in `ContentSeo-Workflow.md` | 30min |
| W4-B.3 | Implementation Notes section | 15min |

---

## Phase W4-C: ContentPlaces 9.5 → 10.0 (1 hour)

**Source plan**: `Agents/Plans/ContentPlaces-FixPlan.md` (9 fixes — all applied)

| Sub-phase | Fix | Effort |
|-----------|-----|--------|
| W4-C.1 | Verify no pending EF migration needed | 15min |
| W4-C.2 | Doc minor refresh — confirm 41 endpoints, 15 events, 13 handlers | 30min |
| W4-C.3 | Implementation Notes section | 15min |

---

## Phase W4-D: BlogCreatorPost-Merger 9.3 → 10.0 (1 hour)

**Source plan**: `Agents/Plans/BlogCreatorPost-Merger-FixPlan.md` (7 fixes — all applied)

| Sub-phase | Fix | Effort |
|-----------|-----|--------|
| W4-D.1 | Delete empty `ContentBlogs.Application/Commands/Creator/Posts/` folder | 5min |
| W4-D.2 | Delete empty `ContentBlogs.Application/Queries/Creator/Posts/` folder | 5min |
| W4-D.3 | Grep verify: zero `CreatorPost` references (excluding comments) | 30min |
| W4-D.4 | Archive plan doc with "Merger Complete" banner | 15min |
| W4-D.5 | Implementation Notes section | 5min |

### Verification

```powershell
Select-String -Path "**\*.cs" -Pattern "CreatorPost" -Recurse -Path C:\Users\admin1\source\repos\YallaJo | Where-Object { $_.Line -notmatch "//.*Merged|removed|deleted|legacy" }
# Expect: 0 matches (or only intentional comments documenting the removal)
```

---

## Phase W4-E: Platform-Onboarding 9.0 → 10.0 (1.5 hours)

**Source plan**: `Agents/Plans/Platform-Onboarding-FixPlan.md` (8 fixes — all applied)

| Sub-phase | Fix | Effort |
|-----------|-----|--------|
| W4-E.1 | EF migration check for Reapply() flow | 15min |
| W4-E.2 | Integration tests for Rejected→Draft→Submit reapplication | 1h |
| W4-E.3 | Doc archive with completion status | 15min |

---

## Phase W4-F: Role-System 9.5 → 10.0 (30 min)

**Source plan**: `Agents/Plans/Role-System-FixPlan.md` (4 fixes — all applied)

| Sub-phase | Fix | Effort |
|-----------|-----|--------|
| W4-F.1 | Integration test: verify 8 roles + permission claims | 30min |
| W4-F.2 | Doc archive with completion status | 5min |
| W4-F.3 | Implementation Notes section | 10min |

---

# Final Verification Phase (1-2 hours)

## V1: Build & Warnings

```powershell
cd C:\Users\admin1\source\repos\YallaJo
dotnet build YallaJo.sln --no-restore
# Expect: 0 errors, warning count ≤ baseline + 5
```

## V2: Test Suite (if exists)

```powershell
dotnet test YallaJo.sln --no-build
# Expect: all green
```

## V3: Re-Audit All 12 Plans

Dispatch 12 parallel explore agents (one per module) to verify plan claims against code:

```
For each plan: task(subagent_type='explore', prompt='Verify {Module}-Workflow.md claims against actual code in {Module}.Domain, .Application, .Infrastructure, .Presentation, .Contracts. Report any mismatches.', run_in_background=true)
```

Expected result: 0 mismatches per plan.

## V4: Score Recalculation

Update tracking table at bottom with new scores. Goal: 12/12 at 10.0/10.

## V5: Architectural Linting

```powershell
# Check no DbContext in any Presentation project
Select-String -Path "**\*.Presentation\**\*.cs" -Pattern "DbContext\s+\w+" -Recurse

# Check no anonymous mutations remain
ast_grep --pattern 'MapPost($_, $_).$$$.AllowAnonymous()' --lang csharp .
ast_grep --pattern 'MapPut($_, $_).$$$.AllowAnonymous()' --lang csharp .
ast_grep --pattern 'MapDelete($_, $_).$$$.AllowAnonymous()' --lang csharp .

# Verify all integration events registered
Select-String -Path "**\IntegrationEventTypeRegistry.cs" -Pattern '\["' | Measure-Object
```

---

# Risk Mitigation

| Risk | Detection | Mitigation |
|------|-----------|-----------|
| Cache invalidation bugs cause stale data | Manual test on cached endpoint after write | Conservative TTLs (15-60s for dynamic data) |
| CQRS refactors break Analytics dashboard performance | Benchmark before/after | Add HybridCache to slow queries |
| Removing TourTourGuide breaks assign/unassign | Run integration tests | Find all references first, batch rename |
| EF migrations conflict | Review migration SQL before apply | Generate SQL with `--script` flag, peer review |
| Doc updates introduce stale counts again | Re-grep after every plan touch | Use grep-derived counts only |
| Build failures cascade across waves | Run build after every phase | Atomic commits, easy rollback per phase |
| Validator changes break existing integration tests | Run tests after each W1-A batch | Add validators in small batches (5 at a time) |

---

# Rollback Strategy

Each phase is git-atomic.

```powershell
git log --oneline -20  # find phase commit
git revert <commit-hash>
dotnet build YallaJo.sln --no-restore  # verify rollback clean
```

For multi-file phases, use staging commit:
```powershell
git add <files>
git commit -m "Phase W2-A.3: Guide dashboard endpoints"
# If anything breaks downstream: git revert HEAD
```

---

# Architectural Invariants (Must Hold After Every Phase)

Verify after each phase:

1. **Build passes**: `dotnet build YallaJo.sln --no-restore` returns 0 errors
2. **No DbContext in Presentation**: Endpoints only consume `ISender`
3. **All commands return Result<T>**: No exceptions thrown from handlers (except infra)
4. **All entities have private setters**: Public mutation through methods only
5. **No `.AllowAnonymous()` on mutations**: Unless explicitly documented as public-write
6. **All `IPermissionCatalog` entries used**: No dead permissions
7. **All integration events registered in `IntegrationEventTypeRegistry`**: Else they don't dispatch
8. **All migrations forward-compatible**: Don't drop columns still referenced

---

# Tracking Table (Update as you go)

| Phase | Effort | Status | Started | Completed | Score Before | Score After |
|-------|--------|--------|---------|-----------|--------------|-------------|
| W1-A Validators | 3-4h | ⏳ | — | — | — | — |
| W1-B HybridCache | 3-4h | ⏳ | — | — | — | — |
| W1-C Auth Gates | 2-3h | ⏳ | — | — | — | — |
| W1-D CQRS Bypass | 2-3h | ⏳ | — | — | — | — |
| W2-A Analytics | 8-10h | ⏳ | — | — | 6.8 | — |
| W2-B Finance | 6-8h | ⏳ | — | — | 7.0 | — |
| W2-C Messaging | 6-7h | ⏳ | — | — | 7.5 | — |
| W3-A Booking | 4-5h | ⏳ | — | — | 8.5 | — |
| W3-B Social | 4-5h | ⏳ | — | — | 7.8 | — |
| W3-C TourGuide | 2-3h | ⏳ | — | — | 9.0 | — |
| W4-A ContentCore | 1-2h | ⏳ | — | — | 9.2 | — |
| W4-B ContentSeo | 1h | ⏳ | — | — | 8.8 | — |
| W4-C ContentPlaces | 1h | ⏳ | — | — | 9.5 | — |
| W4-D BlogMerger | 1h | ⏳ | — | — | 9.3 | — |
| W4-E Onboarding | 1.5h | ⏳ | — | — | 9.0 | — |
| W4-F Role-System | 30min | ⏳ | — | — | 9.5 | — |
| W1-E Doc Batch | 1-2h | ⏳ | — | — | — | — |
| Final Verify | 1-2h | ⏳ | — | — | — | — |

Legend: ⏳ Pending • 🔄 In Progress • ✅ Complete • ⚠️ Blocked • ❌ Failed (rolled back)

---

# Quick-Win Sub-Plan (If Time-Constrained — 6-8 hours total)

If you only have one day, do this for maximum score-per-hour:

1. **Hour 1-2**: W1-A (validators) — boosts 4 modules
2. **Hour 3**: W1-C (auth gates) — boosts 2 high-gap modules
3. **Hour 4**: W4-B (ContentSeo cleanup) — gets one plan to 10
4. **Hour 5**: W4-D (BlogMerger cleanup) — gets one plan to 10
5. **Hour 6**: W4-F (Role-System tests) — gets one plan to 10
6. **Hour 7-8**: W1-E (doc batch updates for all 12) — +0.2 each

**Result after 8 hours**: Average score 8.0 → 9.4, with 3 plans hitting 10/10.

---

# Per-Module Detailed Plan Cross-Reference

| Plan | Audit Report | Fix Plan |
|------|--------------|----------|
| ContentCore | `Agents/Plans/ContentCore-Audit-Report.md` | `Agents/Plans/ContentCore-FixPlan.md` |
| Role-System | `Agents/Plans/Role-System-Audit-Report.md` | `Agents/Plans/Role-System-FixPlan.md` |
| Platform-Onboarding | `Agents/Plans/Platform-Onboarding-Audit-Report.md` | `Agents/Plans/Platform-Onboarding-FixPlan.md` |
| BlogCreatorPost-Merger | `Agents/Plans/BlogCreatorPost-Merger-Audit-Report.md` | `Agents/Plans/BlogCreatorPost-Merger-FixPlan.md` |
| ContentPlaces | `Agents/Plans/ContentPlaces-Audit-Report.md` | `Agents/Plans/ContentPlaces-FixPlan.md` |
| TourGuide-Flow | `Agents/Plans/TourGuide-Flow-Audit-Report.md` | `Agents/Plans/TourGuide-Flow-FixPlan.md` |
| Booking | `Agents/Plans/Booking-Audit-Report.md` | `Agents/Plans/Booking-FixPlan.md` |
| Analytics | `Agents/Plans/Analytics-Audit-Report.md` | `Agents/Plans/Analytics-FixPlan.md` |
| ContentSeo | `Agents/Plans/ContentSeo-Audit-Report.md` | `Agents/Plans/ContentSeo-FixPlan.md` |
| Finance | `Agents/Plans/Finance-Audit-Report.md` | `Agents/Plans/Finance-FixPlan.md` |
| Messaging | `Agents/Plans/Messaging-Audit-Report.md` | `Agents/Plans/Messaging-FixPlan.md` |
| Social | `Agents/Plans/Social-Audit-Report.md` | `Agents/Plans/Social-FixPlan.md` |

> **Before starting any phase**, the Sisyphus worker MUST read the corresponding fix plan — it contains exact rule details, command signatures, and validation patterns.

---

# Optimal 2-Week Schedule

```
WEEK 1 (cross-cutting + high-impact):
  Mon: W1-A (validators) + W1-D (CQRS bypass) — parallel via 2 task() calls
  Tue: W1-B (HybridCache) + W1-C (auth gates) — parallel
  Wed: W2-A (Analytics) — full day
  Thu: W2-B (Finance) — full day
  Fri: W2-C (Messaging) — full day

WEEK 2 (medium + polish):
  Mon: W3-A (Booking) + W3-B (Social) — parallel
  Tue: W3-C (TourGuide) + W4-A (ContentCore) + W4-B (ContentSeo) — parallel
  Wed: W4-C + W4-D + W4-E + W4-F — 4 parallel quick tasks
  Thu: W1-E doc batch updates for all 12 plans
  Fri: Final Verify + integration test sweep + score recalculation
```

---

# Done Criteria for "All Plans at 10/10"

- [ ] All 12 plan docs updated with Implementation Notes
- [ ] All 12 plans verified by re-audit explore agents — 0 mismatches
- [ ] All cross-cutting concerns resolved (validators, cache, auth, CQRS)
- [ ] All HIGH-gap modules feature-complete (Analytics, Finance, Messaging)
- [ ] All MEDIUM-gap modules cleaned up (Booking, Social, TourGuide)
- [ ] All LOW-gap modules polished (ContentCore, ContentSeo, ContentPlaces, Blog, Onboarding, Role)
- [ ] Build: 0 errors, warning count stable
- [ ] (Optional) Integration tests: all green
- [ ] Final score recalculation: 12/12 at 10.0/10

When all checkboxes ticked, this document becomes the **execution log** — commit it to git as the historical record.

---

# Sisyphus Worker Delegation Patterns

This is a planning artifact. To execute, dispatch Sisyphus workers via `task()`:

### Single-phase delegation (typical)

```
task(
  category='deep',
  load_skills=[],
  description='Execute Phase W1-A: Add 19 validators',
  prompt='Execute Phase W1-A of .sisyphus/Master-ExecutionPlan-To10.md. Read Agents/Plans/Messaging-FixPlan.md for exact validation rules. Read Agents/Plans/Social-FixPlan.md for Social validators. Create 19 validator files. Verify build passes. Report files created and build status.',
  run_in_background=false
)
```

### Multi-phase parallel delegation (for independent waves)

```
# Wave 4 - dispatch 6 parallel quick tasks
task(subagent_type='build', ..., run_in_background=true) × 6
```

### Re-audit verification (post-execution)

```
# 12 parallel verification agents
task(subagent_type='explore', prompt='Verify {Module}-Workflow.md against code', run_in_background=true) × 12
```

---

**This plan is now the canonical execution document for raising all 12 audited plans to 10/10.**

When Sisyphus workers complete phases, they should append progress to the tracking table and commit the change.
