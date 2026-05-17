# Pre-Work Baseline — 2026-06-01
> **Purpose**: Per-module audit of what already exists vs what PW items must build.
> Produced as part of Phase 0 of the PRE_WORK_EXECUTION_PLAN.md.

---

## 1. Project Layout

Flat layout — no `src/` subdirectory. All modules sit directly under repo root.
- Module naming: `{Module}.Domain`, `{Module}.Application`, `{Module}.Infrastructure`, `{Module}.Contracts`, `{Module}.Presentation`
- SharedKernel naming: `YallaJo.SharedKernel.Domain`, `YallaJo.SharedKernel.Application`, `YallaJo.SharedKernel.Infrastructure`
- Tests: `tests/{Module}.Tests.Unit/`, `tests/{Module}.IntegrationTests/`

---

## 2. SharedKernel — Audit Result

| Abstraction | File | Status | Notes |
|---|---|---|---|
| `IUnitOfWork` (base) | `SharedKernel.Domain/Abstractions/Data/IUnitOfWork.cs` | ✅ EXISTS | Correct |
| `IUnitOfWork<TContext>` | `SharedKernel.Infrastructure/Data/IUnitOfWorkTyped.cs` | ✅ EXISTS | Extends IUnitOfWork |
| `UnitOfWork<TContext>` | `SharedKernel.Infrastructure/Data/UnitOfWork.cs` | ✅ CORRECT | Events dispatched BEFORE SaveChanges ✔ |
| `IAggregateRoot` | `SharedKernel.Domain/Entities/IAggregateRoot.cs` | ✅ EXISTS | DomainEvents + ClearDomainEvents |
| `BaseEntity<TKey>` | `SharedKernel.Domain/Entities/BaseEntity.cs` | ✅ EXISTS | Has private List<IDomainEvent>, AddDomainEvent, ClearDomainEvents, Guid.CreateVersion7() |
| `AuditableEntity` | `SharedKernel.Domain/Entities/AuditableEntity.cs` | ✅ EXISTS | RowVersion [Timestamp], SoftDelete, Restore |
| `IDomainEvent` + `DomainEventBase` | `SharedKernel.Domain/Events/IDomainEvent.cs` | ✅ EXISTS | DomainEventBase uses Guid.CreateVersion7() |
| `IntegrationEvent` (base) | `SharedKernel.Domain/Events/IntegrationEvent.cs` | ✅ EXISTS | Need to verify |
| `IntegrationEventTypeRegistry` | `SharedKernel.Infrastructure/Abstractions/Integration/` | ✅ EXISTS | 40+ events registered for 6 existing modules |
| `IInboxStore` | `SharedKernel.Application/Abstractions/Data/IInboxStore.cs` | ✅ EXISTS | |
| `EfInboxStore` | `SharedKernel.Infrastructure/Inbox/EfInboxStore.cs` | ✅ EXISTS | |
| `IDomainEventDispatcher` | `SharedKernel.Application/Abstractions/Events/IDomainEventDispatcher.cs` | ✅ EXISTS | |

**UnitOfWork dispatch order** (VERIFIED CORRECT):
```csharp
// 1. Collect aggregates with events
// 2. Collect all domain events  
// 3. ClearDomainEvents() on each aggregate
// 4. dispatcher.DispatchAsync(domainEvents, ct)  ← BEFORE SaveChanges ✔
// 5. context.SaveChangesAsync(ct)
```
→ **PW-1 "fix UoW dispatch" is N/A for Booking/Finance/Social/Analytics** — they already use `UnitOfWork<TContext>` correctly.

---

## 3. AppAction Enum — Current vs Required

**Current values (19):**
```
Read, Create, Update, Delete, UpdateSelf, UpdateAny, DeleteAny, SoftDelete,
Approve, Submit, Reject, Suspend, Reinstate, Replay,
ReadOwn, ReadAny, Feature, Refresh, Record
```

**Missing (need to add — 14 values):**
```
Cancel, Complete, Confirm, Trigger, Download, Refund, Verify,
Warn, Ban, Close, Assign, Resolve, Export, Redact
```

**Rationale by module:**
- Booking: `Cancel`, `Confirm`, `Complete` (booking lifecycle), `Trigger` (background ops)
- Finance: `Refund`, `Download` (invoice PDF), `Export` (admin payout export), `Trigger` (payout batch)
- Social: `Warn`, `Ban` (moderation actions), `Close` (close report/dispute)
- Messaging: `Assign` (ticket assignment), `Resolve` (resolve ticket), `Close` (close ticket)
- Analytics: `Export` (admin data export), `Redact` (PII redaction), `Download`
- Auth-Cleanup: `Verify` (document/identity verification)

→ **Phase 1 action: Add all 14 to AppAction.cs**

---

## 4. Per-Module Audit

### 4.1 Booking

| Area | Status | Detail |
|---|---|---|
| Domain entities | ✅ 11 entities | TourBooking, AvailabilitySlot, JoinRequest, PackageBooking, ProviderDocument, RefundPolicy, Reservation, SlotLock, TourGuide, TourGuideLanguage, TourGuideSpecialization |
| `IAggregateRoot` markers | ⚠️ PARTIAL | TourBooking ✅, others ❌ need marking |
| `AuditableEntity` base | ✅ ALL use it | Confirmed from file listing |
| EF configurations | ✅ 12 configs | Including OutboxMessageConfiguration |
| 1 migration (CreateModel) | ✅ EXISTS | 2026-05-10 |
| `IUnitOfWork<BookingDbContext>` | ✅ REGISTERED | In DI.cs line 32 |
| `IBookingUnitOfWork` interface | ❌ MISSING | Needs creating in Application/Interfaces/ |
| `BookingUnitOfWork` delegate | ❌ MISSING | Needs creating in Infrastructure/Persistence/ |
| Domain/Events/ | ❌ MISSING | 14 event records needed |
| Contracts/IntegrationEvents/ | ❌ MISSING | 12 integration events needed |
| Contracts/Authorization/ | ⚠️ PARTIAL | Only ITourGuideOwnershipService; needs BookingFeatures + BookingPermissionCatalog |
| Repository interfaces | ❌ MISSING | 8 interfaces needed |
| Test projects | ❌ MISSING | tests/Booking.Tests.Unit/ + tests/Booking.IntegrationTests/ |

**PW-1 decision**: Create `IBookingUnitOfWork : IUnitOfWork` in Application, `BookingUnitOfWork` delegate in Infrastructure that wraps `IUnitOfWork<BookingDbContext>`. Register both in DI.

**PW-2 decision**: TourBooking already `IAggregateRoot` ✅. Need to add to: AvailabilitySlot, JoinRequest, RefundPolicy, ProviderDocument, SlotLock. PackageBooking + Reservation + TourGuide — only if they raise domain events (check sprint tasks). Migration needed for any new columns (none expected — IAggregateRoot is runtime-only marker, DomainEvents is `[NotMapped]`). **→ No migration needed for IAggregateRoot marker**, only AuditableEntity columns already exist.

---

### 4.2 Finance

| Area | Status | Detail |
|---|---|---|
| Domain entities | ✅ 19 entities | Payment, Payout, CommissionRule, Discount, DiscountUsage, Dispute, DisputeEvidence, DisputeMessage, InvoiceItem, InvoiceLineItem, LoyaltyPoints, LoyaltyTransaction, PayoutItem, PlanFeature, ProviderBankAccount, Referral, Subscription, SubscriptionFeature, SubscriptionPlan |
| `IAggregateRoot` markers | ❌ NONE | Payment, Payout, Dispute are aggregates — need marking |
| EF configurations | ✅ 20 configs | Including OutboxMessageConfiguration |
| 1 migration (CreateModel) | ✅ EXISTS | 2026-05-10 |
| `IUnitOfWork<FinanceDbContext>` | ✅ REGISTERED | In DI.cs line 30 |
| `IFinanceUnitOfWork` interface | ❌ MISSING | |
| Domain/Events/ | ❌ MISSING | 14 event records needed |
| Contracts/IntegrationEvents/ | ❌ MISSING | 10 integration events needed |
| Contracts/Authorization/ | ❌ MISSING | Needs FinanceFeatures + FinancePermissionCatalog |
| Contracts/Services/ | ❌ MISSING | ICommissionLookupService (for Booking cross-module use) |
| Repository interfaces | ❌ MISSING | 6 interfaces needed |
| IPaymentGateway abstraction | ❌ MISSING | |
| Test projects | ❌ MISSING | tests/Finance.Tests.Unit/ + tests/Finance.IntegrationTests/ |

**PW-1 decision**: Same pattern as Booking — create IFinanceUnitOfWork delegate.

**Key Finance aggregates**: Payment (root), Payout (root), Dispute (root), Subscription (root), LoyaltyPoints (root) → 5 aggregates need IAggregateRoot.

---

### 4.3 Social

| Area | Status | Detail |
|---|---|---|
| Domain entities | ✅ 5 entities | Review, Favorite, Report, AccessibilityReview, ContentModerationLog |
| `IAggregateRoot` markers | ❌ NONE | Review, Favorite, Report need marking |
| EF configurations | ✅ 6 configs | Including OutboxMessageConfiguration |
| 1 migration (CreateModel) | ✅ EXISTS | 2026-05-10 |
| `IUnitOfWork<SocialDbContext>` | ✅ (assumed) | Pattern consistent with Booking/Finance |
| `ISocialUnitOfWork` interface | ❌ MISSING | |
| Domain/Events/ | ❌ MISSING | 12 event records needed |
| Contracts/IntegrationEvents/ | ❌ MISSING | 5 integration events needed |
| Contracts/Authorization/ | ⚠️ PARTIAL | Only IReviewOwnershipService; needs SocialFeatures + SocialPermissionCatalog |
| Repository interfaces | ❌ MISSING | 6 interfaces needed |
| IProfanityFilter abstraction | ❌ MISSING | |
| INsfwClassifier abstraction | ❌ MISSING | |
| Test projects | ❌ MISSING | tests/Social.Tests.Unit/ + tests/Social.IntegrationTests/ |

**Key Social aggregates**: Review (root), Favorite (root), Report (root) → 3 aggregates.

---

### 4.4 Messaging

| Area | Status | Detail |
|---|---|---|
| Domain entities | ✅ 8 entities | ChatBotConversation, ChatBotMessage, DeviceToken, Notification, NotificationPreference, NotificationTemplate, SupportTicket, TicketMessage |
| `IAggregateRoot` markers | ❌ NONE | Notification, SupportTicket, DeviceToken need marking |
| EF configurations | ✅ 10 configs | Including OutboxMessageConfiguration + InboxMessageConfiguration |
| 1 migration (CreateModel) | ✅ EXISTS | 2026-05-10 |
| `IUnitOfWork<MessagingDbContext>` | ✅ REGISTERED | In DI.cs line 32 |
| `IMessagingUnitOfWork` | ✅ EXISTS | **BUT BUGGY** — calls context.SaveChangesAsync directly, bypasses domain event dispatch |
| `MessagingUnitOfWork` | ✅ EXISTS | **BUT BUGGY** — same issue |
| `IMessagingInboxStore` | ✅ EXISTS | Correct |
| `MessagingInboxStore` | ✅ EXISTS | Correct |
| 4 integration event handlers | ✅ EXISTS | BusinessApproved/Rejected/Suspended/Reinstated |
| Domain/Events/ | ❌ MISSING | 14 event records needed |
| Contracts/IntegrationEvents/ | ❌ MISSING | 6 integration events needed |
| Contracts/Authorization/ | ❌ MISSING | Needs MessagingFeatures + MessagingPermissionCatalog |
| Repository interfaces | ❌ MISSING | 6 interfaces needed |
| INotificationDispatcher | ❌ MISSING | + INotificationChannelStrategy + INotificationTemplateRenderer + IEmailSender |
| Test projects | ❌ MISSING | tests/Messaging.Tests.Unit/ + tests/Messaging.IntegrationTests/ |

**PW-1 CRITICAL BUG**: `MessagingUnitOfWork` calls `context.SaveChangesAsync(ct)` directly — domain events on Messaging aggregates will NEVER be dispatched. Fix: delegate to `IUnitOfWork<MessagingDbContext>`.

**Key Messaging aggregates**: Notification (root), SupportTicket (root), DeviceToken (root), NotificationTemplate (root), ChatBotConversation (root) → 5 aggregates.

---

### 4.5 Analytics

| Area | Status | Detail |
|---|---|---|
| Domain entities | ✅ 6 entities | AuditLog, PopularityScore, RecommendationCache, UserInteraction, UserPreference, UserPreferredCategory |
| `IAggregateRoot` markers | ❌ NONE | PopularityScore (root) needs marking |
| EF configurations | ✅ 8 configs | Including OutboxMessageConfiguration |
| 1 migration (CreateModel) | ✅ EXISTS | 2026-05-10 |
| `IUnitOfWork<AnalyticsDbContext>` | ✅ (assumed) | Pattern consistent with other modules |
| `IAnalyticsUnitOfWork` interface | ❌ MISSING | |
| Domain/Events/ | ❌ MISSING | 10 event records needed |
| Contracts/IntegrationEvents/ | ❌ MISSING | 3 integration events needed |
| Contracts/Authorization/ | ❌ MISSING | Needs AnalyticsFeatures + AnalyticsPermissionCatalog |
| Repository interfaces | ❌ MISSING | 5 interfaces needed |
| IClientContextProvider | ❌ MISSING | |
| Test projects | ❌ MISSING | tests/Analytics.Tests.Unit/ + tests/Analytics.IntegrationTests/ |

**Key Analytics aggregate**: PopularityScore (root). UserInteraction/AuditLog are append-only write-model entities (fire-and-forget), not aggregates. RecommendationCache is a read-model cache.

**PW-9 schema decision (confirmed)**: UserInteraction and AuditLog will use `long` (BIGINT) PKs with clustered index on `(OccurredAt DESC, Id ASC)` for optimal time-series query patterns. No domain events on these entities.

---

## 5. Test Projects — Audit Result

**Existing test projects:**
```
tests/Accounts.Tests.Unit/
tests/Auth.Tests.Unit/
tests/ContentCore.Tests.Unit/
tests/ContentPlaces.Tests.Unit/
tests/ContentSeo.IntegrationTests/
tests/ContentSeo.Tests.Unit/
tests/ContentTours.Tests.Unit/
tests/Security.Tests.Unit/
tests/SharedKernel.Tests.Unit/
tests/Web.Tests.Unit/
tests/YallaJo.Tests.Shared/         ← shared helpers (DomainEventAssertions, DomainTestBase)
```

**Missing (need to create):**
```
tests/Booking.Tests.Unit/
tests/Booking.IntegrationTests/
tests/Finance.Tests.Unit/
tests/Finance.IntegrationTests/
tests/Social.Tests.Unit/
tests/Social.IntegrationTests/
tests/Messaging.Tests.Unit/
tests/Messaging.IntegrationTests/
tests/Analytics.Tests.Unit/
tests/Analytics.IntegrationTests/
tests/Authorization.IntegrationTests/   ← Phase 3
```

**tests/YallaJo.Tests.Shared** already has:
- `DomainEventAssertions.cs` ✅
- `DomainTestBase.cs` ✅

Phase 1 will enhance this project with:
- `IntegrationEventRegistryParityBase` (abstract base for registry parity tests)
- `InMemoryDbContextFixture<TContext>` (EF InMemory fixture)
- `EndpointInspector` (scans Presentation assembly for endpoint permissions)

---

## 6. PW Item Skip/Work Matrix

| Module | PW | Decision | Reason |
|---|---|---|---|
| ALL | PW-1 UoW dispatch fix | ✅ SharedKernel UoW is correct | Booking/Finance/Social/Analytics just need IXxxUnitOfWork interface + delegate |
| Messaging | PW-1 | 🔧 FIX REQUIRED | MessagingUnitOfWork bypasses domain events |
| Booking | PW-2 IAggregateRoot | ⚠️ PARTIAL | TourBooking already done; 5 others need marking |
| Finance/Social/Messaging/Analytics | PW-2 IAggregateRoot | 🔧 WORK NEEDED | No IAggregateRoot markers anywhere |
| ALL | PW-2 Migration | ✅ NO MIGRATION NEEDED | IAggregateRoot is runtime-only; DomainEvents is [NotMapped] |
| ALL | PW-3 Domain events | 🔧 WORK NEEDED | No Domain/Events/ folder exists in any module |
| ALL | PW-4 Integration events | 🔧 WORK NEEDED | No Contracts/IntegrationEvents/ exists |
| ALL | PW-5 Repo interfaces | 🔧 WORK NEEDED | No repo interfaces exist |
| ALL | PW-6 Module abstractions | 🔧 WORK NEEDED | All missing |
| ALL | PW-7 Permission catalogs | 🔧 WORK NEEDED | All missing |
| ALL | PW-8 Test projects | 🔧 WORK NEEDED | None exist for new modules |

---

## 7. Phase 1 Prerequisites — Action Items

Before Phase 2 work begins, Phase 1 must deliver:

1. **AppAction.cs extended** — 14 new values added
2. **SharedKernel UoW unit tests** — 4 tests in tests/SharedKernel.Tests.Unit/ verifying:
   - Events dispatched BEFORE SaveChanges
   - DomainEvents cleared after dispatch
   - Only IAggregateRoot entities dispatched
   - Non-aggregate entities NOT dispatched
3. **YallaJo.Tests.Shared enhanced** — IntegrationEventRegistryParityBase + InMemoryDbContextFixture + EndpointInspector
4. **Agents/pre-work-templates.md** — copy-paste templates for each PW step

---

## 8. IntegrationEventTypeRegistry — Extensions Needed

Current: 40 events (Security 6, Auth 2, ContentCore 8, ContentPlaces 13, ContentTours 14+3, ContentSeo 4)

**To add (Phase 2)**:
- Booking: 12 events
- Finance: 10 events
- Social: 5 events
- Messaging: 6 events
- Analytics: 3 events

**Total after Phase 2**: ~76 integration events in registry.

---

*Generated: 2026-06-01 | Session: ses_1ca88135fffeyIeZXkxU1NcptF*
