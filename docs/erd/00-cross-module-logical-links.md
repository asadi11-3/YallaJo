# Cross-Module Logical Links

This document catalogs **every cross-module reference identifier** in YallaJo and
**clearly distinguishes real database foreign keys from logical references only**.

---

## Section A — Reference rules

1. YallaJo is a **modular monolith**; each of the 14 modules owns its **own schema** and
   **its own `DbContext`**.
2. **No database-level foreign key crosses a module boundary.** Every inter-module link is
   a plain `Guid` (or string) column with **no `HasOne`/`HasForeignKey`** to the other
   module's entity.
3. These logical references are kept consistent via **integration events**
   (Outbox in the producing module → Inbox in the consuming module).
4. Some modules persist **snapshot entities** (📸) — local read-model copies of another
   module's data — to avoid synchronous cross-module queries.
5. **Real DB foreign keys exist only *within* a single module's schema.**

**How to read this doc:**
- **LREF** = logical reference (no DB FK). Rendered as a dashed line in diagrams.
- **FK** = real database foreign key (enforced by EF, intra-module only).

---

## Section B — Reference catalog by identifier

Each table lists where the identifier is **used** (source module.entity.column) and what it
**logically points to** (target). Unless explicitly stated as a FK in
[Section C](#section-c--real-db-fk-vs-logical-reference), **all of these are logical-only
references with no DB foreign key.**

### `UserId` → `Security.User.Id`

The most widely referenced identifier. Also appears as role-qualified variants
(`ActorUserId`, `ReviewedByUserId`, `ApprovedByUserId`, `CreatedByUserId`,
`AssignedToUserId`, `ResolvedByUserId`, `RecipientUserId`, `GuideUserId`, `AgencyUserId`,
`OwnerUserId`, `SenderUserId`, `UploadedByUserId`, `RedactedByUserId`, `FollowerUserId`,
`InvitedUserId`, `ApplicantUserId`, `AdminUserId`, `SuspendedByAdminId`, etc.).

| Source module | Example entities / columns | Nullable variants |
|---------------|----------------------------|-------------------|
| Accounts | `Profile.UserId`, `ProviderApplication.UserId`, `ProviderApplication.ReviewedByUserId?`, `Agency*.GuideUserId / AgencyUserId / TerminatedByUserId?` | yes (review/terminate) |
| Auth | `Device.UserId`, `Session.UserId`, `RefreshToken.UserId`, `Otp.UserId`, `ActivationToken.UserId`, `PasswordResetToken.UserId`, `ExternalProvider.UserId` | no |
| Security | `AuditLog.UserId?`, `AuditLog.ActorUserId?` | yes |
| Booking | `TourBooking.UserId`, `TourGuide.UserId`, `SlotLock.UserId`, `JoinRequest.UserId`, `ProviderDocument.ReviewedByUserId?`, `GuideDiscount.GuideUserId`, `TourBooking.CompletedByUserId?` | yes (review/complete) |
| Finance | `Payment.UserId`, `Payout.RecipientUserId / ApprovedByUserId?`, `Invoice.UserId`, `Dispute.UserId / ResolvedByUserId? / ReviewedByAdminId? / EscalatedByAdminId?`, `DisputeMessage.SenderUserId`, `DisputeEvidence.UploadedByUserId`, `DiscountUsage.UserId`, `ProviderBankAccount.UserId`, `ProviderPaymentMethod.UserId / VerifiedByAdminId?` | yes (admin/review) |
| Analytics | `UserPreference.UserId`, `UserPreferredCategory.UserId`, `UserInteraction.UserId?`, `UserExcludedEntity.UserId`, `RecommendationCache.UserId?`, `GdprDeletionRequest.UserId`, `ExperimentAssignment.UserId`, `AuditLog.UserId? / RedactedByUserId?`, `SponsoredClickEvent.UserId?` | yes |
| Messaging | `Notification.UserId`, `NotificationPreference.UserId`, `DeviceToken.UserId`, `SupportTicket.CreatedByUserId / AssignedToUserId? / ResolvedByUserId?`, `TicketMessage.AuthorUserId`, `AdminAssignmentRoster.AdminUserId`, `UserSnapshot.UserId` 📸 | yes (assign/resolve) |
| Tracking | `LiveTrackingSession.UserId` | no |
| Social | `Review.UserId`, `ReviewReply.ProviderUserId`, `ReviewHelpfulVote.UserId`, `Report.ReporterUserId / ResolvedByUserId?`, `Favorite.UserId`, `ContentModerationLog.AdminUserId`, `UserModerationRecord.UserId / IssuedByAdminId` | yes (resolve) |
| ContentTours | `Tour.CreatedByUserId / ApprovedByUserId? / RejectedByUserId?`, `TourGuide.UserId / SuspendedByAdminId?`, `TourPackage.CreatedByUserId`, `TourProposal.GuideUserId / ReviewedByAdminId?`, `GuideApplication.GuideUserId / ReviewedByAdminId?`, `GuideTourOffering.AssignedByUserId? / SuspendedByAdminId?` | yes (admin) |
| ContentPlaces | `Place.CreatedByUserId`, `Business.OwnerId / ReviewedByUserId?`, `BusinessStaff.UserId` | yes (review) |
| ContentBlogs | `Blog.AuthorId / ReviewedByAdminId? / FeaturedByAdminId?`, `BlogComment.UserId`, `BlogCommentReaction.UserId`, `CreatorProfile.UserId / SuspendedByAdminId?`, `CreatorFollow.FollowerUserId`, `CreatorInvitation.InvitedUserId? / SentByAdminId / RedeemedByUserId?`, `CreatorApplication.ApplicantUserId / ReviewedByAdminId?` | yes (admin) |
| ContentCore | `Attachment.UploadedByUserId` | no |

### `TourId` → `ContentTours.Tour.Id`

| Source module | Columns |
|---------------|---------|
| Booking | `TourBooking.TourId`, `AvailabilitySlot.TourId?`, `GuideDiscount.TourId?`, `TourSnapshot.TourId` 📸, `PricingTierSnapshot.TourId` 📸 |
| Finance | `PaymentExpectation.TourId`, `Discount.TourId?` |
| Analytics | `BookingSnapshot.TourId` 📸, polymorphic `EntityId` where `EntityKind = Tour` |
| Social | `TourSnapshot.TourId` 📸, polymorphic `TargetId` where `TargetType = Tour` |
| ContentBlogs | `BlogTour.TourId` (⊕ join, Blog side only), `TourProposal.CreatedTourId?` (intra-module) |
| ContentTours (intra) | `TourTranslation.TourId` (**FK**), many child FKs — see [Section C](#section-c--real-db-fk-vs-logical-reference) |

### `BookingId` / `TourBookingId` → `Booking.TourBooking.Id`

| Source module | Columns |
|---------------|---------|
| Finance | `Payment.BookingId?`, `PaymentExpectation.BookingId`, `Invoice.BookingId`, `PayoutItem.BookingId`, `DiscountUsage.BookingId?` |
| Analytics | `PaymentSnapshot.BookingId` 📸, `BookingSnapshot.BookingId` 📸 |
| Social | `BookingEligibilitySnapshot` (verifies completed booking) 📸 |
| Tracking | `LiveTrackingSession.TourBookingId` |
| Booking (intra, FK-less) | `SlotLock.BookingId?`, `JoinRequest.ResultingBookingId?`, `TourBooking.JoinedFromBookingId?` (self-ref) |

### `PlaceId` → `ContentPlaces.Place.Id`

| Source module | Columns |
|---------------|---------|
| ContentTours | `Tour.PlaceId` (required), `TourProposal.PlaceId` |
| ContentBlogs | `Blog.PlaceId?` |
| Analytics | `SeasonalityRule.PlaceId`, `EntityAttributeSnapshot.PlaceId?` |
| ContentSeo | `WeatherCache.PlaceId?` |
| Social | `TourSnapshot.PlaceId?` 📸, `PlaceSnapshot.PlaceId` 📸, `BusinessSnapshot.PlaceId?` 📸 |
| ContentPlaces (intra, FK-less) | `Business.PlaceId` (scalar + index, **no FK**) — see edge cases |
| ContentPlaces (intra, FK) | `PlaceBusiness.PlaceId` (**FK**) |

### `ProviderId` / `OwnerProviderId` / `LinkedProviderId` → Accounts/Provider (Security.User acting as provider)

| Source module | Columns |
|---------------|---------|
| Booking | `TourBooking.ProviderId`, `ProviderSnapshot.ProviderId` 📸 |
| Finance | `Payment.ProviderId`, `Payout.ProviderId`, `PaymentExpectation.ProviderId`, `Invoice.ProviderId`, `BoostPackage`-style refs |
| Analytics | `PaymentSnapshot.ProviderId` 📸, `BookingSnapshot.ProviderId` 📸, `BoostPackage.ProviderId` |
| Social | `TourSnapshot.OwnerProviderId` 📸, `BusinessSnapshot.OwnerId` 📸 |
| ContentTours | `TourGuide.LinkedProviderId?` |
| ContentBlogs | `CreatorProfile.LinkedProviderId?` |
| Finance | `Discount.ProviderId?` |

### `BusinessId` → `ContentPlaces.Business.Id`

| Source module | Columns |
|---------------|---------|
| Booking | `AvailabilitySlot.BusinessId?`, `ProviderDocument.BusinessId?` (XOR with TourGuideId — see edge cases) |
| Finance | `Discount.BusinessId?` |
| Social | `BusinessSnapshot.BusinessId` 📸 |
| ContentPlaces (intra, FK) | `PlaceBusiness.BusinessId` (**FK**) |

### `CategoryId` → `ContentCore.Category.Id`

| Source module | Columns |
|---------------|---------|
| ContentPlaces | `Place.CategoryId?` |
| Finance | `Discount.CategoryId?` |
| Analytics | `UserPreferredCategory.CategoryId` |
| ContentCore (intra, FK) | `EntityCategory.CategoryId` (**FK**), `CategoryTranslation.CategoryId` (**FK**), `Category.ParentCategoryId?` (**FK**, self-ref) |

### `LanguageId` / `LanguageCode` → `ContentCore.Language.Id` (or ISO code)

> ⚠️ **Inconsistency:** most translations use `LanguageId` (Guid), but
> `ContentTours.TourPricingTierTranslation` uses `LanguageCode` (string). Both are
> logical references (no FK) to `Language`.

| Source module | Columns |
|---------------|---------|
| ContentTours | `TourTranslation.LanguageId`, `TourPricingTierTranslation.LanguageCode` (string) |
| ContentPlaces | `PlaceTranslation.LanguageId`, `BusinessTranslation.LanguageId` |
| ContentCore | `CategoryTranslation.LanguageId`, `TagTranslation.LanguageId`, `SpecializationTranslation.LanguageId` (all logical, **no FK** to Language) |
| ContentSeo | `FaqItemTranslation.LanguageId` |
| ContentBlogs | `Blog.LanguageId`, `BlogTranslation.LanguageId`, `CreatorApplication.LanguageIds[]` (JSON list) |
| Messaging | `UserSnapshot.LanguageCode` (string) 📸 |

### `SpecializationId` → `ContentCore.Specialization.Id`

| Source module | Columns |
|---------------|---------|
| Booking | `TourGuideSpecialization.SpecializationId` |
| ContentTours | `TourGuideSpecialization.SpecializationId` |

### `TourGuideId` / `GuideId` → `ContentTours.TourGuide.Id` (and Booking.TourGuide local copy)

| Source module | Columns |
|---------------|---------|
| Booking | `TourBooking.GuideId` (logical → ContentTours.TourGuide) |
| Tracking | `LiveTrackingSession.TourGuideId` |
| Booking/ContentTours (intra, FK) | child tables of the local `TourGuide` — see Section C |

### `PaymentId` → `Finance.Payment.Id`

| Source module | Columns |
|---------------|---------|
| Analytics | `PaymentSnapshot.PaymentId` 📸 |
| Finance (intra, FK-less) | `Invoice.PaymentId` (unique index, **no FK**), `Payment.OriginalPaymentId?` (self-ref, no FK), `PaymentExpectation.PaymentId?` |

### `BlogId` → `ContentBlogs.Blog.Id`

| Source module | Columns |
|---------------|---------|
| ContentBlogs (intra, FK) | `BlogTranslation.BlogId` (**FK**), `BlogComment.BlogId` (**FK**), `BlogTour.BlogId` (**FK**), `BlogView.BlogId` (**FK**) |

> `BlogId` is not referenced outside the ContentBlogs module.

### `WaypointId` → `ContentTours.TourWaypoint.Id`

| Source module | Columns |
|---------------|---------|
| Tracking | `TourCheckpoint.WaypointId` (logical, no FK; unique with `SessionId`) |

### `AttachmentId` → `ContentCore.Attachment.Id`

| Source module | Columns |
|---------------|---------|
| ContentCore (intra, FK) | `EntityImage.AttachmentId` (**FK**) |

### Polymorphic references — `EntityType`/`TargetType`/`SourceKind` + `EntityId`/`TargetId`/`SourceId`

These point to **different target entities depending on a discriminator**. Always logical
(no FK). Discriminator is an **enum** unless noted as **string**.

| Source module | Entity.columns | Discriminator type |
|---------------|----------------|--------------------|
| ContentCore | `Attachment(EntityType,EntityId)`, `EntityTag(EntityType,EntityId)`, `EntityCategory(EntityType,EntityId)`, `EntityImage(EntityType,EntityId)`, `TranslationCache(EntityType,EntityId?)` | enum / `EntityType`; TranslationCache = **string** |
| ContentSeo | `SeoMetadata(EntityType,EntityId)`, `FaqItem(EntityType,EntityId)` (enum); `SitemapEntry(EntityType,EntityId?)` | enum; SitemapEntry = **string** |
| ContentPlaces | `AccessibilityFeature(EntityType byte, EntityId)` | byte enum |
| Analytics | `UserInteraction`, `SuggestionBatch(SourceKind,SourceId)`, `RecommendationCache`, `BoostPackage`, `EditorialPin`, `PopularityScore`, `EntityPopularitySnapshot`, `UserExcludedEntity`, `SponsoredClickEvent`, `AuditLog(EntityType,EntityId)` | enum; AuditLog = **string** |
| Social | `Review(TargetType,TargetId)`, `EntityRatingCache(TargetType,TargetId)`, `BookingEligibilitySnapshot(TargetType,TargetId)` (ReviewTargetType); `Report`, `ContentModerationLog`, `UserModerationRecord(EntityType,EntityId)` (ReportableEntityType); `Favorite(EntityType,EntityId)` (FavoriteEntityType) | enum |
| Messaging | `Notification(EntityType,EntityId?)` | **string**, optional |
| ContentBlogs | `Blog.DisclosedTargets[]` → `DisclosureTarget(EntityType,EntityId)` stored as JSON | **string**, JSON |

---

## Section C — Real DB FK vs logical reference

### C.1 Real database foreign keys (intra-module only)

These are the **only** real, EF-enforced foreign keys in the system. All are within a
single module's schema.

| Module | Principal → Dependent | FK column | Delete behavior |
|--------|----------------------|-----------|-----------------|
| Accounts | ProviderApplication → ProviderDocument | `ApplicationId` | Cascade |
| Auth | Device → Session | `DeviceId` | Cascade (navigationless) |
| Auth | Session → RefreshToken | `SessionId` | Cascade (navigationless) |
| Security | User → Email | `UserId` | Cascade |
| Security | User → Phone | `UserId` | Cascade |
| Security | User → UserRole | `UserId` | Cascade |
| Security | User → UserClaim | `UserId` | Cascade |
| Security | Role → UserRole | `RoleId` | Cascade |
| Security | Role → RoleClaim | `RoleId` | Cascade |
| Booking | TourBooking → JoinRequest | `TourBookingId` | Restrict |
| Booking | TourGuide → TourGuideLanguage | `TourGuideId` | Cascade |
| Booking | TourGuide → TourGuideSpecialization | `TourGuideId` | Cascade |
| Booking | TourGuide → AvailabilitySlot | `TourGuideId` | Restrict |
| Booking | TourGuide → ProviderDocument | `TourGuideId` (**optional**, XOR) | Restrict |
| Booking | AvailabilitySlot → SlotLock | `AvailabilitySlotId` | Restrict (navigationless) |
| Finance | Payment → Dispute | `PaymentId` | Restrict |
| Finance | Dispute → DisputeMessage | `DisputeId` | Cascade |
| Finance | Dispute → DisputeEvidence | `DisputeId` | Cascade |
| Finance | Payout → PayoutItem | `PayoutId` | Restrict |
| Finance | Discount → DiscountUsage | `DiscountId` | Restrict |
| Finance | Invoice → InvoiceItem (owned) | `InvoiceId` | (owned collection) |
| Tracking | LiveTrackingSession → LocationSnapshot | `SessionId` | Cascade |
| Tracking | LiveTrackingSession → TourCheckpoint | `SessionId` | Cascade |
| Social | Review → ReviewReply (owned) | `ReviewId` | (owned collection) |
| ContentSeo | FaqItem → FaqItemTranslation | `FaqItemId` | Cascade |
| ContentPlaces | Place → PlaceTranslation | `PlaceId` | Cascade |
| ContentPlaces | Business → BusinessTranslation / BusinessHours / ServiceItem / BusinessStaff / BusinessAmenity | `BusinessId` | Cascade |
| ContentPlaces | PlaceBusiness → Place | `PlaceId` | Cascade |
| ContentPlaces | PlaceBusiness → Business | `BusinessId` | Cascade |
| ContentCore | Category → CategoryTranslation | `CategoryId` | Cascade |
| ContentCore | Category → Category (self) | `ParentCategoryId?` (**optional**) | Restrict |
| ContentCore | Tag → TagTranslation | `TagId` | Cascade |
| ContentCore | Specialization → SpecializationTranslation | `SpecializationId` | Cascade |
| ContentCore | EntityTag → Tag | `TagId` | Cascade |
| ContentCore | EntityCategory → Category | `CategoryId` | Cascade |
| ContentCore | EntityImage → Attachment | `AttachmentId` | Cascade |
| ContentTours | Tour → TourTranslation / TourSchedule / TourWaypoint / TourPricingTier / GuideTourOffering / TourChildFacility / TourTourGuide | `TourId` | Cascade |
| ContentTours | TourPricingTier → TourPricingTierTranslation | `TourPricingTierId` | Cascade |
| ContentTours | TourPackage → TourPackageTour / TourPackageInclusion | `TourPackageId` | Cascade |
| ContentTours | TourPackageTour → Tour | `TourId` | Restrict |
| ContentTours | TourGuide → TourGuideLanguage / TourGuideSpecialization | `TourGuideId` | Cascade |
| ContentTours | TourGuide → GuideAvailabilityBlock | `GuideId` | Cascade |
| ContentBlogs | Blog → BlogTranslation / BlogComment / BlogTour | `BlogId` | Cascade |
| ContentBlogs | Blog → BlogView | `BlogId` | Cascade (navigationless) |
| ContentBlogs | BlogComment → BlogComment (self) | `ParentCommentId?` (**optional**) | Restrict |
| ContentBlogs | BlogComment → BlogCommentReaction | `CommentId` | Cascade |
| ContentBlogs | CreatorProfile → CreatorFollow | `CreatorProfileId` | Cascade *(convention-inferred)* |
| Messaging | SupportTicket → TicketMessage | `TicketId` | Cascade |

### C.2 Logical references only (no DB FK)

**All identifiers in [Section B](#section-b--reference-catalog-by-identifier) are logical
references with no DB foreign key**, except the rows explicitly marked **FK** in C.1. This
includes every `UserId`, `TourId`, `BookingId`, `PlaceId`, `ProviderId`, `BusinessId`,
`CategoryId`, `LanguageId`, `SpecializationId`, `TourGuideId`/`GuideId`, `PaymentId`,
`WaypointId`, and every polymorphic `EntityType`+`EntityId` reference that crosses a module
boundary.

### C.3 Edge cases — same-module but **no FK** (logical within a module)

These reference an entity in the **same** module but are deliberately **not** modeled as EF
relationships (snapshot semantics, decoupling, or unique-index-only constraints):

| Module | Column | Points to (same module) | Modeling |
|--------|--------|-------------------------|----------|
| ContentPlaces | `Business.PlaceId` | `Place` | scalar + index, **no FK** (distinct from the `PlaceBusiness` join) |
| Finance | `Invoice.PaymentId` | `Payment` | **unique index**, no FK (de-facto 1:1) |
| Finance | `Payment.OriginalPaymentId?` | `Payment` (self) | scalar, no FK (refund → original) |
| Finance | `Payout.BankAccountId?` | `ProviderBankAccount` | scalar, no FK |
| Finance | `PayoutItem.CommissionRuleSnapshotId?` | `CommissionRule` | snapshot ref, no FK |
| Finance | `PaymentExpectation.PaymentId?` | `Payment` | scalar, no FK |
| Booking | `TourBooking.AvailabilitySlotId` | `AvailabilitySlot` | indexed, no FK |
| Booking | `JoinRequest.AvailabilitySlotId` | `AvailabilitySlot` | indexed, no FK |
| Booking | `TourBooking.JoinedFromBookingId?` | `TourBooking` (self) | indexed, no FK |
| Booking | `ProviderDocument.BusinessId?` | (cross-module Business) | scalar (XOR with `TourGuideId`) |
| Messaging | `NotificationDeliveryAttempt.NotificationId` | `Notification` | scalar, no FK |
| Auth | `RefreshToken.ReplacedByTokenId?` | `RefreshToken` (self) | scalar, no FK (rotation chain) |
| ContentTours | `TourProposal.CreatedTourId?` | `Tour` | scalar, no FK |
| ContentBlogs | `Blog.AuthoredByCreatorId?` | `CreatorProfile` | scalar, no FK |

> **`Booking.ProviderDocument` XOR constraint:** exactly one of `TourGuideId` (real FK,
> optional) or `BusinessId` (logical, cross-module) must be set — enforced by check
> constraint `CK_ProviderDocuments_SingleTarget`.
