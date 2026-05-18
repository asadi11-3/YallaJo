# ContentBlogs & ContentSeo — Compliance Gaps & Event Topology

> **Scope:** Code-level findings only. WBS / hours / team / process out of scope per user direction.
> **Source of truth:** `Agents/Endpoints.pdf` + `Agents/YallaJo Business Rules & Edge Cases.pdf` + `Agents/YallaJo.md` + `Agents/agent-context.md`.
> **Audit references:** Sprint plan at `Agents/tasks/ContentBlogs-ContentSeo-team-tasks.md`.
> **Repo layout note:** Modules live at repo root (e.g. `ContentBlogs.Domain/...`), **not** under `src/Modules/`.

---

## TL;DR

The repo is **mostly built** — ContentSeo + Translation API + 2 background services + ~23 ContentBlogs endpoints are present. The remaining work is **compliance remediation**, not greenfield. There are **12 blockers** (8 PDF-rule violations + 4 event-topology bugs that will cause runtime failures or silent business-rule breaks), **5 warnings**, and **3 naming/code drift items**.

The single most dangerous finding: **the 4 ContentSeo integration events are not registered in `IntegrationEventTypeRegistry`**, so any FAQ / Redirect / SeoMetadata domain-event handler that tries to enqueue an outbox message will throw at runtime (the registry's `GetName(typeof(T))` is called by `OutboxMessage.Create`).

---

## 1. Event & Handler Topology

### 1.1 `IntegrationEventTypeRegistry` — full registration list

Source: `YallaJo.SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs`

| Line | Module | Logical Name | Event Class |
|------|--------|--------------|-------------|
| 22 | Security | `security.user.created.v1` | `UserCreatedIntegrationEvent` |
| 23 | Security | `security.user.email-verified.v1` | `EmailVerifiedIntegrationEvent` |
| 24 | Security | `security.user.password-changed.v1` | `PasswordChangedIntegrationEvent` |
| 25 | Security | `security.user.password-reset.v1` | `PasswordResetIntegrationEvent` |
| 26 | Security | `security.user.phone-updated.v1` | `PhoneNumberUpdatedIntegrationEvent` |
| 27 | Security | `security.user.lifecycle-changed.v1` | `UserLifecycleChangedIntegrationEvent` |
| 30 | Auth | `auth.user.logged-in.v1` | `UserLoggedInIntegrationEvent` |
| 31 | Auth | `auth.session.revoked.v1` | `SessionRevokedIntegrationEvent` |
| 34 | ContentCore | `content-core.language.activated.v1` | `LanguageActivatedIntegrationEvent` |
| 35 | ContentCore | `content-core.language.deactivated.v1` | `LanguageDeactivatedIntegrationEvent` |
| 36 | ContentCore | `content-core.attachment.uploaded.v1` | `AttachmentUploadedIntegrationEvent` |
| 37 | ContentCore | `content-core.attachment.deleted.v1` | `AttachmentDeletedIntegrationEvent` |
| 38 | ContentCore | `content-core.category.created.v1` | `CategoryCreatedIntegrationEvent` |
| 39 | ContentCore | `content-core.category.updated.v1` | `CategoryUpdatedIntegrationEvent` |
| 40 | ContentCore | `content-core.category.deleted.v1` | `CategoryDeletedIntegrationEvent` |
| 41 | ContentCore | `content-core.category.restored.v1` | `CategoryRestoredIntegrationEvent` |
| 44 | ContentPlaces | `content-places.place.created.v1` | `PlaceCreatedIntegrationEvent` |
| 45 | ContentPlaces | `content-places.place.updated.v1` | `PlaceUpdatedIntegrationEvent` |
| 46 | ContentPlaces | `content-places.place.deleted.v1` | `PlaceDeletedIntegrationEvent` |
| 49 | ContentPlaces | `content-places.business.created.v1` | `BusinessCreatedIntegrationEvent` |
| 50 | ContentPlaces | `content-places.business.approved.v1` | `BusinessApprovedIntegrationEvent` |
| 51 | ContentPlaces | `content-places.business.rejected.v1` | `BusinessRejectedIntegrationEvent` |
| 52 | ContentPlaces | `content-places.business.suspended.v1` | `BusinessSuspendedIntegrationEvent` |
| 53 | ContentPlaces | `content-places.business.reinstated.v1` | `BusinessReinstatedIntegrationEvent` |
| 54 | ContentPlaces | `content-places.business.resubmitted.v1` | `BusinessResubmittedIntegrationEvent` |
| 57 | ContentPlaces | `content-places.service-item.created.v1` | `ServiceItemCreatedIntegrationEvent` |
| 58 | ContentPlaces | `content-places.service-item.deleted.v1` | `ServiceItemDeletedIntegrationEvent` |
| 61 | ContentPlaces | `content-places.business-staff.added.v1` | `BusinessStaffAddedIntegrationEvent` |
| 62 | ContentPlaces | `content-places.business-staff.removed.v1` | `BusinessStaffRemovedIntegrationEvent` |
| 65 | ContentTours | `content-tours.place.tour-count-updated.v1` | `PlaceTourCountUpdatedIntegrationEvent` |
| 66 | ContentTours | `content-tours.schedule.changed.v1` | `TourScheduleChangedIntegrationEvent` |
| 67 | ContentTours | `content-tours.pricing-tier.changed.v1` | `TourPricingTierChangedIntegrationEvent` |
| 68 | ContentTours | `content-tours.tour.featured-changed.v1` | `TourFeaturedChangedIntegrationEvent` |
| 69 | ContentTours | `content-tours.tour.deleted.v1` | `TourDeletedIntegrationEvent` |
| 70 | ContentTours | `content-tours.tour.created.v1` | `TourCreatedIntegrationEvent` |
| 71 | ContentTours | `content-tours.tour.updated.v1` | `TourUpdatedIntegrationEvent` |
| 72 | ContentTours | `content-tours.tour.submitted.v1` | `TourSubmittedIntegrationEvent` |
| 73 | ContentTours | `content-tours.tour.approved.v1` | `TourApprovedIntegrationEvent` |
| 74 | ContentTours | `content-tours.tour.rejected.v1` | `TourRejectedIntegrationEvent` |
| 75 | ContentTours | `content-tours.tour.suspended.v1` | `TourSuspendedIntegrationEvent` |
| 76 | ContentTours | `content-tours.tour.reinstated.v1` | `TourReinstatedIntegrationEvent` |
| 79 | ContentTours | `content-tours.tour-guide.assigned.v1` | `TourGuideAssignedIntegrationEvent` |
| 80 | ContentTours | `content-tours.tour-guide.unassigned.v1` | `TourGuideUnassignedIntegrationEvent` |
| 82 | ContentTours | `content-tours.package.created.v1` | `TourPackageCreatedIntegrationEvent` |
| 83 | ContentTours | `content-tours.package.updated.v1` | `TourPackageUpdatedIntegrationEvent` |
| 84 | ContentTours | `content-tours.package.deleted.v1` | `TourPackageDeletedIntegrationEvent` |
| 87 | ContentBlogs | `content-blogs.blog.created.v1` | `BlogCreatedIntegrationEvent` |
| 88 | ContentBlogs | `content-blogs.blog.updated.v1` | `BlogUpdatedIntegrationEvent` |
| 89 | ContentBlogs | `content-blogs.blog.deleted.v1` | `BlogDeletedIntegrationEvent` |
| 90 | ContentBlogs | `content-blogs.blog.restored.v1` | `BlogRestoredIntegrationEvent` |
| 91 | ContentBlogs | `content-blogs.blog.published.v1` | `BlogPublishedIntegrationEvent` |
| 92 | ContentBlogs | `content-blogs.blog.unpublished.v1` | `BlogUnpublishedIntegrationEvent` |
| 93 | ContentBlogs | `content-blogs.blog.archived.v1` | `BlogArchivedIntegrationEvent` |
| 94 | ContentBlogs | `content-blogs.blog-tour.linked.v1` | `BlogTourLinkedIntegrationEvent` |
| 95 | ContentBlogs | `content-blogs.blog-tour.unlinked.v1` | `BlogTourUnlinkedIntegrationEvent` |
| 96 | ContentBlogs | `content-blogs.blog.featured.v1` | `BlogFeaturedIntegrationEvent` |
| 97 | ContentBlogs | `content-blogs.blog.unfeatured.v1` | `BlogUnfeaturedIntegrationEvent` |
| — | **ContentSeo** | **(none)** | **— ⚠️ NOT REGISTERED** |

Total registered: **57**. ContentSeo is the only module with zero registry entries.

---

### 1.2 ContentBlogs — Events & Handlers

#### Domain Events (15)

| Event Class | File | Publisher (raises it) | Consumer (handles it) | Status |
|-------------|------|------------------------|------------------------|--------|
| `BlogCreatedDomainEvent` | `ContentBlogs.Domain/Events/BlogCreatedDomainEvent.cs` | `CreateBlogCommandHandler` → `Blog.Create()` | `BlogCreatedDomainEventHandler` | ✅ Complete |
| `BlogUpdatedDomainEvent` | `ContentBlogs.Domain/Events/BlogUpdatedDomainEvent.cs` | `UpdateBlogCommandHandler` → `Blog.Update()`; `PlaceDeletedIntegrationEventHandler` → `Blog.UnlinkFromPlace()` | `BlogUpdatedDomainEventHandler` | ✅ Complete |
| `BlogDeletedDomainEvent` | `ContentBlogs.Domain/Events/BlogDeletedDomainEvent.cs` | `DeleteBlogCommandHandler` → `Blog.Delete()` | `BlogDeletedDomainEventHandler` | ✅ Complete |
| `BlogRestoredDomainEvent` | `ContentBlogs.Domain/Events/BlogRestoredDomainEvent.cs` | `RestoreBlogCommandHandler` → `Blog.Restore()` | `BlogRestoredDomainEventHandler` | ✅ Complete |
| `BlogPublishedDomainEvent` | `ContentBlogs.Domain/Events/BlogPublishedDomainEvent.cs` | `PublishBlogCommandHandler` → `Blog.Publish()` | `BlogPublishedDomainEventHandler` | ✅ Complete |
| `BlogUnpublishedDomainEvent` | `ContentBlogs.Domain/Events/BlogUnpublishedDomainEvent.cs` | `UnpublishBlogCommandHandler` → `Blog.Unpublish()` | `BlogUnpublishedDomainEventHandler` | ✅ Complete |
| `BlogArchivedDomainEvent` | `ContentBlogs.Domain/Events/BlogArchivedDomainEvent.cs` | `ArchiveBlogCommandHandler` → `Blog.Archive()` | `BlogArchivedDomainEventHandler` | ✅ Complete |
| `BlogFeaturedDomainEvent` | `ContentBlogs.Domain/Events/BlogFeaturedDomainEvent.cs` | `MarkBlogAsFeaturedCommandHandler` → `Blog.MarkAsFeatured()` | `BlogFeaturedDomainEventHandler` | ✅ Complete |
| `BlogUnfeaturedDomainEvent` | `ContentBlogs.Domain/Events/BlogUnfeaturedDomainEvent.cs` | `MarkBlogAsUnfeaturedCommandHandler` → `Blog.MarkAsUnfeatured()` | `BlogUnfeaturedDomainEventHandler` | ✅ Complete |
| `BlogTourLinkedDomainEvent` | `ContentBlogs.Domain/Events/BlogTourLinkedDomainEvent.cs` | `LinkBlogToursCommandHandler` → `Blog.LinkTours()` | `BlogTourLinkedDomainEventHandler` | ✅ Complete |
| `BlogTourUnlinkedDomainEvent` | `ContentBlogs.Domain/Events/BlogTourUnlinkedDomainEvent.cs` | `UnlinkBlogFromTourCommandHandler` → `Blog.RegisterTourUnlinked()` | `BlogTourUnlinkedDomainEventHandler` | ✅ Complete |
| `BlogCommentCreatedDomainEvent` | `ContentBlogs.Domain/Events/BlogCommentCreatedDomainEvent.cs` | `CreateBlogCommentCommandHandler` → `BlogComment.Create()` | **none** | ⚠️ Missing handler |
| `BlogCommentUpdatedDomainEvent` | `ContentBlogs.Domain/Events/BlogCommentUpdatedDomainEvent.cs` | `UpdateBlogCommentCommandHandler` → `BlogComment.Edit()` | **none** | ⚠️ Missing handler |
| `BlogCommentDeletedDomainEvent` | `ContentBlogs.Domain/Events/BlogCommentDeletedDomainEvent.cs` | `DeleteBlogCommentCommandHandler` → `BlogComment.Redact()` | **none** | ⚠️ Missing handler |
| `BlogCommentReactionChangedDomainEvent` | `ContentBlogs.Domain/Events/BlogCommentReactionChangedDomainEvent.cs` | `AddOrReplaceBlogCommentReactionCommandHandler` → `BlogComment.AddOrReplaceReaction()`; `RemoveBlogCommentReactionCommandHandler` → `BlogComment.RemoveReaction()` | **none** | ⚠️ Missing handler |

#### Integration Events (11) — published by ContentBlogs

| Event Class | Logical Name | Publisher (domain-event handler) | External Consumer | Status |
|-------------|--------------|-----------------------------------|---------------------|--------|
| `BlogCreatedIntegrationEvent` | `content-blogs.blog.created.v1` | `BlogCreatedDomainEventHandler` | **none** | ⚠️ No subscribers |
| `BlogUpdatedIntegrationEvent` | `content-blogs.blog.updated.v1` | `BlogUpdatedDomainEventHandler` | **none** | ⚠️ No subscribers |
| `BlogDeletedIntegrationEvent` | `content-blogs.blog.deleted.v1` | `BlogDeletedDomainEventHandler` | **none** | ⚠️ No subscribers |
| `BlogRestoredIntegrationEvent` | `content-blogs.blog.restored.v1` | `BlogRestoredDomainEventHandler` | **none** | ⚠️ No subscribers |
| `BlogPublishedIntegrationEvent` | `content-blogs.blog.published.v1` | `BlogPublishedDomainEventHandler` | **none** | 🔴 No subscribers (ContentSeo should subscribe to update sitemap) |
| `BlogUnpublishedIntegrationEvent` | `content-blogs.blog.unpublished.v1` | `BlogUnpublishedDomainEventHandler` | **none** | 🔴 No subscribers (ContentSeo should subscribe) |
| `BlogArchivedIntegrationEvent` | `content-blogs.blog.archived.v1` | `BlogArchivedDomainEventHandler` | **none** | 🔴 No subscribers (ContentSeo should subscribe) |
| `BlogFeaturedIntegrationEvent` | `content-blogs.blog.featured.v1` | `BlogFeaturedDomainEventHandler` | **none** | ⚠️ No subscribers |
| `BlogUnfeaturedIntegrationEvent` | `content-blogs.blog.unfeatured.v1` | `BlogUnfeaturedDomainEventHandler` | **none** | ⚠️ No subscribers |
| `BlogTourLinkedIntegrationEvent` | `content-blogs.blog-tour.linked.v1` | `BlogTourLinkedDomainEventHandler` | **none** | ⚠️ No subscribers |
| `BlogTourUnlinkedIntegrationEvent` | `content-blogs.blog-tour.unlinked.v1` | `BlogTourUnlinkedDomainEventHandler` | **none** | ⚠️ No subscribers |

#### Inbox Consumers (external events ContentBlogs subscribes to)

| Source Module | Event | Handler | What It Does | PDF Rule |
|----------------|-------|---------|---------------|----------|
| ContentCore | `LanguageActivatedIntegrationEvent` | `ContentBlogs.Infrastructure/EventHandlers/LanguageActivatedIntegrationEventHandler.cs` | Backfills blog translations for the new language | Supports §9 multilingual blogs |
| ContentPlaces | `PlaceDeletedIntegrationEvent` | `ContentBlogs.Infrastructure/EventHandlers/PlaceDeletedIntegrationEventHandler.cs` | Nulls `Blog.PlaceId` on linked blogs; invalidates caches | ✅ §9.3 “PlaceId set to null” |
| **ContentTours** | **`TourDeletedIntegrationEvent`** | **— MISSING —** | **— should remove BlogTours rows for deleted tour —** | 🔴 §9.3 NOT IMPLEMENTED |

#### Command Handlers (17)

| Handler | Domain Event Raised |
|---------|---------------------|
| `CreateBlogCommandHandler` | `BlogCreatedDomainEvent` |
| `UpdateBlogCommandHandler` | `BlogUpdatedDomainEvent` (conditional) |
| `DeleteBlogCommandHandler` | `BlogDeletedDomainEvent` |
| `RestoreBlogCommandHandler` | `BlogRestoredDomainEvent` |
| `PublishBlogCommandHandler` | `BlogPublishedDomainEvent` |
| `UnpublishBlogCommandHandler` | `BlogUnpublishedDomainEvent` |
| `ArchiveBlogCommandHandler` | `BlogArchivedDomainEvent` |
| `MarkBlogAsFeaturedCommandHandler` | `BlogFeaturedDomainEvent` |
| `MarkBlogAsUnfeaturedCommandHandler` | `BlogUnfeaturedDomainEvent` |
| `LinkBlogToursCommandHandler` | `BlogTourLinkedDomainEvent` (one per link) |
| `UnlinkBlogFromTourCommandHandler` | `BlogTourUnlinkedDomainEvent` |
| `TrackBlogViewCommandHandler` | none (by design) |
| `CreateBlogCommentCommandHandler` | `BlogCommentCreatedDomainEvent` |
| `UpdateBlogCommentCommandHandler` | `BlogCommentUpdatedDomainEvent` (conditional) |
| `DeleteBlogCommentCommandHandler` | `BlogCommentDeletedDomainEvent` |
| `AddOrReplaceBlogCommentReactionCommandHandler` | `BlogCommentReactionChangedDomainEvent` |
| `RemoveBlogCommentReactionCommandHandler` | `BlogCommentReactionChangedDomainEvent` |

#### Infrastructure Event Handlers (13)

All 11 blog domain events → matching outbox publisher in `ContentBlogs.Infrastructure/EventHandlers/`:
- `BlogCreatedDomainEventHandler.cs` → outbox `BlogCreatedIntegrationEvent`
- `BlogUpdatedDomainEventHandler.cs` → outbox `BlogUpdatedIntegrationEvent`
- `BlogDeletedDomainEventHandler.cs` → outbox `BlogDeletedIntegrationEvent`
- `BlogRestoredDomainEventHandler.cs` → outbox `BlogRestoredIntegrationEvent`
- `BlogPublishedDomainEventHandler.cs` → outbox `BlogPublishedIntegrationEvent`
- `BlogUnpublishedDomainEventHandler.cs` → outbox `BlogUnpublishedIntegrationEvent`
- `BlogArchivedDomainEventHandler.cs` → outbox `BlogArchivedIntegrationEvent`
- `BlogFeaturedDomainEventHandler.cs` → outbox `BlogFeaturedIntegrationEvent`
- `BlogUnfeaturedDomainEventHandler.cs` → outbox `BlogUnfeaturedIntegrationEvent`
- `BlogTourLinkedDomainEventHandler.cs` → outbox `BlogTourLinkedIntegrationEvent`
- `BlogTourUnlinkedDomainEventHandler.cs` → outbox `BlogTourUnlinkedIntegrationEvent`

Plus 2 inbox consumers:
- `PlaceDeletedIntegrationEventHandler.cs`
- `LanguageActivatedIntegrationEventHandler.cs`

`ContentBlogs.Application/EventHandlers/` — **empty** (only `.gitkeep`).

---

### 1.3 ContentSeo — Events & Handlers

#### Domain Events (10)

| Event Class | File | Publisher (raises it) | Consumer (handles it) | Status |
|-------------|------|------------------------|------------------------|--------|
| `FaqItemCreatedDomainEvent` | `ContentSeo.Domain/Events/FaqItemCreatedDomainEvent.cs` | `FaqItem.Create(...)` | `FaqItemCreatedDomainEventHandler` | ✅ Complete |
| `FaqItemUpdatedDomainEvent` | `ContentSeo.Domain/Events/FaqItemUpdatedDomainEvent.cs` | `FaqItem.Update(...)` | `FaqItemUpdatedDomainEventHandler` | ✅ Complete |
| `FaqItemReorderedDomainEvent` | `ContentSeo.Domain/Events/FaqItemReorderedDomainEvent.cs` | `FaqItem.Reorder(...)` | `FaqItemReorderedDomainEventHandler` | ✅ Complete |
| `FaqItemDeletedDomainEvent` | `ContentSeo.Domain/Events/FaqItemDeletedDomainEvent.cs` | `FaqItem.SoftDelete()` | `FaqItemDeletedDomainEventHandler` | ✅ Complete |
| `RedirectCreatedDomainEvent` | `ContentSeo.Domain/Events/RedirectCreatedDomainEvent.cs` | `Redirect.Create(...)` | `RedirectCreatedDomainEventHandler` | ✅ Complete |
| `RedirectDeactivatedDomainEvent` | `ContentSeo.Domain/Events/RedirectDeactivatedDomainEvent.cs` | `Redirect.Deactivate()` | `RedirectDeactivatedDomainEventHandler` (log-only) | ✅ Complete |
| `RedirectChainFlattenedDomainEvent` | `ContentSeo.Domain/Events/RedirectChainFlattenedDomainEvent.cs` | `Redirect.RewriteTo(...)` | `RedirectChainFlattenedDomainEventHandler` | ✅ Complete |
| `SeoMetadataCreatedDomainEvent` | `ContentSeo.Domain/Events/SeoMetadataCreatedDomainEvent.cs` | `SeoMetadata.Create(...)` | `SeoMetadataCreatedDomainEventHandler` | ✅ Complete |
| `SeoMetadataUpdatedDomainEvent` | `ContentSeo.Domain/Events/SeoMetadataUpdatedDomainEvent.cs` | `SeoMetadata.UpdateMeta/UpdateOg/UpdateSchema/UpdateSitemapHints(...)` (4 raisers per command call) | `SeoMetadataUpdatedDomainEventHandler` | ✅ Complete |
| `SeoMetadataDeletedDomainEvent` | `ContentSeo.Domain/Events/SeoMetadataDeletedDomainEvent.cs` | **none found** | `SeoMetadataDeletedDomainEventHandler` | ⚠️ Orphan event |

#### Integration Events (4) — published by ContentSeo

| Event Class | Logical Name | Publisher | External Consumer | Status |
|-------------|--------------|-----------|---------------------|--------|
| `FaqItemChangedIntegrationEvent` | **NOT REGISTERED** | `FaqItem*DomainEventHandler` (×4: Created/Updated/Reordered/Deleted) | **none** | 🔴 **Registry-broken** |
| `RedirectCreatedIntegrationEvent` | **NOT REGISTERED** | `RedirectCreatedDomainEventHandler` | **none** | 🔴 **Registry-broken** |
| `RedirectChainFlattenedIntegrationEvent` | **NOT REGISTERED** | `RedirectChainFlattenedDomainEventHandler` | **none** | 🔴 **Registry-broken** |
| `SeoMetadataChangedIntegrationEvent` | **NOT REGISTERED** | `SeoMetadata*DomainEventHandler` (×3: Created/Updated/Deleted) | **none** | 🔴 **Registry-broken** |

> **Runtime impact:** `OutboxMessage.Create(...)` internally calls `IntegrationEventTypeRegistry.GetName(typeof(T))`. Because these 4 ContentSeo events have no registry entry, every domain-event-handler that attempts to publish them will throw `InvalidOperationException` at runtime.

#### Inbox Consumers (external events ContentSeo subscribes to)

| Source Module | Event | Handler | What It Does | PDF Rule |
|----------------|-------|---------|---------------|----------|
| ContentCore | `LanguageActivatedIntegrationEvent` | `ContentSeo.Infrastructure/EventHandlers/LanguageActivatedIntegrationEventHandler.cs` | Backfills FAQ translations for the new language | Supports §8 multilingual FAQ |
| ContentPlaces | `PlaceCreatedIntegrationEvent` | `ContentSeo.Infrastructure/EventHandlers/PlaceCreatedIntegrationEventHandler.cs` | Creates baseline `SeoMetadata` + active `SitemapEntry` | ✅ §8.2 sitemap |
| ContentPlaces | `PlaceUpdatedIntegrationEvent` | `ContentSeo.Infrastructure/EventHandlers/PlaceUpdatedIntegrationEventHandler.cs` | Touches sitemap entry; self-heals missing metadata | ✅ §8.2 freshness |
| ContentPlaces | `PlaceDeletedIntegrationEvent` | `ContentSeo.Infrastructure/EventHandlers/PlaceDeletedIntegrationEventHandler.cs` | Deactivates sitemap entry | ✅ §8.2 “only active” |
| ContentPlaces | `BusinessCreatedIntegrationEvent` | `ContentSeo.Infrastructure/EventHandlers/BusinessCreatedIntegrationEventHandler.cs` | Creates baseline `SeoMetadata` + inactive `SitemapEntry` | ✅ §8.2 |
| **ContentBlogs** | **`BlogPublishedIntegrationEvent`** | **— MISSING —** | **— should create/activate blog sitemap entry —** | 🔴 §8.2 NOT IMPLEMENTED |
| **ContentBlogs** | **`BlogUnpublishedIntegrationEvent`** | **— MISSING —** | **— should deactivate blog sitemap entry —** | 🔴 §8.2 NOT IMPLEMENTED |
| **ContentBlogs** | **`BlogArchivedIntegrationEvent`** | **— MISSING —** | **— should remove blog from sitemap listing —** | 🔴 §8.2 NOT IMPLEMENTED |
| **ContentBlogs** | **`BlogUpdatedIntegrationEvent`** | **— MISSING —** | **— should touch sitemap LastModified; auto-create 301 redirect if slug changed —** | 🔴 §8.2 slug-change redirect rule |
| **ContentBlogs** | **`BlogDeletedIntegrationEvent`** | **— MISSING —** | **— should deactivate sitemap entry —** | 🔴 §8.2 |
| **ContentTours** | **`TourCreatedIntegrationEvent` / `TourApprovedIntegrationEvent`** | **— MISSING —** | **— should create/activate tour sitemap entry —** | 🔴 §8.2 (canonical priorities Tours=0.8) |
| **ContentTours** | **`TourDeletedIntegrationEvent` / `TourSuspendedIntegrationEvent`** | **— MISSING —** | **— should deactivate tour sitemap entry —** | 🔴 §8.2 |

#### Command Handlers (10)

| Handler | Domain Event Raised |
|---------|---------------------|
| `CreateFaqItemCommandHandler` | `FaqItemCreatedDomainEvent` |
| `UpdateFaqItemCommandHandler` | `FaqItemUpdatedDomainEvent` |
| `ReorderFaqItemsCommandHandler` | `FaqItemReorderedDomainEvent` × N (per reordered item) |
| `DeleteFaqItemCommandHandler` | `FaqItemDeletedDomainEvent` |
| `CreateRedirectCommandHandler` | `RedirectCreatedDomainEvent` + `RedirectChainFlattenedDomainEvent` × N (per rewritten chain link) |
| `DeleteRedirectCommandHandler` | `RedirectDeactivatedDomainEvent` |
| `UpsertSeoMetadataCommandHandler` | `SeoMetadataCreatedDomainEvent` on insert + `SeoMetadataUpdatedDomainEvent` × 3; update branch raises `SeoMetadataUpdatedDomainEvent` × 4 |
| `UpdateSeoMetadataCommandHandler` | `SeoMetadataUpdatedDomainEvent` × 4 per call (one per partial-update method) |
| `RegenerateSitemapCommandHandler` | none (by design) |
| `RefreshWeatherCommandHandler` | none (by design) |

`ContentSeo.Application/EventHandlers/` — **empty** (only `.gitkeep`).

#### Background Services (2)

| Service | File | Cadence | Consumes | Publishes |
|---------|------|---------|----------|-----------|
| `SitemapRegenerationService` | `ContentSeo.Infrastructure/BackgroundServices/SitemapRegenerationService.cs` | Every 6h (`TimeSpan.FromHours(6)`) | nothing | nothing |
| `WeatherPreFetchService` | `ContentSeo.Infrastructure/BackgroundServices/WeatherPreFetchService.cs` | Daily @ `WeatherOptions.PreFetchHourUtc` (default 05:00 UTC); budget = `WeatherOptions.DailyBudget` (default 1000) | nothing | nothing |

> ⚠️ Cadence drift: PDF §8 mandates the sitemap regenerate at the cron times `00:00 / 06:00 / 12:00 / 18:00 UTC`. The current `TimeSpan.FromHours(6)` is **interval-based** from process start, not cron-aligned. After a restart at 14:00 UTC, the next regen happens at 20:00 UTC, not 18:00.

---

### 1.4 Cross-Module Event Flow

```
┌──────────────────────┐                ┌──────────────────────┐
│   ContentCore        │                │     Auth/Security    │
│   • Language         │                │     (out of scope)   │
│   • Attachment       │                └──────────────────────┘
│   • Category         │
└──────┬───────────────┘
       │ LanguageActivated
       ├──────────────────────────┐
       │                          │
       ▼                          ▼
┌──────────────────────┐  ┌──────────────────────┐
│    ContentBlogs      │  │     ContentSeo       │
│                      │  │                      │
│ • Blog CRUD          │  │ • SeoMetadata        │
│ • BlogComment        │  │ • Redirect           │
│ • BlogTour           │  │ • FaqItem            │
│ • Reaction           │  │ • SitemapEntry       │
│                      │  │ • WeatherCache       │
└────┬─────────────────┘  └──────────────────────┘
     │                              ▲   ▲   ▲   ▲
     │ 11 Blog* events              │   │   │   │
     │ ❌ NO SUBSCRIBERS ───────────┘   │   │   │
     │                                  │   │   │
     ▼                                  │   │   │
[outbox table]                          │   │   │
                                        │   │   │
┌──────────────────────┐                │   │   │
│   ContentPlaces      │                │   │   │
│ • Place CRUD         │ ───────────────┘   │   │
│ • Business CRUD      │ Place* + Business* │   │
│ • Staff              │     (✅ wired)      │   │
└──────────────────────┘                    │   │
       │                                    │   │
       │ PlaceDeleted ──────────────────────┘   │
       │                                        │
       │                                        │
       ▼                                        │
┌──────────────────────┐                        │
│   ContentTours       │                        │
│ • Tour CRUD          │                        │
│ • Tour Guide         │  Tour* events          │
│ • Package            │  ❌ NO SUBSCRIBERS ────┘
│ • Schedule           │  (PDF §9.3 cleanup rule + §8.2 sitemap)
└──────────────────────┘
```

**Key observations:**

- **ContentBlogs** outputs 11 integration events. **Zero** external modules consume them.
- **ContentSeo** outputs 4 integration events. None are registered. Even if they were, **zero** external modules consume them.
- **ContentSeo** correctly subscribes to ContentPlaces and ContentCore events but **does not subscribe to any ContentBlogs or ContentTours event**, so the sitemap never auto-updates when blogs/tours change state (PDF §8.2 violation).
- **ContentBlogs** correctly subscribes to `PlaceDeleted` but **does not subscribe to `TourDeleted`**, so PDF §9.3's “tour deleted → BlogTours row removed” rule is silently violated.

---

## 2. Compliance Gaps (Code-Level)

### 2.1 🔴 BLOCKERS

| # | Title | Source-of-Truth Rule | Current Code | Required Fix |
|---|-------|----------------------|--------------|---------------|
| **B1** | Blog publish — AR+EN translation gate missing | PDF §9: AR + EN required before publish | `PublishBlogCommandHandler` checks source EN only; task line L567 explicitly allows publish without AR | Add AR translation existence check in `Blog.Publish()` invariants or in `PublishBlogCommandHandler` validation gate |
| **B2** | BlogCommentReaction — same-type should toggle/remove | PDF §9 + `YallaJo.md` L1376: "re-sending same reaction type removes it" | `AddOrReplaceBlogCommentReactionCommandHandler` treats same-type as idempotent no-op | When `(UserId, CommentId, ReactionType)` row exists, **delete** it and raise `BlogCommentReactionChangedDomainEvent` with `Removed` flag |
| **B3** | SitemapRenderer — >50K URL sharding | PDF §8: split into sitemap-index when URL count >50,000 | `SitemapRenderer` returns `Sitemap.SizeOverflow` 503 error; sharding deferred | Generate sitemap-index XML when count >50K; emit one sub-sitemap per `EntityType` (or numbered chunks) |
| **B4** | WeatherCache — keyed by `PlaceId`, not coordinates | PDF §11: cache key = `(lat-rounded-2dp, lng-rounded-2dp, date)` so nearby tours share cache | `WeatherCache` entity + `WeatherCacheRepository` lookups use `PlaceId`; endpoint is `/weather/{placeId}` | Refactor `WeatherCache` to composite key `(RoundedLat, RoundedLng, Date)`; migrate existing rows; expose endpoint as `/weather?lat=&lng=` or rebuild lookup logic |
| **B5** | Weather forecast — horizon not 7 days | PDF §11: 7-day forecast required (older `YallaJo.md` says 5 — PDF wins) | `WeatherSnapshot.ForecastJson` schema unbounded; `IWeatherProvider.GetForecastAsync` contract not pinned to 7 | Lock `IWeatherProvider.GetForecastAsync` return shape to 7 daily forecasts; add unit test |
| **B6** | Weather 1000/day budget — in-memory only | PDF §11: 1000 calls/day budget, persistent | `WeatherPreFetchService` uses local `budgetLeft = 1000`, resets each BG run; multi-instance doubles | Persist daily call counter (e.g. `WeatherDailyBudget(Date, CallsUsed)` row); check before every provider call across all BG instances |
| **B7** | `SeoRedirectMiddleware` — missing | PDF §8 + `YallaJo.md` L155-L168: middleware must check redirects on every 404 before returning error page | CRUD endpoints exist; middleware not registered in pipeline | Add `SeoRedirectMiddleware` registered before endpoint mapping in `YallaJo.Web/Program.cs`: intercept 404, lookup `Redirects` by `OldUrl`, return 301/302 to `NewUrl`, increment `HitCount` |
| **B8** | Blog publish — OG image gate too strict | PDF §8: OG image uses primary image, falls back to YallaJo default | `PublishBlogCommandHandler` rejects publish without ≥1 image | Remove ≥1-image guard; configure default OG image fallback in OG-tag generator |
| **B9** | **ContentSeo integration events NOT REGISTERED** | `agent-context.md` §0.3 + §5: every integration event must be in `IntegrationEventTypeRegistry`; outbox `OutboxMessage.Create` calls `GetName(typeof(T))` and throws on missing | All 4 ContentSeo events (`FaqItemChanged`, `RedirectCreated`, `RedirectChainFlattened`, `SeoMetadataChanged`) have no entries | Register all 4 in `IntegrationEventTypeRegistry.cs` with logical names `content-seo.faq.changed.v1`, `content-seo.redirect.created.v1`, `content-seo.redirect.chain-flattened.v1`, `content-seo.metadata.changed.v1` |
| **B10** | **ContentBlogs missing `TourDeletedIntegrationEventHandler`** | PDF §9.3: tour deletion removes BlogTours rows | No handler in `ContentBlogs.Infrastructure/EventHandlers/` | Add `TourDeletedIntegrationEventHandler.cs` that removes `BlogTours` rows where `TourId == event.TourId`; raise `BlogTourUnlinkedDomainEvent` per row removed |
| **B11** | **BlogComment domain events have no consumers** | `agent-context.md` outbox pattern: state-changing events should propagate or be intentionally consumed; PDF §9 implies comment activity is sitemap-relevant for blog freshness | 4 domain events (`BlogCommentCreated/Updated/Deleted/ReactionChanged`) have **no** infrastructure handler at all | Decide intent: either (a) add no-op consumers explicitly marking them internal-only, or (b) add outbox publishers that emit `content-blogs.comment.*.v1` integration events + register in `IntegrationEventTypeRegistry` |
| **B12** | **ContentSeo does not subscribe to ContentBlogs / ContentTours state changes** | PDF §8.2: sitemap reflects active+published entities; LastModified = entity.UpdatedAt | `ContentSeo.Infrastructure/EventHandlers/` has no handler for `BlogPublished`, `BlogUnpublished`, `BlogArchived`, `BlogUpdated`, `BlogDeleted`, `TourCreated`, `TourApproved`, `TourDeleted`, `TourSuspended` | Add inbox consumers in `ContentSeo.Infrastructure/EventHandlers/` to create/update/deactivate `SitemapEntry` and auto-create 301 redirects on slug change (for `BlogUpdated`). Pattern: mirror existing `PlaceCreatedIntegrationEventHandler` |

### 2.2 🟡 WARNINGS

| # | Title | Rule | Current Code | Fix |
|---|-------|------|--------------|-----|
| **W1** | Hreflang — only `ar` alternate emitted | PDF §8: hreflang `ar` AND `en` | `SitemapRenderer` emits only `ar` `xhtml:link` | Emit both `ar` and `en` `xhtml:link` entries per entity |
| **W2** | Stale weather DTO missing timestamp | PDF §11: stale cache shown with "Last updated X hours ago" | Returns 200 with `X-Weather-Stale` header but no DTO field | Add `FetchedAt` / `StaleSince` field to weather response DTO |
| **W3** | Weather budget exhaustion — no admin alert | PDF §11: rate-limit → admin alert | `WeatherPreFetchService` logs warning only | Emit `Weather.BudgetExhausted` integration event or push notification |
| **W4** | `SitemapEntry.Create()` not bound to `entity.UpdatedAt` | PDF §8: LastModified = entity.UpdatedAt | Factory lacks `lastModified` parameter | Add `lastModified` parameter to `SitemapEntry.Create()`; persist; default-update on each inbox event |
| **W5** | `SeoMetadataDeletedDomainEvent` is orphan | n/a | Handler exists in `ContentSeo.Infrastructure/EventHandlers/SeoMetadataDeletedDomainEventHandler.cs` but no entity method (`SeoMetadata.SoftDelete()` or similar) raises it | Either add a `SeoMetadata.SoftDelete()` method that raises the event, or delete the handler if soft-delete is unused |
| **W6** | Sitemap cadence not cron-aligned | PDF §8: regen at 00:00/06:00/12:00/18:00 UTC | `SitemapRegenerationService` uses `TimeSpan.FromHours(6)` from process start | Implement cron-style scheduler (e.g. `NCrontab` or compute next slot from UTC clock) so cadence is wall-clock-aligned across restarts |

### 2.3 ⚠️ Code / Naming Drift

| # | Title | Detail |
|---|-------|---------|
| **D1** | `ContentBlogFeatures` (singular) vs `ContentBlogsFeatures` (plural) | Endpoint files (`BlogEndpoints.cs` L22, L124) reference `ContentBlogFeatures` (singular). Task plan + ContentSeo precedent (`ContentSeoFeatures`) use plural. Decide convention and rename catalog/feature class + DI registrations |
| **D2** | Generic `catch (Exception)` in handler snippet | Task L580-L604 shows `BlogCreatedDomainEventHandler` catching generic `Exception` — verify actual code at `ContentBlogs.Infrastructure/EventHandlers/BlogCreatedDomainEventHandler.cs`. If present, narrow per `agent-context.md` §5.5: allowed only for Infrastructure external calls / `DbUpdateConcurrencyException` / BackgroundService-per-item |
| **D3** | `Application/EventHandlers/` folders are empty in both modules | All domain-event handlers live in `Infrastructure/EventHandlers/`. Confirm this is the intended layering (Application = pure CQRS; Infrastructure = outbox/inbox wiring). If so, delete the empty `Application/EventHandlers/` folders to remove the false-positive scaffold |

---

## 3. Implementation Status Snapshot

### 3.1 What's already built (no greenfield work needed)

| Area | Status | Files |
|------|--------|-------|
| ContentSeo — FAQ CRUD + reorder | ✅ Built | `Commands/FaqItem/`, `Queries/FaqItem/`, `FaqItemEndpoints.cs` |
| ContentSeo — Redirect CRUD + chain flatten | ✅ Built | `Commands/Redirect/`, `Queries/Redirect/`, `RedirectEndpoints.cs` |
| ContentSeo — SeoMetadata upsert + update | ✅ Built | `Commands/SeoMetadata/`, `Queries/SeoMetadata/`, `SeoMetadataEndpoints.cs` |
| ContentSeo — Sitemap render + regenerate | ✅ Built (compliance gaps) | `Commands/Sitemap/RegenerateSitemap/`, `SitemapEndpoints.cs`, `SitemapRenderer.cs`, `NoOpSearchConsolePinger.cs` |
| ContentSeo — Weather get + refresh | ✅ Built (compliance gaps) | `Commands/Weather/RefreshWeather/`, `Queries/Weather/`, `WeatherEndpoints.cs`, `NoOpWeatherProvider.cs`, `WeatherOptions.cs` |
| ContentSeo — Background services | ✅ Built (compliance gaps) | `BackgroundServices/SitemapRegenerationService.cs`, `BackgroundServices/WeatherPreFetchService.cs` |
| ContentSeo — Migrations | ✅ Built | `20260510100651_CreateModel`, `20260513122151_UpgradeWeatherCacheToAuditableEntity` |
| ContentSeo — Authorization | ✅ Built | `ContentSeoFeatures.cs`, `ContentSeoPermissionCatalog.cs` |
| ContentCore — Translation API | ✅ Built (7 endpoints, more than task's 3) | `Commands/Translation/{ApproveTranslation, BatchApproveTranslations, BatchTranslate, TranslateText, TriggerTranslationBackfill, UpdateTranslation}/`, `Queries/Translation/GetEntityTranslations/`, `TranslationEndpoints.cs` |
| ContentBlogs — ~23 endpoints | ✅ Built (compliance gaps + naming drift) | `BlogEndpoints.cs`, `BlogCommentEndpoints.cs` |
| ContentBlogs — Domain (entities, enums, events) | ✅ Built | `Domain/Entities/{Blog, BlogComment, BlogCommentReaction, BlogTour, BlogTranslation}.cs`, `Domain/Enums/{BlogStatus, ReactionType}.cs`, `Domain/Events/*.cs` (15 events) |
| ContentBlogs — Infrastructure | ✅ Built | `ContentBlogsDbContext`, `ContentBlogsUnitOfWork`, `ContentBlogsInboxStore`, `BlogOwnershipService`, 13 event handlers |
| ContentBlogs — Migrations | ✅ Built | `20260510100608_CreateModel` |

### 3.2 What's NOT done (the work that remains)

All of the work that remains is **compliance remediation**, mapped to the 12 blockers + 6 warnings + 3 drift items in §2.

There is **no greenfield endpoint scaffolding** to build. Pre-work items PW-1 through PW-7 in the task plan are either already done (PW-4 migration is merged) or have moved into the compliance-fix work above.

---

## 4. Recommended Remediation Order

### Phase 1 — Stop runtime failures (~1 day)

1. **B9** — Register the 4 ContentSeo integration events in `IntegrationEventTypeRegistry`. **This is the #1 priority** because every FAQ/Redirect/SeoMetadata domain-event handler will throw at runtime today.
2. **W5** — Resolve `SeoMetadataDeletedDomainEvent` orphan (add raiser or delete handler).
3. Outbox round-trip sanity test (FAQ create → outbox row visible → no exception).

### Phase 2 — Close PDF §9 (Blog) compliance gaps (~1-2 days)

4. **B1** — AR+EN translation gate in `Blog.Publish()`.
5. **B2** — Reaction same-type toggle (delete row + raise event with Removed=true).
6. **B8** — Remove image requirement from publish gate; configure default OG image.
7. **B10** — Add `TourDeletedIntegrationEventHandler` for BlogTours cleanup.
8. **B11** — Decide intent for BlogComment domain events; add consumers or outbox publishers.

### Phase 3 — Close PDF §8 (SEO) compliance gaps (~2 days)

9. **B7** — Add `SeoRedirectMiddleware` in `YallaJo.Web` pipeline.
10. **B3** — Implement sitemap-index sharding for >50K URLs.
11. **B12** — Add inbox consumers in ContentSeo for `Blog*` and `Tour*` events (sitemap auto-update).
12. **W1** — Emit both `ar` and `en` hreflang alternates.
13. **W4** — Add `lastModified` parameter to `SitemapEntry.Create()`.
14. **W6** — Cron-align `SitemapRegenerationService` cadence.

### Phase 4 — Close PDF §11 (Weather) compliance gaps (~1-2 days)

15. **B4** — Refactor `WeatherCache` to `(lat-2dp, lng-2dp, date)` composite key.
16. **B5** — Lock `IWeatherProvider.GetForecastAsync` contract to 7 days.
17. **B6** — Persist daily API budget counter.
18. **W2** — Add `FetchedAt`/`StaleSince` to weather DTO.
19. **W3** — Emit `Weather.BudgetExhausted` integration event for admin alert.

### Phase 5 — Code hygiene (~half day)

20. **D1** — Reconcile `ContentBlogFeatures` ↔ `ContentBlogsFeatures` naming.
21. **D2** — Verify and narrow generic `catch (Exception)` per agent-context.md §5.5.
22. **D3** — Delete empty `Application/EventHandlers/` folders if Infrastructure layering is intentional.

---

## 5. Appendix — File References

### Key files cited

- `YallaJo.SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs` — registration source (must add 4 ContentSeo entries)
- `ContentBlogs.Infrastructure/EventHandlers/` — 11 outbox publishers + 2 inbox consumers; **add** `TourDeletedIntegrationEventHandler.cs` + (decision) BlogComment handlers
- `ContentBlogs.Application/Commands/Blog/PublishBlog/PublishBlogCommandHandler.cs` — add AR translation check, remove image gate
- `ContentBlogs.Application/Commands/BlogComment/AddOrReplaceBlogCommentReaction/AddOrReplaceBlogCommentReactionCommandHandler.cs` — change same-type behavior to delete
- `ContentSeo.Infrastructure/EventHandlers/` — 5 inbox consumers exist; **add** Blog* and Tour* consumers for sitemap auto-update
- `ContentSeo.Infrastructure/BackgroundServices/SitemapRegenerationService.cs` — cron-align cadence; implement sharding
- `ContentSeo.Infrastructure/BackgroundServices/WeatherPreFetchService.cs` — persist daily budget
- `ContentSeo.Domain/Entities/WeatherCache.cs` + `ContentSeo.Infrastructure/Persistence/Repositories/WeatherCacheRepository.cs` — refactor key strategy
- `ContentSeo.Infrastructure/Sitemap/SitemapRenderer.cs` — sharding + hreflang emission
- `YallaJo.Web/Program.cs` — register `SeoRedirectMiddleware`

### Excluded by user direction

- WBS / working-day estimates / hour totals
- Team allocation (Fadwa / Mohammad / Mahmoud / Tech Lead)
- Sprint window / weekend rules
- Document-internal-consistency math (endpoint counts, acceptance-test counts)
- Process / ceremony / review-hour overhead

---

*Generated from audit of `Agents/tasks/ContentBlogs-ContentSeo-team-tasks.md` against the four canonical sources, with full event/handler topology inventoried across 61 registry entries + 25 domain events + 15 integration events + 7 inbox consumers + 27 command handlers + 2 background services.*
