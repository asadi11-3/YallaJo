# ContentCore Deferred GAPs — Implementation Plan

> **Scope**: GAP-09, GAP-10, GAP-14, GAP-15 — deferred from the 2026-04-23 ContentCore audit.
> **Status**: Not started. TODO comments are in place in source (see §1 below).
> **Total estimate**: ~12–16 hours of focused work (1.5–2 focused days).
> **Last updated**: 2026-04-23

---

## Table of Contents

1. [TODO Anchors in Source](#1-todo-anchors-in-source)
2. [GAP-09 — Category Integration Events](#2-gap-09--category-integration-events)
3. [GAP-10 — Attachment Integration Events](#3-gap-10--attachment-integration-events)
4. [GAP-14 — Multilingual Tag](#4-gap-14--multilingual-tag)
5. [GAP-15 — Multilingual Specialization](#5-gap-15--multilingual-specialization)
6. [Execution Order & Dependencies](#6-execution-order--dependencies)
7. [Risk Register](#7-risk-register)
8. [Commit Strategy](#8-commit-strategy)

---

## 1. TODO Anchors in Source

All 6 anchors in place as of v2.4 — grep for `TODO (GAP-` to find them:

| GAP | File | Location |
|-----|------|----------|
| GAP-09 | `ContentCore.Infrastructure/EventHandlers/CategoryCreatedDomainEventHandler.cs` | Top, under namespace |
| GAP-09 | `ContentCore.Infrastructure/EventHandlers/CategoryUpdatedDomainEventHandler.cs` | Top, under namespace |
| GAP-10 | `ContentCore.Infrastructure/EventHandlers/AttachmentUploadedDomainEventHandler.cs` | Top, under namespace |
| GAP-10 | `ContentCore.Infrastructure/EventHandlers/AttachmentDeletedDomainEventHandler.cs` | Top, under namespace |
| GAP-14 | `ContentCore.Domain/Entities/Tag.cs` | Above `class Tag` declaration |
| GAP-15 | `ContentCore.Domain/Entities/Specialization.cs` | Above `class Specialization` declaration |

Each anchor cross-references the section of this document that contains the full plan, so grep-based discovery leads directly to executable instructions.

---

## 2. GAP-09 — Category Integration Events

### 2.1 Problem
`Category.Create()` and `Category.Update()` raise domain events, but ContentCore never publishes matching *integration* events to the outbox. Downstream modules (e.g. ContentSeo) cannot react to category lifecycle changes.

### 2.2 Pre-conditions (must exist before this GAP is useful)
- A consumer module that actually cares about category events.
- Current candidate: **ContentSeo** should add `SeoEntityType.Category` so it can auto-create `SeoMetadata` rows on category creation and invalidate them on rename.

> Without a consumer, implementing this produces orphaned outbox rows. **Verify a consumer exists/is planned before starting.**

### 2.3 Scope
| Concern | Decision |
|---------|----------|
| Which events? | `CategoryCreatedIntegrationEvent`, `CategoryUpdatedIntegrationEvent`, `CategoryDeletedIntegrationEvent`, `CategoryRestoredIntegrationEvent` |
| Payload | `CategoryId`, `Slug`, `ParentCategoryId?`, `SourceLanguageCode`, `OccurredAt` — NO translations (consumer pulls via query) |
| Transport | Outbox (follows `LanguageDeactivatedIntegrationEvent` pattern) |
| Registry count | 20 → 24 (4 new events) |

### 2.4 Files to Create (7 new)
```
ContentCore.Contracts/IntegrationEvents/
    CategoryCreatedIntegrationEvent.cs
    CategoryUpdatedIntegrationEvent.cs
    CategoryDeletedIntegrationEvent.cs
    CategoryRestoredIntegrationEvent.cs

ContentCore.Infrastructure/EventHandlers/
    CategoryDeletedDomainEventHandler.cs      (if not already publishing — check first)
    CategoryRestoredDomainEventHandler.cs     (new — raise RestoreCategory command too)
```

### 2.5 Files to Edit (4)
```
ContentCore.Domain/Events/
    CategoryRestoredDomainEvent.cs            (ensure it exists; Category.Restore() must raise it)

ContentCore.Domain/Entities/Category.cs       (raise CategoryRestoredDomainEvent in Restore() override)

ContentCore.Infrastructure/EventHandlers/
    CategoryCreatedDomainEventHandler.cs      (add IIntegrationEventPublisher call)
    CategoryUpdatedDomainEventHandler.cs      (add IIntegrationEventPublisher call)

YallaJo.SharedKernel.Infrastructure/Abstractions/Integration/
    IntegrationEventTypeRegistry.cs           (20 → 24)

tests/SharedKernel.Tests.Unit/
    IntegrationEventTypeRegistryTests.cs      (update count + method name)
```

### 2.6 Implementation Steps
1. **Verify consumer intent** — ping ContentSeo team / check backlog for `SeoEntityType.Category`. **Abort if no consumer planned.**
2. Override `Category.Restore()` to raise `CategoryRestoredDomainEvent` (AuditableEntity.Restore is non-virtual → add a new virtual method OR raise event in command handler after calling Restore()). **Recommended**: raise event in handler (simpler, no SharedKernel churn).
3. Create 4 integration event records in `ContentCore.Contracts/IntegrationEvents/`.
4. Update the 2 existing domain event handlers to publish integration events AFTER translation work completes.
5. Create `CategoryDeletedDomainEventHandler` + `CategoryRestoredDomainEventHandler` (publish-only).
6. Register all 4 in `IntegrationEventTypeRegistry`.
7. Update test count 20 → 24.
8. Build + test.

### 2.7 Estimate
**~3 hours** (once consumer is confirmed).

### 2.8 Out of scope
- Actually writing the ContentSeo consumer — separate PR in the ContentSeo module.

---

## 3. GAP-10 — Attachment Integration Events

### 3.1 Problem
Attachment upload/delete events are raised internally but never published to the outbox. SEO cannot auto-populate OG image URLs; downstream image CDN workers cannot react.

### 3.2 Pre-conditions
- A consumer exists/is planned. Candidate: **ContentSeo** auto-populates `SeoMetadata.OgImageUrl` for Place/Tour/Business entities when their primary image is uploaded.

### 3.3 Scope
| Concern | Decision |
|---------|----------|
| Which events? | `AttachmentUploadedIntegrationEvent`, `AttachmentDeletedIntegrationEvent` |
| Payload | `AttachmentId`, `EntityType`, `EntityId`, `StoragePath`, `IsPrimary`, `ContentType`, `OccurredAt` |
| Filter? | Only publish for image content types? → **No**, publish all; consumers filter. |
| Transport | Outbox |
| Registry count | After GAP-09: 24 → 26 |

### 3.4 Files to Create (2)
```
ContentCore.Contracts/IntegrationEvents/
    AttachmentUploadedIntegrationEvent.cs
    AttachmentDeletedIntegrationEvent.cs
```

### 3.5 Files to Edit (4)
```
ContentCore.Infrastructure/EventHandlers/
    AttachmentUploadedDomainEventHandler.cs   (add IIntegrationEventPublisher call)
    AttachmentDeletedDomainEventHandler.cs    (add IIntegrationEventPublisher call)

YallaJo.SharedKernel.Infrastructure/Abstractions/Integration/
    IntegrationEventTypeRegistry.cs           (24 → 26)

tests/SharedKernel.Tests.Unit/
    IntegrationEventTypeRegistryTests.cs
```

### 3.6 Implementation Steps
1. Verify ContentSeo consumer intent.
2. Create 2 integration event records.
3. Inject `IIntegrationEventPublisher` into both existing domain event handlers.
4. Publish integration events after existing work completes (same transaction → outbox).
5. Register + update test count.
6. Build + test.

### 3.7 Estimate
**~1.5 hours**.

---

## 4. GAP-14 — Multilingual Tag

### 4.1 Problem
`Tag.Name` is a single string (e.g. "Historical"). Cannot display localized tag names in Arabic, French, etc. `Category` and `Place` both already have `EntityTranslation` support — tags are inconsistent.

### 4.2 Reference Implementation
Mirror `Category` + `CategoryTranslation` pattern exactly:
- `Category` has `Name` (source language)
- `CategoryTranslation` stores translated variants, keyed by `(CategoryId, LanguageCode, FieldName)`
- `CategoryCreatedDomainEventHandler` auto-translates on create
- `CategoryUpdatedDomainEventHandler` re-translates on update

### 4.3 Scope (mirrors Category pattern)
| Concern | Decision |
|---------|----------|
| New entity | `TagTranslation : AuditableEntity` |
| Source field on `Tag` | Add `SourceLanguageCode` (default "en") |
| Translation fields | `Name` only (tags have no description — keep minimal) |
| Auto-translate on Create/Update? | Yes — reuse `IAutoTranslationService` + add domain events |
| Permission changes | None (same `Tag.Create/Update/Delete`) |
| DB migration | Required — new `TagTranslations` table + FK + unique index |

### 4.4 Files to Create (~14)
```
ContentCore.Domain/
    Entities/TagTranslation.cs
    Events/TagCreatedDomainEvent.cs
    Events/TagUpdatedDomainEvent.cs
    Repositories/ITagTranslationRepository.cs

ContentCore.Application/
    Queries/Tag/GetTagTranslations/            (Query + Handler)
    DTOs/TagTranslationDto.cs

ContentCore.Infrastructure/
    Repositories/TagTranslationRepository.cs
    EntityConfigurations/TagTranslationConfiguration.cs
    EventHandlers/TagCreatedDomainEventHandler.cs    (auto-translate)
    EventHandlers/TagUpdatedDomainEventHandler.cs    (re-translate)
    Migrations/YYYYMMDD_AddTagTranslations.cs        (EF migration)

ContentCore.Presentation/
    Endpoints/Tag/Models/TagTranslationResponse.cs
```

### 4.5 Files to Edit (~8)
```
ContentCore.Domain/Entities/Tag.cs              (promote to IAggregateRoot, add SourceLanguageCode, raise events)
ContentCore.Application/Commands/Tag/CreateTag/
    CreateTagCommand.cs                         (add optional Translations dict)
    CreateTagCommandHandler.cs                  (raise TagCreatedDomainEvent)
ContentCore.Application/Commands/Tag/UpdateTag/
    UpdateTagCommand.cs                         (add optional Translations dict)
    UpdateTagCommandHandler.cs                  (raise TagUpdatedDomainEvent)
ContentCore.Application/Queries/Tag/ListTags/
    ListTagsQuery.cs                            (add WithTranslations flag)
    ListTagsQueryHandler.cs                     (join translations)
ContentCore.Infrastructure/ContentCoreDbContext.cs  (DbSet + entity config)
ContentCore.Presentation/Endpoints/Tag/TagEndpoints.cs  (Accept-Language header)
```

### 4.6 Critical Design Decisions
1. **Tag promoted from `AuditableEntity` to `IAggregateRoot`** — required to raise domain events. Breaking change at the entity level, but not at the API level.
2. **Reuse `TranslationCache`** (raw API result cache) but add dedicated `TagTranslations` table for *materialized* entity-specific translations (matches Category pattern).
3. **Migration must be reversible** — backfill `SourceLanguageCode = "en"` for existing rows.

### 4.7 Implementation Steps (ordered)
1. Domain layer: `TagTranslation` entity, `TagCreatedDomainEvent`, `TagUpdatedDomainEvent`, make `Tag` implement `IAggregateRoot`, add `SourceLanguageCode`.
2. Infrastructure: `TagTranslationConfiguration` (unique index on `(TagId, LanguageCode, FieldName)`), `TagTranslationRepository`, DbContext registration.
3. Migration: `dotnet ef migrations add AddTagTranslations --project ContentCore.Infrastructure`.
4. Application: extend Create/Update commands, raise domain events, update `ListTags` to join translations when `WithTranslations=true`.
5. Event handlers: auto-translate via `IAutoTranslationService` (exactly mirror `CategoryCreatedDomainEventHandler`).
6. Endpoints: honor `Accept-Language` header like `ListCategoriesQuery` does.
7. Tests: unit tests for domain events + integration test for end-to-end translation flow.
8. `agent-context.md` update: mark Tag as multilingual.

### 4.8 Estimate
**~5 hours** (entity + repo + migration + 2 handlers + 4 command changes + endpoint + tests).

### 4.9 Backfill strategy
After migration, run a one-time background job (or admin endpoint) that:
- Iterates all existing `Tag` rows.
- For each active `Language` ≠ source language, calls `IAutoTranslationService` once.
- Inserts `TagTranslation` rows.

Document this as a separate ops task — **not part of the migration itself** (migrations should not call external APIs).

---

## 5. GAP-15 — Multilingual Specialization

### 5.1 Problem
Same as GAP-14 but for `Specialization`. `Specialization.Name` is a single string.

### 5.2 Reference Implementation
**Identical pattern to GAP-14 Tag.** Copy the Tag plan, s/Tag/Specialization/g.

### 5.3 Differences from GAP-14
| Concern | Tag (GAP-14) | Specialization (GAP-15) |
|---------|--------------|-------------------------|
| Entity has description field? | No | **Check `Specialization.cs`** — if yes, translate Name + Description |
| Usage frequency | High (every place/tour has multiple tags) | Low (tourist guides only — ~50–200 records) |
| Priority | Medium | Low |

### 5.4 File count
Same as GAP-14 — ~14 new + ~8 edits.

### 5.5 Estimate
**~4 hours** (slightly less than Tag because pattern is already established and volume is lower).

### 5.6 Optimization
If GAP-14 is completed first, consider extracting a shared base:
```csharp
public abstract class TranslatableEntity : AuditableEntity, IAggregateRoot
{
    public string SourceLanguageCode { get; protected set; } = "en";
}
```
**Decision**: skip unless a 3rd multilingual entity appears. Two instances is not enough duplication to justify abstraction.

---

## 6. Execution Order & Dependencies

```
GAP-09 (3h)  ─┐
              ├─→ no dependencies; either can go first
GAP-10 (1.5h)─┘

GAP-14 (5h)  ──→ GAP-15 (4h)    [GAP-15 reuses pattern established in GAP-14]
```

### Recommended sequence
1. **GAP-10** first (smallest, builds muscle memory for the outbox pattern)
2. **GAP-09** (same pattern, more events)
3. **GAP-14** (new DB migration — requires attention)
4. **GAP-15** (copy-paste of 14)

### Blockers to resolve first
- **GAP-09 / GAP-10 blocker**: confirm ContentSeo team will add consumers. Otherwise we publish to outbox with zero handlers (works, but wastes DB writes).
- **GAP-14 / GAP-15 blocker**: none — purely internal.

---

## 7. Risk Register

| # | Risk | Mitigation |
|---|------|------------|
| R1 | GAP-09/10: no consumer exists → orphan outbox rows forever | Confirm ContentSeo subscription before merging. Abort the phase otherwise. |
| R2 | Registry count drift between branches (other modules may bump count) | Rebase before the final commit that updates the registry test. |
| R3 | GAP-14: promoting `Tag` to `IAggregateRoot` breaks callers that treat it as a value | Search `ITagRepository` callers. Currently only used in `CreateTag`/`UpdateTag`/`DeleteTag` handlers + `ListTagsQueryHandler`. Low risk. |
| R4 | GAP-14: EF migration on production DB with existing tags → null `SourceLanguageCode` | Migration must include `defaultValue: "en"` + `NOT NULL`. |
| R5 | GAP-14/15: auto-translation inside domain event handler may throw + fail the whole save | Wrap in try/catch; log error; mark translation status as `Failed` in `TranslationCache`. Do NOT fail the create. (Follows Category pattern.) |
| R6 | GAP-14: `TagTranslation` table unique-index collisions during concurrent creates | `DbUpdateException` → retry once with `INCLUDE` existing row lookup. Mirror `CategoryTranslationRepository` behavior. |
| R7 | Test file `IntegrationEventTypeRegistryTests.cs` method rename cascades | Each phase renames method: `AllRegisteredTypes_Contains_AllN_Events`. Easy but must remember. |
| R8 | GAP-14/15: admin UI (YallaJo.Web) not yet updated to show translations | Document as follow-up. API is forward-compatible. |

---

## 8. Commit Strategy

One commit per GAP phase, optional sub-commits for large ones.

| # | Commit message | GAP |
|---|----------------|-----|
| 1 | `feat(ContentCore): publish AttachmentUploaded/Deleted integration events via outbox` | GAP-10 |
| 2 | `feat(ContentCore): publish CategoryCreated/Updated/Deleted/Restored integration events via outbox` | GAP-09 |
| 3a | `feat(ContentCore): add TagTranslation entity + migration` | GAP-14 (1/3) |
| 3b | `feat(ContentCore): auto-translate tag names on create/update` | GAP-14 (2/3) |
| 3c | `feat(ContentCore): expose tag translations via Accept-Language header` | GAP-14 (3/3) |
| 4a | `feat(ContentCore): add SpecializationTranslation entity + migration` | GAP-15 (1/3) |
| 4b | `feat(ContentCore): auto-translate specialization names on create/update` | GAP-15 (2/3) |
| 4c | `feat(ContentCore): expose specialization translations via Accept-Language header` | GAP-15 (3/3) |
| 5 | `chore(ContentCore): update agent-context.md — deferred GAPs resolved` | Final doc sync |

### Build gates
After each commit:
- `dotnet build YallaJo.sln` → 0 errors
- `dotnet test tests/SharedKernel.Tests.Unit` → all pass (registry count test)
- `dotnet test tests/ContentCore.Tests.Unit` → all pass

---

## 9. What’s NOT in this plan (explicit non-goals)

- **GAP-12 BatchApproveTranslations** — separate feature, not a gap fix.
- **ContentSeo-side consumers** — separate PR in ContentSeo module after GAP-09/10 ship.
- **Admin UI for editing translations** — YallaJo.Web work, not API.
- **Translation approval workflow for Tag/Specialization** — reuse existing approval from GAP-11 + `ApproveTranslation` command as-is.
- **Per-tenant translation overrides** — out of scope; tags/specializations are system-wide.

---

## 10. Definition of Done

- [ ] All TODO anchors in source removed (replaced by actual implementation).
- [ ] `IntegrationEventTypeRegistry` count matches reality.
- [ ] Registry test passes with new count.
- [ ] `ContentCore.Tests.Unit` adds at least 1 test per new command/event.
- [ ] `dotnet build YallaJo.sln` → 0 errors.
- [ ] `agent-context.md` v2.5 published with updated build state.
- [ ] One consumer exists in code for each published integration event (GAP-09, GAP-10) — otherwise the event registration is pointless.
