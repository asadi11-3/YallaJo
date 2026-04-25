# Plan: 8 Missing Integration Event Handlers (ContentSeo + Messaging)

> **Scope**: 8 handlers across 2 modules.
> **Why**: 8 integration events were added to `ContentPlaces.Contracts` + 1 to `ContentTours.Contracts` in the previous fix pass. Only 1 has a consumer (`PlaceTourCountUpdatedIntegrationEventHandler` in ContentPlaces). The remaining events hit the outbox, get polled, and fail to dispatch because no `INotificationHandler<IntegrationEventNotification<T>>` is registered. Every retry fails until dead-lettered.
> **Status at planning**: Build clean (0 errors, 183 tests). Adding these handlers closes the event loop and unblocks: owner notifications on business status changes + SEO sitemap auto-population.
>
> **Related files**:
> - `Agents/agent-context.md` §2.6 (Outbox/Inbox Atomicity) · §3.4 (Integration Events)
> - `Agents/agent-context.md` gotcha #25 (non-aggregate outbox) + #28 (registry requirement)
> - `Agents/ContentPlaces-fixes-required.md` (events catalog)
> - `Agents/ContentPlaces-remaining-fix-plan.md` §Phase 2.1/2.2 (events already created)
> - `ContentPlaces.Contracts/IntegrationEvents/*.cs` · `ContentTours.Contracts/*.cs`

---

## 1. Event → Handler Map

| # | Event | Published by | Consumer module | Handler to create | Side effect |
|---|-------|--------------|-----------------|-------------------|-------------|
| 1 | `PlaceCreatedIntegrationEvent` | ContentPlaces | ContentSeo | `PlaceCreatedIntegrationEventHandler` | Insert `SeoMetadata` + `SitemapEntry` |
| 2 | `PlaceUpdatedIntegrationEvent` | ContentPlaces | ContentSeo | `PlaceUpdatedIntegrationEventHandler` | Update `SitemapEntry.LastModified` + slug URL |
| 3 | `PlaceDeletedIntegrationEvent` | ContentPlaces | ContentSeo | `PlaceDeletedIntegrationEventHandler` | Mark `SeoMetadata` + `SitemapEntry` inactive |
| 4 | `BusinessCreatedIntegrationEvent` | ContentPlaces | ContentSeo | `BusinessCreatedIntegrationEventHandler` | Insert `SeoMetadata` + `SitemapEntry` (inactive until approved) |
| 5 | `BusinessApprovedIntegrationEvent` | ContentPlaces | Messaging | `BusinessApprovedIntegrationEventHandler` | Create `Notification` for owner |
| 6 | `BusinessRejectedIntegrationEvent` | ContentPlaces | Messaging | `BusinessRejectedIntegrationEventHandler` | Create `Notification` for owner with reason |
| 7 | `BusinessSuspendedIntegrationEvent` | ContentPlaces | Messaging | `BusinessSuspendedIntegrationEventHandler` | Create `Notification` for owner with reason (Critical) |
| 8 | `BusinessReinstatedIntegrationEvent` | ContentPlaces | Messaging | `BusinessReinstatedIntegrationEventHandler` | Create `Notification` for owner |

> `ServiceItemCreatedIntegrationEvent` + `ServiceItemDeletedIntegrationEvent` are **intentionally unhandled** for now — primary consumer is Analytics module which isn't started yet. The outbox will keep the rows pending; when an Analytics handler ships, retries will deliver historic events via outbox replay. Flag these in the work log as "ACTIVE EVENT, NO SUBSCRIBER — acceptable, will be consumed by Analytics when it ships".

---

## 2. Cross-Cutting Business Rules (apply to every handler)

### Rule A — Inbox idempotency first, mark processed last

Every handler follows the invariant order:

1. `if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;`
2. Do the business work (create/update entities — **in memory only, no SaveChanges**)
3. `inboxStore.MarkAsProcessed(notification.MessageId);`
4. `await unitOfWork.SaveChangesAsync(ct);` — ONE atomic commit

Violation consequence: outbox retries re-run side effects (duplicate `Notification` rows, double `SeoMetadata` inserts → DB constraint violations → dead-lettered).

### Rule B — Never call external services inside handlers

Handlers only stage **in-process entities**. No email send, no HTTP, no SMS. Those are delivered by dedicated `BackgroundService`s that poll the Notification table (or equivalent).

Why: external calls during outbox dispatch break idempotency. If SMTP succeeds but SaveChanges fails, the user gets an email for an event that didn't "happen" durably. External effects go AFTER commit via background polling.

### Rule C — Respect user `NotificationPreference`

For Messaging handlers, before creating a `Notification`, check `NotificationPreference(UserId, Type, Channel)`. Defaults when no row exists:
- InApp: always on
- Email: opt-in only
- Push: opt-in only
- SMS: opt-in only

**Exception — Critical notifications override preferences**:
- `BusinessSuspendedIntegrationEventHandler` creates **InApp + Email** unconditionally (revenue-blocking event, user must know)
- `BusinessRejectedIntegrationEventHandler` creates InApp + Email (user may not log in for days; needs email to see rejection reason)

### Rule D — Every write is a new row, never modify historic events

For Messaging: `Notification` rows are append-only. If an event arrives twice (beyond inbox retry), that's a different user experience than "update the existing notification". Preserve audit trail.

For ContentSeo: `SeoMetadata` and `SitemapEntry` ARE mutated (one row per entity) but mutations are minimal — `IsActive` toggle, `LastModified` update, `Url` change on slug rename.

### Rule E — Entity not found is a log + return, not an exception

If `PlaceDeletedIntegrationEvent` arrives but the SEO records don't exist: log warning, mark inbox processed, return. The event is idempotent — no SEO record = nothing to deactivate.

If `BusinessApprovedIntegrationEvent` arrives but the Business's `OwnerId` is missing: log warning, skip. The caller already has `OwnerId` in the event payload — this is defensive.

### Rule F — All handler implementations live in `{Module}.Infrastructure/EventHandlers/`

Per gotcha #22 in `agent-context.md`: domain event handlers need the DbContext, which is Infrastructure. Application-layer handlers would require a `DbContext` reference — arch violation. Pattern matches existing `LanguageActivatedIntegrationEventHandler` in both modules.

---

## 3. Foundation Work (prerequisite for handlers)

Messaging module is barer than ContentSeo. Needs 5 foundation items before handlers can be written.

### 3.1 Messaging — create Inbox + UnitOfWork abstractions

**Files to create**:

```
Messaging.Application/Interfaces/IMessagingInboxStore.cs
Messaging.Application/Interfaces/IMessagingUnitOfWork.cs
Messaging.Infrastructure/Persistence/MessagingInboxStore.cs
Messaging.Infrastructure/Persistence/MessagingUnitOfWork.cs
```

**Pattern** (mirror `ContentSeoInboxStore` / `ContentSeoUnitOfWork`):

```csharp
// Messaging.Application/Interfaces/IMessagingInboxStore.cs
using YallaJo.SharedKernel.Application.Abstractions.Data;
namespace Messaging.Application.Interfaces;
public interface IMessagingInboxStore : IInboxStore { }

// Messaging.Application/Interfaces/IMessagingUnitOfWork.cs
namespace Messaging.Application.Interfaces;
public interface IMessagingUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// Messaging.Infrastructure/Persistence/MessagingInboxStore.cs
internal sealed class MessagingInboxStore(MessagingDbContext dbContext) : IMessagingInboxStore
{
    public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct)
        => dbContext.InboxMessages.AnyAsync(m => m.Id == messageId, ct);

    public void MarkAsProcessed(Guid messageId)
        => dbContext.InboxMessages.Add(new InboxMessage { Id = messageId, ProcessedOnUtc = DateTime.UtcNow });
}

// Messaging.Infrastructure/Persistence/MessagingUnitOfWork.cs
internal sealed class MessagingUnitOfWork(IUnitOfWork<MessagingDbContext> inner) : IMessagingUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => inner.SaveChangesAsync(ct);
}
```

**Register in DI** (`Messaging.Infrastructure/DependencyInjection.cs`):
```csharp
services.AddScoped<IMessagingUnitOfWork, MessagingUnitOfWork>();
services.AddScoped<IMessagingInboxStore, MessagingInboxStore>();
```

### 3.2 Messaging — add `InboxMessages` DbSet + EF config

**Current state**: `MessagingDbContext` has OutboxMessages but NO InboxMessages. Must add.

```csharp
// MessagingDbContext.cs — add:
public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
```

Create `Messaging.Infrastructure/Persistence/Configurations/InboxMessageConfiguration.cs` by copying the pattern from any other module (e.g. `ContentSeo.Infrastructure/Persistence/Configurations/InboxMessageConfiguration.cs` — already exists there per dir listing).

**Generate migration**:
```powershell
dotnet ef migrations add Messaging_AddInboxMessagesTable `
  --project Messaging.Infrastructure `
  --startup-project YallaJo.Api `
  --context MessagingDbContext
```

### 3.3 ContentSeo.Infrastructure — add ContentPlaces.Contracts reference

**Current `ContentSeo.Infrastructure.csproj`** has no reference to `ContentPlaces.Contracts`. Handler would fail to compile.

```xml
<!-- Add to ItemGroup: -->
<ProjectReference Include="..\ContentPlaces.Contracts\ContentPlaces.Contracts.csproj" />
```

### 3.4 Messaging.Infrastructure — add ContentPlaces.Contracts reference

Same as above for Messaging.

```xml
<ProjectReference Include="..\ContentPlaces.Contracts\ContentPlaces.Contracts.csproj" />
```

### 3.5 Domain entity factory methods (currently missing)

Both modules' entities are anemic — no `Create()` factories. Handlers cannot construct entities via property setters (setters are private). Need to add factories.

**`Messaging.Domain/Entities/Notification.cs`** — add factory:
```csharp
public static Notification Create(
    Guid userId,
    NotificationType type,
    NotificationChannel channel,
    string title,
    string body,
    NotificationPriority priority = NotificationPriority.Medium,
    string? data = null,
    string? entityType = null,
    Guid? entityId = null)
{
    if (userId == Guid.Empty)
        throw new ArgumentException("UserId cannot be empty.", nameof(userId));
    if (string.IsNullOrWhiteSpace(title))
        throw new ArgumentException("Title is required.", nameof(title));
    if (string.IsNullOrWhiteSpace(body))
        throw new ArgumentException("Body is required.", nameof(body));

    return new Notification
    {
        Id         = Guid.CreateVersion7(),
        UserId     = userId,
        Type       = type,
        Channel    = channel,
        Priority   = priority,
        Title      = title.Trim(),
        Body       = body.Trim(),
        Data       = data,
        IsRead     = false,
        EntityType = entityType,
        EntityId   = entityId,
    };
}

public void MarkSent() { SentAt = DateTime.UtcNow; }
public void MarkRead() { IsRead = true; ReadAt = DateTime.UtcNow; }
```

**`Messaging.Domain/Enums/NotificationType.cs`** — add `Business` type:
```csharp
public enum NotificationType : byte
{
    System = 0,
    Booking = 1,
    Payment = 2,
    Review = 3,
    Promotion = 4,
    Social = 5,
    Chat = 6,
    Business = 7,  // ← ADD
}
```

**`ContentSeo.Domain/Entities/SeoMetadata.cs`** — add factory + business methods:
```csharp
public static SeoMetadata Create(
    SeoEntityType entityType,
    Guid entityId,
    string? metaTitle = null,
    string? metaDescription = null,
    string? canonicalUrl = null,
    decimal sitemapPriority = 0.5m,
    string? sitemapChangeFrequency = "weekly")
{
    if (entityId == Guid.Empty)
        throw new ArgumentException("EntityId cannot be empty.", nameof(entityId));
    if (sitemapPriority is < 0m or > 1m)
        throw new ArgumentOutOfRangeException(nameof(sitemapPriority),
            "SitemapPriority must be between 0.0 and 1.0.");

    return new SeoMetadata
    {
        Id                      = Guid.CreateVersion7(),
        EntityType              = entityType,
        EntityId                = entityId,
        MetaTitle               = metaTitle?.Trim(),
        MetaDescription         = metaDescription?.Trim(),
        CanonicalUrl            = canonicalUrl?.Trim(),
        SitemapPriority         = sitemapPriority,
        SitemapChangeFrequency  = sitemapChangeFrequency,
    };
}

public void UpdateMeta(string? metaTitle, string? metaDescription)
{
    MetaTitle       = metaTitle?.Trim();
    MetaDescription = metaDescription?.Trim();
    MarkUpdated();
}
```

**`ContentSeo.Domain/Entities/SitemapEntry.cs`** — add factory + business methods:
```csharp
public static SitemapEntry Create(
    string url,
    string entityType,
    Guid? entityId = null,
    string? changeFrequency = "weekly",
    decimal? priority = 0.5m,
    bool isActive = true)
{
    if (string.IsNullOrWhiteSpace(url))
        throw new ArgumentException("Url is required.", nameof(url));
    if (string.IsNullOrWhiteSpace(entityType))
        throw new ArgumentException("EntityType is required.", nameof(entityType));

    return new SitemapEntry
    {
        Id              = Guid.CreateVersion7(),
        Url             = url.Trim(),
        EntityType      = entityType.Trim(),
        EntityId        = entityId,
        ChangeFrequency = changeFrequency,
        Priority        = priority,
        LastModified    = DateTime.UtcNow,
        IsActive        = isActive,
    };
}

public void Touch()
{
    LastModified = DateTime.UtcNow;
    MarkUpdated();
}

public void ChangeUrl(string newUrl)
{
    if (string.IsNullOrWhiteSpace(newUrl))
        throw new ArgumentException("Url is required.", nameof(newUrl));

    Url = newUrl.Trim();
    LastModified = DateTime.UtcNow;
    MarkUpdated();
}

public void Deactivate()
{
    IsActive = false;
    LastModified = DateTime.UtcNow;
    MarkUpdated();
}

public void Reactivate()
{
    IsActive = true;
    LastModified = DateTime.UtcNow;
    MarkUpdated();
}
```

**Repository / DbContext access**: Both modules already expose `DbContext.{Entity}` via DbSets. Handlers use `dbContext.SeoMetadata.Add(...)`, `dbContext.Notifications.Add(...)` directly — non-aggregate, no `IRepository` needed.

---

## 4. Per-Handler Specifications

### 4.1 `PlaceCreatedIntegrationEventHandler` (ContentSeo)

**File**: `ContentSeo.Infrastructure/EventHandlers/PlaceCreatedIntegrationEventHandler.cs`

**Business rules**:
- Every new place gets an SEO baseline automatically.
- `SeoMetadata.MetaTitle` defaults to place name (editors can override later).
- `SitemapEntry.Url = "/places/{slug}"` — fixed URL schema. Admin UI can customise later.
- `SitemapEntry.IsActive = true` — place is visible in sitemap the moment it's created (admins hide via SEO UI if needed).
- **Idempotency beyond inbox**: if a `SeoMetadata` for `(Place, PlaceId)` already exists (rare — implies inbox + DB drift), skip creation. Log info.

**Inputs from event**:
```csharp
public sealed record PlaceCreatedIntegrationEvent(
    Guid PlaceId, string Name, string Slug) : IntegrationEventBase;
```

**Handler skeleton**:
```csharp
using ContentPlaces.Contracts.IntegrationEvents;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using ContentSeo.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

public sealed class PlaceCreatedIntegrationEventHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<PlaceCreatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PlaceCreatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<PlaceCreatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
            return;

        var evt = notification.Event;

        // Defensive: avoid duplicate SeoMetadata if DB is out of sync with inbox.
        var seoExists = await dbContext.SeoMetadata
            .AnyAsync(s => s.EntityType == SeoEntityType.Place && s.EntityId == evt.PlaceId, ct);

        if (!seoExists)
        {
            dbContext.SeoMetadata.Add(SeoMetadata.Create(
                entityType: SeoEntityType.Place,
                entityId:   evt.PlaceId,
                metaTitle:  evt.Name,
                sitemapPriority: 0.6m));   // places slightly higher than businesses
        }

        var sitemapExists = await dbContext.SitemapEntries
            .AnyAsync(s => s.EntityType == "Place" && s.EntityId == evt.PlaceId, ct);

        if (!sitemapExists)
        {
            dbContext.SitemapEntries.Add(SitemapEntry.Create(
                url:        $"/places/{evt.Slug}",
                entityType: "Place",
                entityId:   evt.PlaceId,
                changeFrequency: "weekly",
                priority:   0.6m,
                isActive:   true));
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "ContentSeo: initialised SEO for Place {PlaceId} (slug={Slug})",
            evt.PlaceId, evt.Slug);
    }
}
```

**Failure modes handled**:
- Duplicate SeoMetadata / SitemapEntry → skip insert (idempotent).
- Empty slug → factory throws → message dead-letters after `MaxRetryCount=10`. Should never happen (ContentPlaces validates slug before publishing).

### 4.2 `PlaceUpdatedIntegrationEventHandler` (ContentSeo)

**Business rules**:
- Refresh `SitemapEntry.LastModified` so search engines re-crawl.
- If slug changed: update `SitemapEntry.Url`.
- **Known limitation**: current `PlaceUpdatedIntegrationEvent` doesn't carry old slug → cannot auto-generate a `Redirect`. Logged as TODO in handler — ContentPlaces event shape would need `OldSlug` field for redirect generation.

**Event**:
```csharp
public sealed record PlaceUpdatedIntegrationEvent(
    Guid PlaceId, string Name, string Slug) : IntegrationEventBase;
// Note: does NOT carry old slug. Redirect generation is a future enhancement.
```

**Logic**:
```csharp
var sitemapEntry = await dbContext.SitemapEntries
    .FirstOrDefaultAsync(
        s => s.EntityType == "Place" && s.EntityId == evt.PlaceId, ct);

if (sitemapEntry is null)
{
    // Place exists but no sitemap — self-heal by creating one.
    logger.LogWarning("PlaceUpdated: SitemapEntry for Place {PlaceId} missing; creating.", evt.PlaceId);
    dbContext.SitemapEntries.Add(SitemapEntry.Create(
        url: $"/places/{evt.Slug}", entityType: "Place", entityId: evt.PlaceId,
        changeFrequency: "weekly", priority: 0.6m, isActive: true));
}
else
{
    var newUrl = $"/places/{evt.Slug}";
    if (sitemapEntry.Url != newUrl)
    {
        // TODO: Also create a 301 Redirect when OldSlug is added to the event payload.
        sitemapEntry.ChangeUrl(newUrl);
    }
    else
    {
        sitemapEntry.Touch();
    }
}

// Update MetaTitle only if it still matches the old name (don't clobber editor edits).
var meta = await dbContext.SeoMetadata
    .FirstOrDefaultAsync(
        s => s.EntityType == SeoEntityType.Place && s.EntityId == evt.PlaceId, ct);
// intentionally NOT auto-updating meta — editors own that field after creation.
```

### 4.3 `PlaceDeletedIntegrationEventHandler` (ContentSeo)

**Business rules**:
- Never hard-delete SEO records (audit trail + analytics).
- Flip `SitemapEntry.IsActive = false` (removes from sitemap.xml output).
- Leave `SeoMetadata` alive but its sitemap exposure is gone.
- If records don't exist: log + return (idempotent).

**Logic**:
```csharp
var sitemapEntry = await dbContext.SitemapEntries
    .FirstOrDefaultAsync(s => s.EntityType == "Place" && s.EntityId == evt.PlaceId, ct);

if (sitemapEntry is not null && sitemapEntry.IsActive)
    sitemapEntry.Deactivate();

// SeoMetadata is left intact — historical reference.

inboxStore.MarkAsProcessed(notification.MessageId);
await unitOfWork.SaveChangesAsync(ct);
```

### 4.4 `BusinessCreatedIntegrationEventHandler` (ContentSeo)

**Business rules**:
- A new business is `Pending` status (awaiting admin approval) → **SEO records are created INACTIVE**. They become active only after `BusinessApproved` event.
- Prevents un-moderated business pages from appearing in sitemap / search.
- On approval, a separate ContentSeo handler flips sitemap to active. (Phase 2 — not in this PR.)
- For now, the handler creates records with `SitemapEntry.IsActive = false`. A follow-up `BusinessApprovedIntegrationEventHandler` in ContentSeo will flip them. See §4.5 handler plan — it ONLY lives in Messaging for now. ContentSeo activation on approval is **Phase 2**.

**Simplification for this PR**: create SEO records but don't auto-activate. They stay inactive until an admin UI or Phase 2 handler activates them. Leaves a cleaner surface than coupling SEO to approval state.

**Payload**:
```csharp
public sealed record BusinessCreatedIntegrationEvent(
    Guid BusinessId, string Name, string Slug, Guid OwnerId, Guid? PlaceId) : IntegrationEventBase;
```

**Logic**: same pattern as Place but with `SeoEntityType.Business`, URL `/businesses/{slug}`, priority `0.5m`, `isActive: false`.

### 4.5 `BusinessApprovedIntegrationEventHandler` (Messaging)

**File**: `Messaging.Infrastructure/EventHandlers/BusinessApprovedIntegrationEventHandler.cs`

**Business rules**:
1. Create in-app `Notification` for `OwnerId` — always.
2. Check `NotificationPreference(OwnerId, NotificationType.Business, NotificationChannel.Email)` — if enabled OR no preference row, also create Email notification.
3. Priority: **High** — owner is waiting for this news.
4. Title: `"Business Approved"`, Body: `"Your business has been approved and is now live on YallaJo."`
5. `EntityType = "Business"`, `EntityId = BusinessId` — deep-link from notification to the business page.

**Event**:
```csharp
public sealed record BusinessApprovedIntegrationEvent(
    Guid BusinessId, Guid OwnerId) : IntegrationEventBase;
```

**Handler skeleton**:
```csharp
using ContentPlaces.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

public sealed class BusinessApprovedIntegrationEventHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<BusinessApprovedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<BusinessApprovedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<BusinessApprovedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
            return;

        var evt = notification.Event;

        // ── InApp always ──────────────────────────────────────────────────────
        dbContext.Notifications.Add(Notification.Create(
            userId:     evt.OwnerId,
            type:       NotificationType.Business,
            channel:    NotificationChannel.InApp,
            priority:   NotificationPriority.High,
            title:      "Business Approved",
            body:       "Your business has been approved and is now live on YallaJo.",
            entityType: "Business",
            entityId:   evt.BusinessId));

        // ── Email if opted-in (default opt-out — see Rule C) ──────────────────
        var emailOptIn = await IsChannelEnabledAsync(
            evt.OwnerId, NotificationType.Business, NotificationChannel.Email, defaultEnabled: false, ct);

        if (emailOptIn)
        {
            dbContext.Notifications.Add(Notification.Create(
                userId:     evt.OwnerId,
                type:       NotificationType.Business,
                channel:    NotificationChannel.Email,
                priority:   NotificationPriority.High,
                title:      "Business Approved",
                body:       "Your business has been approved and is now live on YallaJo.",
                entityType: "Business",
                entityId:   evt.BusinessId));
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Messaging: notifications queued for BusinessApproved Owner={OwnerId} Business={BusinessId} EmailOptIn={EmailOptIn}",
            evt.OwnerId, evt.BusinessId, emailOptIn);
    }

    private async Task<bool> IsChannelEnabledAsync(
        Guid userId, NotificationType type, NotificationChannel channel,
        bool defaultEnabled, CancellationToken ct)
    {
        var pref = await dbContext.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.UserId == userId && p.NotificationType == type && p.Channel == channel, ct);

        return pref?.IsEnabled ?? defaultEnabled;
    }
}
```

### 4.6 `BusinessRejectedIntegrationEventHandler` (Messaging)

**Business rules**:
- Same two channels as Approved (InApp always + Email).
- **Email is unconditional** (override preference) — user needs the rejection reason ASAP and may not log in.
- Priority: **High**.
- Title: `"Business Application Rejected"`.
- Body: `$"Your business application was rejected. Reason: {evt.Reason}"`.

**Event**:
```csharp
public sealed record BusinessRejectedIntegrationEvent(
    Guid BusinessId, Guid OwnerId, string Reason) : IntegrationEventBase;
```

### 4.7 `BusinessSuspendedIntegrationEventHandler` (Messaging)

**Business rules**:
- Priority: **Critical** (revenue-blocking).
- **Both InApp + Email** unconditional (override preference).
- Title: `"Business Suspended"`.
- Body: `$"Your business has been suspended. Reason: {evt.Reason}"`.
- Include remediation hint in body: `"Contact support@yallajo.com for next steps."`.

### 4.8 `BusinessReinstatedIntegrationEventHandler` (Messaging)

**Business rules**:
- Priority: **High**.
- InApp always + Email opt-in (same as Approved).
- Title: `"Business Reinstated"`.
- Body: `"Your business is active again and visible to customers."`.

---

## 5. File Checklist (23 files total)

### Foundation (5 files + 1 migration)
- [ ] `Messaging.Application/Interfaces/IMessagingInboxStore.cs` (new)
- [ ] `Messaging.Application/Interfaces/IMessagingUnitOfWork.cs` (new)
- [ ] `Messaging.Infrastructure/Persistence/MessagingInboxStore.cs` (new)
- [ ] `Messaging.Infrastructure/Persistence/MessagingUnitOfWork.cs` (new)
- [ ] `Messaging.Infrastructure/Persistence/Configurations/InboxMessageConfiguration.cs` (new)
- [ ] `MessagingDbContext.cs` — add `InboxMessages` DbSet (edit)
- [ ] EF migration: `Messaging_AddInboxMessagesTable`

### Project references (2 files)
- [ ] `ContentSeo.Infrastructure.csproj` — add `ContentPlaces.Contracts` reference
- [ ] `Messaging.Infrastructure.csproj` — add `ContentPlaces.Contracts` reference

### Entity business methods (4 files)
- [ ] `Messaging.Domain/Enums/NotificationType.cs` — add `Business = 7`
- [ ] `Messaging.Domain/Entities/Notification.cs` — add `Create`, `MarkSent`, `MarkRead`
- [ ] `ContentSeo.Domain/Entities/SeoMetadata.cs` — add `Create`, `UpdateMeta`
- [ ] `ContentSeo.Domain/Entities/SitemapEntry.cs` — add `Create`, `Touch`, `ChangeUrl`, `Deactivate`, `Reactivate`

### DI registration (2 files)
- [ ] `Messaging.Infrastructure/DependencyInjection.cs` — register `IMessagingUnitOfWork`, `IMessagingInboxStore`
- [ ] (ContentSeo DI already complete)

### Handlers (8 files)
- [ ] `ContentSeo.Infrastructure/EventHandlers/PlaceCreatedIntegrationEventHandler.cs`
- [ ] `ContentSeo.Infrastructure/EventHandlers/PlaceUpdatedIntegrationEventHandler.cs`
- [ ] `ContentSeo.Infrastructure/EventHandlers/PlaceDeletedIntegrationEventHandler.cs`
- [ ] `ContentSeo.Infrastructure/EventHandlers/BusinessCreatedIntegrationEventHandler.cs`
- [ ] `Messaging.Infrastructure/EventHandlers/BusinessApprovedIntegrationEventHandler.cs`
- [ ] `Messaging.Infrastructure/EventHandlers/BusinessRejectedIntegrationEventHandler.cs`
- [ ] `Messaging.Infrastructure/EventHandlers/BusinessSuspendedIntegrationEventHandler.cs`
- [ ] `Messaging.Infrastructure/EventHandlers/BusinessReinstatedIntegrationEventHandler.cs`

### Integration event registry update
- [ ] No change needed — all 4 Business events + 3 Place events are already registered (confirmed from prior work).

---

## 6. Execution Order

Sequential — later steps depend on earlier.

### Phase A — Foundation (≈1h)
1. Messaging inbox abstractions (§3.1)
2. Messaging `InboxMessages` DbSet + config + migration (§3.2)
3. Project references (§3.3, §3.4)
4. Entity factories + NotificationType (§3.5)
5. `dotnet build` — confirm 0 errors

### Phase B — ContentSeo handlers (≈1h)
6. `PlaceCreatedIntegrationEventHandler` (§4.1)
7. `PlaceUpdatedIntegrationEventHandler` (§4.2)
8. `PlaceDeletedIntegrationEventHandler` (§4.3)
9. `BusinessCreatedIntegrationEventHandler` (§4.4)
10. `dotnet build` — MediatR auto-registers handlers via `AddMediatR(RegisterServicesFromAssembly)` in ContentSeo DI; no manual wiring needed.

### Phase C — Messaging handlers (≈1.5h)
11. `BusinessApprovedIntegrationEventHandler` (§4.5) — includes shared `IsChannelEnabledAsync` helper
12. `BusinessRejectedIntegrationEventHandler` (§4.6)
13. `BusinessSuspendedIntegrationEventHandler` (§4.7)
14. `BusinessReinstatedIntegrationEventHandler` (§4.8)

### Phase D — Verification (≈30 min)
15. `dotnet build YallaJo.sln` — 0 errors
16. `dotnet test YallaJo.sln` — 183+ passed (no new tests required but existing must stay green)
17. Smoke test via Swagger: approve a business → check `messaging.Notifications` table for row
18. Smoke test: create a place → check `content_seo.SeoMetadata` + `content_seo.SitemapEntries`

---

## 7. Risk Register

| # | Risk | Mitigation |
|---|------|------------|
| R1 | Messaging doesn't have an inbox table — first module to receive events | Migration `Messaging_AddInboxMessagesTable`. Ensures composite OutboxProcessor can deduplicate. |
| R2 | `NotificationType.Business = 7` is a new enum value — existing serialised events won't break | Enum stored as `byte`. Value 7 is new, never written before. Safe. |
| R3 | ContentSeo auto-creates SEO records — editors may overwrite MetaTitle and expect event to NOT clobber | `PlaceUpdatedIntegrationEventHandler` intentionally does NOT update MetaTitle — only URL + LastModified. Editors own MetaTitle after creation. |
| R4 | Slug rename scenario — current events don't carry old slug → no 301 redirect | Documented as TODO. Add `OldSlug` to `PlaceUpdatedIntegrationEvent` in a later PR. Not blocking. |
| R5 | Critical notification channels bypass user preferences (Suspend/Reject) | Explicit business decision. Documented in Rule C. User cannot opt out of existential business events. |
| R6 | Email channel creates `Notification` rows but no actual SMTP send | Email dispatch is a separate `BackgroundService` that polls `Notifications` where `Channel=Email AND SentAt IS NULL`. That service is not in this PR. Notification rows simply sit pending until the dispatcher ships. Acceptable. |
| R7 | Two notifications created for Approved (InApp + Email) — user sees 2 rows | Intentional. InApp and Email are different delivery surfaces, user experience is one notification per channel. Matches industry standard (Slack, Linear, GitHub). |
| R8 | Handler writes to `Notifications` table which has no unique constraint on `(UserId, EntityId, Type)` — retries beyond inbox could duplicate | Inbox idempotency check is the primary defence. If inbox store is itself corrupted (unlikely), a background dedup job could merge. Out of scope. |
| R9 | `ServiceItemCreated/Deleted` events have no subscriber and will keep retrying | These events are published but no handler is registered. Outbox will retry until dead-letter (10× default). **Acceptable** for now — when Analytics module ships, handler will process backlog via dead-letter replay. Document in work log. Alternative: register a no-op handler to mark processed — but that defeats future replay. Leaving as-is is correct. |
| R10 | `MessagingDbContext` doesn't have `InboxMessage` configuration yet — migration must generate schema | Follow `ContentSeo.Infrastructure/Persistence/Configurations/InboxMessageConfiguration.cs` exactly. It already exists; copy pattern. |

---

## 8. Commit Strategy

Atomic commits, reviewable in isolation:

| # | Commit message | Scope |
|---|----------------|-------|
| 1 | `feat(Messaging): add inbox abstractions + InboxMessages table` | Phase A steps 1-2 + migration |
| 2 | `feat(Messaging): add Notification.Create factory + Business enum value` | Phase A step 4 (Messaging) |
| 3 | `feat(ContentSeo): add SeoMetadata + SitemapEntry factories and business methods` | Phase A step 4 (ContentSeo) |
| 4 | `chore: reference ContentPlaces.Contracts from ContentSeo + Messaging` | Phase A step 3 |
| 5 | `feat(ContentSeo): handle Place created/updated/deleted integration events` | Phase B steps 6-8 |
| 6 | `feat(ContentSeo): handle BusinessCreated integration event` | Phase B step 9 |
| 7 | `feat(Messaging): handle BusinessApproved integration event` | Phase C step 11 |
| 8 | `feat(Messaging): handle BusinessRejected integration event` | Phase C step 12 |
| 9 | `feat(Messaging): handle BusinessSuspended + BusinessReinstated integration events` | Phase C steps 13-14 |

---

## 9. Definition of Done

- [ ] `dotnet build YallaJo.sln` — 0 errors, no new warnings
- [ ] `dotnet test YallaJo.sln` — 183+ tests pass (no regressions)
- [ ] `lsp_diagnostics` clean on all 23 files
- [ ] Smoke test: `POST /api/v1/content-places/places` → row appears in both `content_seo.SeoMetadata` + `content_seo.SitemapEntries` within 10 s (outbox poll interval)
- [ ] Smoke test: `PATCH /api/v1/content-places/businesses/{id}/approve` → row appears in `messaging.Notifications` (InApp channel) within 10 s
- [ ] Smoke test: `PATCH .../suspend` → 2 rows in `messaging.Notifications` (InApp + Email, Priority=Critical)
- [ ] `messaging.InboxMessages` table grows as events are processed — no duplicate processing
- [ ] Work log updated in `agent-context.md` §11.2 with 9 commit references
- [ ] Module status updated: ContentSeo 🟡 → still 🟡 (endpoints remain empty), Messaging 🟡 → still 🟡 (endpoints remain empty, but event consumers live)
- [ ] `error-log.md` updated if any new mistakes discovered
- [ ] `agent-context.md` gotchas registry considered for new entries (e.g. "Critical notifications override user preferences" if not already captured)

---

## 10. Out of Scope (future work, not in this PR)

1. **Email dispatcher background service** — reads `Notifications WHERE Channel=Email AND SentAt IS NULL`, sends via SMTP, marks `SentAt`. Separate ticket.
2. **Push dispatcher background service** — same pattern for `Channel=Push` using `DeviceTokens`. Separate ticket.
3. **SEO activation on business approval** — ContentSeo subscribing to `BusinessApprovedIntegrationEvent` and flipping `SitemapEntry.IsActive = true`. Phase 2 — business modelling decision (do we want auto-publish or admin review?).
4. **Redirect generation on slug rename** — requires adding `OldSlug` to `PlaceUpdatedIntegrationEvent`. Separate event-shape change.
5. **NotificationTemplate usage** — current plan hard-codes Title/Body strings. A future enhancement pulls templates from `messaging.NotificationTemplates` with placeholders (`{{BusinessName}}`, `{{Reason}}`). Would support localisation. Separate ticket.
6. **`ServiceItemCreated/Deleted` Analytics handlers** — Analytics module not started. Events will queue in outbox; dead-letter replay once module ships.
7. **Push notifications to registered `DeviceTokens`** — needs FCM/APNS integration. Out of scope.

---

## 11. Quick Reference — Event-to-Effect Summary

```
PlaceCreated       → ContentSeo:  +SeoMetadata(Place)    +SitemapEntry(/places/{slug}, active)
PlaceUpdated       → ContentSeo:  SitemapEntry.Touch()   (+ change URL on slug rename)
PlaceDeleted       → ContentSeo:  SitemapEntry.Deactivate()
BusinessCreated    → ContentSeo:  +SeoMetadata(Business) +SitemapEntry(/businesses/{slug}, INACTIVE)

BusinessApproved   → Messaging:   +Notification(Owner, InApp, High)       [+Email if opted-in]
BusinessRejected   → Messaging:   +Notification(Owner, InApp, High)       +Notification(Owner, Email, High)
BusinessSuspended  → Messaging:   +Notification(Owner, InApp, Critical)   +Notification(Owner, Email, Critical)
BusinessReinstated → Messaging:   +Notification(Owner, InApp, High)       [+Email if opted-in]
```
