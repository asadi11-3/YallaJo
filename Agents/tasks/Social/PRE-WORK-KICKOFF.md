# Social — Pre-Work Kickoff Briefing

> Sprint window: 2026-10-19 → 11-27 (Wave 6). Owner: Fadwa.

## What's already wired

- `ISocialUnitOfWork` + `SocialUnitOfWork` delegate.
- 3 aggregate roots: Review (already had), Favorite, Report. AccessibilityReview + ContentModerationLog remain flat audit-style entities (not aggregates).
- 12 domain events in `Social.Domain/Events/` (Review×6, Favorite×2, Report×2, Content×2).
- 5 integration events in `Social.Contracts/IntegrationEvents/` registered as `social.{aggregate}.{action}.v1`.
- 4 repositories: IReviewRepository (+ExistsForUserAndTargetAsync to enforce verified-booking gate), IFavoriteRepository (+CountByUserAsync for 500/user cap), IReportRepository (+GetPendingAsync), ISocialOutboxWriter. Plus existing IReviewOwnershipService.
- `IProfanityFilter` + `INsfwClassifier` in `Social.Contracts/Services/` with `NoopProfanityFilter` (returns false / pass-through) and `NoopNsfwClassifier` (Score=0, IsExplicit=false) registered.
- `SocialFeatures` (6) + `SocialPermissionCatalog` (19 perms — uses ContentManagement + ModerationTools groups).
- Test projects scaffolded.

## Day-0 sprint tasks

1. EF migration for IAggregateRoot markers (Favorite, Report).
2. Replace `NoopProfanityFilter` with real implementation (3rd-party or in-house wordlist).
3. Replace `NoopNsfwClassifier` if image moderation lands this sprint (otherwise leave Noop).
4. Implement verified-booking gate inside `CreateReviewCommandHandler` — call `IReviewRepository.ExistsForUserAndTargetAsync` AND check Booking module for completed booking before allowing.
5. Implement Bayesian weighted rating in `ReviewAggregator` (separate service — sprint scope).
6. Implement 5-report auto-hide rule inside `ReportCreatedDomainEventHandler` — when `CountByEntityAsync(entityType, entityId) >= 5`, raise `ContentHiddenDomainEvent`.

## Watchpoints

- Favorite has hard cap of 500/user — enforce in `AddFavoriteCommandHandler` using `IFavoriteRepository.CountByUserAsync` BEFORE add.
- `Report.Status` defaults `Pending`; resolution flow: Pending → Resolved/Rejected, recorded in audit log.
- All domain events ride the same UoW dispatch chain (ADR-007).
