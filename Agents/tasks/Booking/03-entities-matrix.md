# Booking Sprint — Entity Ownership Matrix

> Read alongside `01-pre-work.md` (which decided IAggregateRoot markers + AuditableEntity upgrades). PR reviewer rejects any aggregate/event combination not listed here.

---

## 1. Domain entities owned by Booking module

| Entity (file) | Base class after PW-2 | Aggregate? | Junction? | Domain events raised | Owner task | Notes |
|---|---|---|---|---|---|---|
| `TourBooking.cs` | `AuditableEntity, IAggregateRoot` | ✅ Yes | — | TourBookingCreated/Confirmed/Cancelled/Completed/Rejected/PaymentExpired | TASK 4 + 5 | Has 6 state transitions; reference YJ-YYYYMMDD-XXXXXX |
| `AvailabilitySlot.cs` | `AuditableEntity, IAggregateRoot` | ✅ Yes | — | AvailabilitySlotCapacityChanged | TASK 1 + 4 + 7 | RowVersion mandatory; computed `AvailableCount = MaxCapacity - BookedCount - LockedCount` is stored AND maintained |
| `SlotLock.cs` | `BaseEntity` (stays) | ❌ No | ✅ Yes (junction-like) | SlotLockCreated, SlotLockReleased | TASK 4 + 7 | TTL=10min; cleanup BG resets `IsActive=false`; unique index `(UserId, AvailabilitySlotId) WHERE IsActive=1` |
| `RefundPolicy.cs` | `AuditableEntity, IAggregateRoot` | ✅ Yes | — | (none) | TASK 2 | One per tour; child `RefundPolicyTier` value objects stored as JSON column |
| `CommissionRule.cs` | (moves to Finance) | — | — | — | — | **REMOVED FROM BOOKING DOMAIN.** Finance owns it. Booking calls `ICommissionLookupService.GetCommissionForProviderAsync(providerId, gross)` |
| `JoinRequest.cs` | `AuditableEntity, IAggregateRoot` | ✅ Yes | — | JoinRequestCreated/Approved/Rejected | TASK 6 | One per (booking, requesterUserId) — unique index |
| `ProviderDocument.cs` | `AuditableEntity, IAggregateRoot` | ✅ Yes | — | ProviderDocumentExpiring/Expired | TASK 3 + 7 | `DocumentType` enum has `IsCritical` extension method (see `DocumentTypeExtensions.cs` to create in TASK 3) |
| `Reservation.cs` | (stays stub) | — | — | — | **OUT OF SCOPE** | Wave 5 second sprint (business reservations, not tour bookings) |
| `PackageBooking.cs` | (stays stub) | — | — | — | **OUT OF SCOPE** | Phase 3 packaging sprint |
| `TourGuide.cs` | (stays stub) | — | — | — | **OUT OF SCOPE** | Wave 3 ContentTours TourGuide sprint owns it (already partial); Booking only references via `Tour.AssignedGuideId` (nullable Guid stamp) |
| `TourGuideLanguage.cs` | (stays stub) | — | — | — | **OUT OF SCOPE** | ditto |
| `TourGuideSpecialization.cs` | (stays stub) | — | — | — | **OUT OF SCOPE** | ditto |

## 2. Entities Booking READS from other modules (via integration events or read-only repos)

| Source module | Entity / Field | How Booking accesses | Used in |
|---|---|---|---|
| ContentTours | `Tour.Id, BasePrice, Currency, MaxGroupSize, IsInstantBooking, MinAge, AgeRestriction, IsActive, Status, ProviderId, Timezone` | Inbox of `content-tours.tour.published.v1` populates a local `BookingTourSnapshot` read model (LAST-WRITE-WINS). DO NOT direct-FK. | POST /tour Step 1, search, RefundPolicy assignment |
| ContentTours | `TourPricingTier.*` (Adult/Child/Infant/Senior/Group/Private) | Inbox of `content-tours.tour-pricing.upserted.v1` populates local `BookingTourPricingSnapshot`. | POST /tour Step 3 (sum tier × quantity) |
| Accounts | `Provider.Id, Status, SubscriptionTier` | Inbox `accounts.provider.status-changed.v1` populates local `BookingProviderSnapshot`. | POST /tour (suspend check), commission lookup, payouts (out-of-sprint) |
| Finance.Contracts | `ICommissionLookupService` (interface only — Booking does NOT see Finance DbContext) | DI-injected service call | POST /tour Step 3 |
| Finance.Contracts | `IDiscountEvaluator` (interface only — stub returns None for this sprint) | DI-injected | POST /tour Step 3 |

> The read-snapshot tables live in Booking's own schema:
> - `booking.TourSnapshots` (PK = TourId, has RowVersion, last-event-id)
> - `booking.TourPricingSnapshots` (composite PK TourId+TierType)
> - `booking.ProviderSnapshots` (PK = ProviderId)
>
> Created in PW-9 (TBD migration `BookingAddReadSnapshots`). If the migration was missed in pre-work, add it as the first step of TASK 4.

## 3. Integration Events Emitted by Booking (consumed by other modules)

| Event name (logical) | When emitted | Payload fields | Downstream consumers |
|---|---|---|---|
| `booking.tour-booking.created.v1` | After POST /tour completes Step 4 | BookingId, UserId, TourId, AvailabilitySlotId, ParticipantCount, TotalAmount, Currency, Reference, CreatedAt | Analytics (interaction log), Finance (initial payment readiness) |
| `booking.tour-booking.confirmed.v1` | Payment confirmed (instant) OR provider confirms OR auto-confirm | BookingId, UserId, TourId, ConfirmedAt, ConfirmationSource (Instant/Manual/Auto) | Messaging (BookingConfirmed notification), Finance (escrow start clock), Analytics (BookingCompleted intent) |
| `booking.tour-booking.cancelled.v1` | Any cancel path | BookingId, UserId, TourId, CancelledAt, CancelledBy (User/Provider/Admin/System), Reason, RefundAmount, RefundCurrency | Messaging (BookingCancelled notification), Finance (initiate refund), Analytics, Social (favorites cleanup later) |
| `booking.tour-booking.completed.v1` | Provider marks complete OR auto on tour-end + buffer | BookingId, UserId, TourId, CompletedAt | Social (review window opens), Finance (escrow → payout eligible), Analytics |
| `booking.tour-booking.rejected.v1` | Provider rejects pending-confirmation booking | BookingId, UserId, TourId, RejectedAt, Reason | Messaging, Finance (auto-full-refund), Analytics |
| `booking.tour-booking.payment-expired.v1` | BookingAutoExpireService cancels AwaitingPayment >10min | BookingId, UserId, TourId, ExpiredAt | Messaging (suppressed by default — silent), Analytics |
| `booking.slot-lock.expired.v1` | SlotLockCleanupService releases an expired lock | SlotLockId, AvailabilitySlotId, UserId, BookingId (nullable), CreatedAt, ExpiredAt | Analytics only (other consumers prefer `tour-booking.payment-expired` for context) |
| `booking.provider-document.expiring.v1` | DocumentExpiryCheckService finds doc within 30d | ProviderDocumentId, ProviderId, DocumentType, ExpiresAt, DaysRemaining | Messaging (DocumentExpiring notification) |
| `booking.provider-document.expired.v1` | doc passes ExpiresAt | ProviderDocumentId, ProviderId, DocumentType, IsCritical | Messaging (DocumentExpired notification), Accounts (if IsCritical) |
| `booking.provider.suspended-doc-expired.v1` | Critical doc expired → suspend provider | ProviderId, TriggeringDocumentId, DocumentType, SuspendedAt | Accounts (flips Provider.Status), ContentTours (`Tour.IsSearchable=false` for all provider's tours), Messaging |
| `booking.join-request.approved.v1` | Booking owner approves join request | JoinRequestId, BookingId, NewParticipantUserId, ApprovedAt | Messaging, Finance (charge new participant share), Analytics |
| `booking.join-request.rejected.v1` | Booking owner rejects | JoinRequestId, BookingId, RequesterUserId, RejectedAt, Reason | Messaging |

> **Registry rule:** every event MUST appear in `IntegrationEventTypeRegistry` with its logical name. PW-4 acceptance test verifies the registry → class type → logical name reverse-parity (no orphans either direction).

## 4. Integration Events Booking CONSUMES (writes inbox handlers)

| Event name (logical) | Source module | Handler responsibility |
|---|---|---|
| `content-tours.tour.published.v1` | ContentTours | Upsert `BookingTourSnapshot` |
| `content-tours.tour.updated.v1` | ContentTours | Upsert `BookingTourSnapshot`; if MaxGroupSize decreased, do NOT touch existing AvailabilitySlots (booked seats stay; per PDF 2 §2.5 cannot reduce below highest BookedCount — that rule lives in ContentTours validator) |
| `content-tours.tour.suspended.v1` | ContentTours | Set `BookingTourSnapshot.IsActive=false`; existing bookings honored (do nothing else) |
| `content-tours.tour.deleted.v1` | ContentTours | Set `BookingTourSnapshot.IsActive=false`; do NOT delete the snapshot (FK preserved for historical bookings) |
| `content-tours.tour-pricing.upserted.v1` | ContentTours | Upsert `BookingTourPricingSnapshot` |
| `content-tours.tour-pricing.deleted.v1` | ContentTours | Delete the snapshot row |
| `accounts.provider.status-changed.v1` | Accounts | Upsert `BookingProviderSnapshot.Status` |
| `accounts.provider.subscription-changed.v1` | Accounts (out of this sprint scope — schema field nullable for now) | Upsert `BookingProviderSnapshot.SubscriptionTier`; if missing, default to "Free" |
| `finance.payment.completed.v1` | Finance (lands in Booking inbox, Finance owns the event) | Transition booking AwaitingPayment → Confirmed (instant) OR PendingConfirmation (non-instant). Idempotent via TransactionId. |
| `finance.payment.failed.v1` | Finance | Release SlotLock; restore capacity; booking stays AwaitingPayment until cleanup BG cancels it |
| `finance.refund.completed.v1` | Finance | Booking status update (already Cancelled but stamp RefundedAt) |

> Inbox table: `booking.InboxMessages` (PK = MessageId from outbox writer). All consumer handlers MUST call `IBookingInboxStore.HasBeenProcessedAsync(messageId)` first, return early if true; do work; then `MarkAsProcessed(messageId)` BEFORE single `SaveChangesAsync` per INDEX §4 R15.

## 5. New enums to add (PW already booked them; doing it here for visibility)

| Enum | Values | Location |
|---|---|---|
| `BookingStatus` | AwaitingPayment, PendingConfirmation, Confirmed, Cancelled, Completed, Rejected, Disputed | `Booking.Domain/Enums/` (already exists — verify values match) |
| `CancellationSource` | User, Provider, Admin, System | `Booking.Domain/Enums/` |
| `ConfirmationSource` | Instant, Manual, Auto | `Booking.Domain/Enums/` |
| `DocumentType` | IndependentGuideID, GovernmentID, MoTALicense, TourismAuthorityLicense, InsuranceCertificate, BusinessLicense, TaxRegistration, FirstAidCertification, HealthSafetyCertificate, FireSafetyCertificate, ActivityCertification, LiabilityInsurance, ProofOfOwnership, AffiliatedGuideList, AgencyRegistration | `Booking.Domain/Enums/` (extend existing) |
| `DocumentStatus` | Pending, Approved, Rejected, Expired | `Booking.Domain/Enums/` |
| `JoinRequestStatus` | Pending, Approved, Rejected, Expired | `Booking.Domain/Enums/` |
| `SlotType` | TourSlot, BusinessSlot | `Booking.Domain/Enums/` (existing) — note BusinessSlot is unused in this sprint (Wave 5 second-half) |

## 6. Value objects to introduce

| VO | Fields | Where |
|---|---|---|
| `Money` | `decimal Amount, string Currency` (3-letter, validated) | `Booking.Domain/ValueObjects/Money.cs` — owned by Booking (Finance has its own copy later; do NOT cross-reference. Use logical equality on (Amount, Currency)) |
| `RefundPolicyTier` | `int HoursBeforeTour, decimal RefundPercentage` | `Booking.Domain/ValueObjects/RefundPolicyTier.cs` — stored as owned-JSON column on RefundPolicy aggregate |
| `BookingReference` | `string Value` (validated against regex `^YJ-\d{8}-[A-Z2-9]{6}$`) | `Booking.Domain/ValueObjects/BookingReference.cs` — strongly typed wrapper |
| `BookingCancellationContext` | `CancellationSource Source, string Reason, bool ProviderInitiated, bool ForceMajeureOverride` | `Booking.Domain/ValueObjects/BookingCancellationContext.cs` — input to `TourBooking.Cancel(...)` |

## 7. Persistence layout (`Booking.Infrastructure/Persistence/`)

| File | Responsibility |
|---|---|
| `BookingDbContext.cs` | Already exists; ensure `OnModelCreating` calls `modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);` |
| `BookingDbContextInitializer.cs` | Seed reference data (default RefundPolicy template per tour? NO — placeholder only) |
| `BookingDbContextFactory.cs` | Design-time factory for migrations |
| `BookingUnitOfWork.cs` | Delegate to `IUnitOfWork<BookingDbContext>` per PW-1 |
| `BookingInboxStore.cs` | Implements `IBookingInboxStore` for inbox handlers |
| `Configurations/TourBookingConfiguration.cs` | RowVersion, indexes (Reference UNIQUE, UserId+Status, AvailabilitySlotId+Status), decimal precision (19,4) |
| `Configurations/AvailabilitySlotConfiguration.cs` | RowVersion, computed-column note for AvailableCount (or maintain in domain methods), indexes (TourId+Date, BusinessId+Date) |
| `Configurations/SlotLockConfiguration.cs` | Unique filtered index `(UserId, AvailabilitySlotId) WHERE IsActive=1`, index `ExpiresAt WHERE IsActive=1` for BG cleanup |
| `Configurations/RefundPolicyConfiguration.cs` | Owned-JSON `RefundPolicyTier[]`, UNIQUE TourId |
| `Configurations/JoinRequestConfiguration.cs` | UNIQUE (BookingId, RequesterUserId) |
| `Configurations/ProviderDocumentConfiguration.cs` | UNIQUE (ProviderId, DocumentType) for non-Rejected status (filtered) |
| `Configurations/BookingTourSnapshotConfiguration.cs` | PK=TourId, rowversion, last-event-id stored as string |
| `Configurations/BookingTourPricingSnapshotConfiguration.cs` | Composite PK (TourId, TierType) |
| `Configurations/BookingProviderSnapshotConfiguration.cs` | PK=ProviderId |
| `Configurations/InboxMessageConfiguration.cs` | Mirror SharedKernel inbox pattern |
| `Configurations/OutboxMessageConfiguration.cs` | Mirror SharedKernel outbox pattern (CompositeOutboxProcessor already exists in host) |

## 8. Migration sequence

| # | Migration | Adds | Owner |
|---|---|---|---|
| 1 | `BookingAddAggregateRootAndAuditMembers` | IsDeleted, DeletedAt, RowVersion on TourBooking/AvailabilitySlot/RefundPolicy/JoinRequest/ProviderDocument | PW-2 |
| 2 | `BookingAddReadSnapshots` | TourSnapshots / TourPricingSnapshots / ProviderSnapshots tables + indexes | PW (or TASK 4 if missed) |
| 3 | `BookingAddSlotLockFilteredIndex` | UNIQUE filtered index on SlotLock | TASK 1 |
| 4 | `BookingAddRefundPolicyJsonColumn` | Tiers JSON column | TASK 2 |
| 5 | `BookingAddProviderDocumentExpiryColumns` | ExpiryWarningSent bit, ExpiryProcessed bit (default 0) | TASK 3 |
| 6 | `BookingAddTourBookingReferenceIndex` | UNIQUE index on Reference | TASK 4 |

> All migrations applied by Tech Lead during the deployment window before Day-N freeze. Devs commit them but do NOT run `database update` against shared dev DB without Tech Lead approval (per error-log.md history).

---

**Next read:** `04-task-availability-slots.md` (TASK 1, Mahmoud).
