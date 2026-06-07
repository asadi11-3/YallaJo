using Accounts.Contracts.IntegrationEvents;
using Analytics.Contracts.IntegrationEvents;
using Auth.Contracts.IntegrationEvents;
using Booking.Contracts.IntegrationEvents;
using ContentBlogs.Contracts.IntegrationEvents;
using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentCore.Contracts.IntegrationEvents;
using ContentPlaces.Contracts.BusinessStaff;
using ContentPlaces.Contracts.IntegrationEvents;
using ContentSeo.Contracts.IntegrationEvents;
using ContentTours.Contracts;
using ContentTours.Contracts.IntegrationEvents;
using Finance.Contracts.IntegrationEvents;
using FluentAssertions;
using Messaging.Contracts.IntegrationEvents;
using Security.Contracts.IntegrationEvents;
using Social.Contracts.IntegrationEvents;
using Tracking.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Infrastructure.Abstractions.Integration;

namespace SharedKernel.Tests.Unit;

/// <summary>
/// Verifies <see cref="IntegrationEventTypeRegistry"/> correctness and completeness.
///
/// The expected count is derived from <see cref="KnownMappings"/> (not hardcoded) so
/// adding a new integration event in any module simply means appending one row here —
/// no scattered count update is required. Two completeness assertions run together:
///
///   1. Every <see cref="KnownMappings"/> entry must be present in the registry.
///   2. Every type in <see cref="IntegrationEventTypeRegistry.AllRegisteredTypes"/>
///      must also be in <see cref="KnownMappings"/>. This catches the reverse drift —
///      a production event added to the registry without a corresponding test row.
/// </summary>
public sealed class IntegrationEventTypeRegistryTests
{
    // ── All registered integration events ─────────────────────────────────────
    //
    // When you add a new integration event:
    //   1) register it in IntegrationEventTypeRegistry.cs (production)
    //   2) append one row here
    // No count constants need updating — both assertions below derive from this list.

    private static readonly (string Key, Type Type)[] KnownMappings =
    [
        // Security (6)
        ("security.user.created.v1",                      typeof(UserCreatedIntegrationEvent)),
        ("security.user.email-verified.v1",               typeof(EmailVerifiedIntegrationEvent)),
        ("security.user.password-changed.v1",             typeof(PasswordChangedIntegrationEvent)),
        ("security.user.password-reset.v1",               typeof(PasswordResetIntegrationEvent)),
        ("security.user.phone-updated.v1",                typeof(PhoneNumberUpdatedIntegrationEvent)),
        ("security.user.lifecycle-changed.v1",            typeof(UserLifecycleChangedIntegrationEvent)),

        // Auth (4)
        ("auth.user.logged-in.v1",                        typeof(UserLoggedInIntegrationEvent)),
        ("auth.session.revoked.v1",                       typeof(SessionRevokedIntegrationEvent)),
        ("auth.user.registered.v1",                       typeof(UserRegisteredIntegrationEvent)),
        ("auth.password-reset.token-issued.v1",           typeof(PasswordResetTokenIssuedIntegrationEvent)),

        // ContentCore (12)
        ("content-core.language.activated.v1",            typeof(LanguageActivatedIntegrationEvent)),
        ("content-core.language.deactivated.v1",          typeof(LanguageDeactivatedIntegrationEvent)),
        ("content-core.attachment.uploaded.v1",           typeof(AttachmentUploadedIntegrationEvent)),
        ("content-core.attachment.deleted.v1",            typeof(AttachmentDeletedIntegrationEvent)),
        ("content-core.category.created.v1",              typeof(CategoryCreatedIntegrationEvent)),
        ("content-core.category.updated.v1",              typeof(CategoryUpdatedIntegrationEvent)),
        ("content-core.category.deleted.v1",              typeof(CategoryDeletedIntegrationEvent)),
        ("content-core.category.restored.v1",             typeof(CategoryRestoredIntegrationEvent)),
        ("content-core.entity-category.assigned.v1",      typeof(EntityCategoryAssignedIntegrationEvent)),
        ("content-core.entity-category.removed.v1",       typeof(EntityCategoryRemovedIntegrationEvent)),
        ("content-core.entity-tag.assigned.v1",           typeof(EntityTagAssignedIntegrationEvent)),
        ("content-core.entity-tag.removed.v1",            typeof(EntityTagRemovedIntegrationEvent)),

        // ContentPlaces — Places (3)
        ("content-places.place.created.v1",               typeof(PlaceCreatedIntegrationEvent)),
        ("content-places.place.updated.v1",               typeof(PlaceUpdatedIntegrationEvent)),
        ("content-places.place.deleted.v1",               typeof(PlaceDeletedIntegrationEvent)),

        // ContentPlaces — Businesses (9)
        ("content-places.business.created.v1",            typeof(BusinessCreatedIntegrationEvent)),
        ("content-places.business.updated.v1",            typeof(BusinessUpdatedIntegrationEvent)),
        ("content-places.business.deleted.v1",            typeof(BusinessDeletedIntegrationEvent)),
        ("content-places.business.approved.v1",           typeof(BusinessApprovedIntegrationEvent)),
        ("content-places.business.rejected.v1",           typeof(BusinessRejectedIntegrationEvent)),
        ("content-places.business.suspended.v1",          typeof(BusinessSuspendedIntegrationEvent)),
        ("content-places.business.reinstated.v1",         typeof(BusinessReinstatedIntegrationEvent)),
        ("content-places.business.resubmitted.v1",        typeof(BusinessResubmittedIntegrationEvent)),
        ("content-places.business.more-docs-requested.v1", typeof(BusinessMoreDocsRequestedIntegrationEvent)),

        // ContentPlaces — ServiceItems (2)
        ("content-places.service-item.created.v1",        typeof(ServiceItemCreatedIntegrationEvent)),
        ("content-places.service-item.deleted.v1",        typeof(ServiceItemDeletedIntegrationEvent)),

        // ContentPlaces — BusinessStaff (2)
        ("content-places.business-staff.added.v1",        typeof(BusinessStaffAddedIntegrationEvent)),
        ("content-places.business-staff.removed.v1",      typeof(BusinessStaffRemovedIntegrationEvent)),

        // ContentTours — pre-Task-1 (4)
        ("content-tours.place.tour-count-updated.v1",     typeof(PlaceTourCountUpdatedIntegrationEvent)),
        ("content-tours.schedule.changed.v1",             typeof(TourScheduleChangedIntegrationEvent)),
        ("content-tours.pricing-tier.changed.v1",         typeof(TourPricingTierChangedIntegrationEvent)),
        ("content-tours.tour.featured-changed.v1",        typeof(TourFeaturedChangedIntegrationEvent)),

        // ContentTours — Task-1 lifecycle (8)
        ("content-tours.tour.deleted.v1",                 typeof(TourDeletedIntegrationEvent)),
        ("content-tours.tour.created.v1",                 typeof(TourCreatedIntegrationEvent)),
        ("content-tours.tour.updated.v1",                 typeof(TourUpdatedIntegrationEvent)),
        ("content-tours.tour.submitted.v1",               typeof(TourSubmittedIntegrationEvent)),
        ("content-tours.tour.approved.v1",                typeof(TourApprovedIntegrationEvent)),
        ("content-tours.tour.rejected.v1",                typeof(TourRejectedIntegrationEvent)),
        ("content-tours.tour.suspended.v1",               typeof(TourSuspendedIntegrationEvent)),
        ("content-tours.tour.reinstated.v1",              typeof(TourReinstatedIntegrationEvent)),

        // ContentTours — TourGuide assignment / lifecycle (8)
        ("content-tours.tour-guide.registered.v1",        typeof(TourGuideRegisteredIntegrationEvent)),
        ("content-tours.tour-guide.assigned.v1",          typeof(TourGuideAssignedIntegrationEvent)),
        ("content-tours.tour-guide.unassigned.v1",        typeof(TourGuideUnassignedIntegrationEvent)),
        ("content-tours.tour-guide.suspended.v1",         typeof(TourGuideSuspendedIntegrationEvent)),
        ("content-tours.tour-guide.activated.v1",         typeof(TourGuideActivatedIntegrationEvent)),
        ("content-tours.tour-guide.deactivated.v1",       typeof(TourGuideDeactivatedIntegrationEvent)),
        ("content-tours.tour-guide.profile-updated.v1",   typeof(TourGuideProfileUpdatedIntegrationEvent)),
        ("content-tours.guide-offering.suspended.v1",     typeof(GuideTourOfferingSuspendedIntegrationEvent)),

        // ContentTours — Guide applications (3)
        ("content-tours.guide-application.created.v1",    typeof(NewGuideApplicationIntegrationEvent)),
        ("content-tours.guide-application.approved.v1",   typeof(GuideApplicationApprovedIntegrationEvent)),
        ("content-tours.guide-application.rejected.v1",   typeof(GuideApplicationRejectedIntegrationEvent)),

        // ContentTours — Tour proposals (3)
        ("content-tours.tour-proposal.submitted.v1",      typeof(TourProposalSubmittedIntegrationEvent)),
        ("content-tours.tour-proposal.approved.v1",       typeof(TourProposalApprovedIntegrationEvent)),
        ("content-tours.tour-proposal.rejected.v1",       typeof(TourProposalRejectedIntegrationEvent)),

        // ContentTours — TourPackage (3)
        ("content-tours.package.created.v1",              typeof(TourPackageCreatedIntegrationEvent)),
        ("content-tours.package.updated.v1",              typeof(TourPackageUpdatedIntegrationEvent)),
        ("content-tours.package.deleted.v1",              typeof(TourPackageDeletedIntegrationEvent)),

        // ContentBlogs (14)
        ("content-blogs.blog.created.v1",                 typeof(BlogCreatedIntegrationEvent)),
        ("content-blogs.blog.updated.v1",                 typeof(BlogUpdatedIntegrationEvent)),
        ("content-blogs.blog.deleted.v1",                 typeof(BlogDeletedIntegrationEvent)),
        ("content-blogs.blog.restored.v1",                typeof(BlogRestoredIntegrationEvent)),
        ("content-blogs.blog.published.v1",               typeof(BlogPublishedIntegrationEvent)),
        ("content-blogs.blog.unpublished.v1",             typeof(BlogUnpublishedIntegrationEvent)),
        ("content-blogs.blog.archived.v1",                typeof(BlogArchivedIntegrationEvent)),
        ("content-blogs.blog-tour.linked.v1",             typeof(BlogTourLinkedIntegrationEvent)),
        ("content-blogs.blog-tour.unlinked.v1",           typeof(BlogTourUnlinkedIntegrationEvent)),
        ("content-blogs.blog.featured.v1",                typeof(BlogFeaturedIntegrationEvent)),
        ("content-blogs.blog.unfeatured.v1",              typeof(BlogUnfeaturedIntegrationEvent)),
        ("content-blogs.blog.submitted-for-review.v1",    typeof(BlogSubmittedForReviewIntegrationEvent)),
        ("content-blogs.blog.rejected.v1",                typeof(BlogRejectedIntegrationEvent)),
        ("content-blogs.blog.removed.v1",                 typeof(BlogRemovedIntegrationEvent)),

        // Creators (12)
        ("creators.application.submitted.v1",             typeof(CreatorApplicationSubmittedIntegrationEvent)),
        ("creators.application.approved.v1",              typeof(CreatorApplicationApprovedIntegrationEvent)),
        ("creators.application.rejected.v1",              typeof(CreatorApplicationRejectedIntegrationEvent)),
        ("creators.application.more-info-requested.v1",   typeof(CreatorApplicationMoreInfoRequestedIntegrationEvent)),
        ("creators.profile.suspended.v1",                 typeof(CreatorProfileSuspendedIntegrationEvent)),
        ("creators.profile.reinstated.v1",                typeof(CreatorProfileReinstatedIntegrationEvent)),
        ("creators.profile.activated.v1",                 typeof(CreatorProfileActivatedIntegrationEvent)),
        ("creators.profile.deactivated.v1",               typeof(CreatorProfileDeactivatedIntegrationEvent)),
        ("creators.profile.updated.v1",                   typeof(CreatorProfileUpdatedIntegrationEvent)),
        ("creators.invitation.sent.v1",                   typeof(CreatorInvitationSentIntegrationEvent)),
        ("creators.invitation.redeemed.v1",               typeof(CreatorInvitationRedeemedIntegrationEvent)),
        ("creators.follow.added.v1",                      typeof(CreatorFollowAddedIntegrationEvent)),

        // Creator Tiers (3)
        ("creators.tier.promoted.v1",                     typeof(CreatorTierPromotedIntegrationEvent)),
        ("creators.tier.demoted.v1",                      typeof(CreatorTierDemotedIntegrationEvent)),
        ("creators.eligible-for-tier-promotion.v1",       typeof(CreatorEligibleForTierPromotionIntegrationEvent)),

        // ContentSeo (5)
        ("content-seo.faq.changed.v1",                    typeof(FaqItemChangedIntegrationEvent)),
        ("content-seo.redirect.created.v1",               typeof(RedirectCreatedIntegrationEvent)),
        ("content-seo.redirect.chain-flattened.v1",       typeof(RedirectChainFlattenedIntegrationEvent)),
        ("content-seo.metadata.changed.v1",               typeof(SeoMetadataChangedIntegrationEvent)),
        ("content-seo.weather.budget-exhausted.v1",       typeof(WeatherBudgetExhaustedIntegrationEvent)),

        // Booking (18)
        ("booking.tour-booking.created.v1",               typeof(TourBookingCreatedIntegrationEvent)),
        ("booking.tour-booking.confirmed.v1",             typeof(TourBookingConfirmedIntegrationEvent)),
        ("booking.tour-booking.cancelled.v1",             typeof(TourBookingCancelledIntegrationEvent)),
        ("booking.tour-booking.completed.v1",             typeof(TourBookingCompletedIntegrationEvent)),
        ("booking.tour-booking.payment-expired.v1",       typeof(TourBookingPaymentExpiredIntegrationEvent)),
        ("booking.tour-booking.rejected.v1",              typeof(TourBookingRejectedIntegrationEvent)),
        ("booking.tour-booking.disputed.v1",              typeof(TourBookingDisputedIntegrationEvent)),
        ("booking.tour-booking.dispute-resolved.v1",      typeof(TourBookingDisputeResolvedIntegrationEvent)),
        ("booking.join-request.created.v1",               typeof(JoinRequestCreatedIntegrationEvent)),
        ("booking.join-request.approved.v1",              typeof(JoinRequestApprovedIntegrationEvent)),
        ("booking.join-request.rejected.v1",              typeof(JoinRequestRejectedIntegrationEvent)),
        ("booking.slot-lock.created.v1",                  typeof(SlotLockCreatedIntegrationEvent)),
        ("booking.slot-lock.released.v1",                 typeof(SlotLockReleasedIntegrationEvent)),
        ("booking.availability-slot.capacity-changed.v1", typeof(AvailabilitySlotCapacityChangedIntegrationEvent)),
        ("booking.reminder.v1",                           typeof(BookingReminderIntegrationEvent)),
        ("booking.provider-document.expiring.v1",         typeof(Booking.Contracts.IntegrationEvents.ProviderDocumentExpiringIntegrationEvent)),
        ("booking.provider-document.expired.v1",          typeof(Booking.Contracts.IntegrationEvents.ProviderDocumentExpiredIntegrationEvent)),
        ("booking.provider.suspended-doc-expired.v1",     typeof(ProviderSuspendedDocumentExpiredIntegrationEvent)),

        // Finance (11)
        ("finance.payment.completed.v1",                  typeof(PaymentCompletedIntegrationEvent)),
        ("finance.payment.failed.v1",                     typeof(PaymentFailedIntegrationEvent)),
        ("finance.refund.initiated.v1",                   typeof(RefundInitiatedIntegrationEvent)),
        ("finance.refund.completed.v1",                   typeof(RefundCompletedIntegrationEvent)),
        ("finance.refund.failed.v1",                      typeof(RefundFailedIntegrationEvent)),
        ("finance.invoice.generated.v1",                  typeof(InvoiceGeneratedIntegrationEvent)),
        ("finance.payout.scheduled.v1",                   typeof(PayoutScheduledIntegrationEvent)),
        ("finance.payout.completed.v1",                   typeof(PayoutCompletedIntegrationEvent)),
        ("finance.commission-rule.upserted.v1",           typeof(CommissionRuleUpsertedIntegrationEvent)),
        ("finance.commission-rule.deleted.v1",            typeof(CommissionRuleDeletedIntegrationEvent)),
        ("finance.dispute.opened.v1",                     typeof(DisputeOpenedIntegrationEvent)),

        // Social (7)
        ("social.review.published.v1",                    typeof(ReviewPublishedIntegrationEvent)),
        ("social.review.deleted.v1",                      typeof(ReviewDeletedIntegrationEvent)),
        ("social.favorite.added.v1",                      typeof(FavoriteAddedIntegrationEvent)),
        ("social.report.submitted.v1",                    typeof(ReportSubmittedIntegrationEvent)),
        ("social.report.resolved.v1",                     typeof(ReportResolvedIntegrationEvent)),
        ("social.rating.recalculated.v1",                 typeof(RatingRecalculatedIntegrationEvent)),
        ("social.review-aggregate.updated.v1",            typeof(ReviewAggregateUpdatedIntegrationEvent)),

        // Analytics (3)
        ("analytics.popularity-scores.recalculated.v1",   typeof(PopularityScoresRecalculatedIntegrationEvent)),
        ("analytics.audit-log.entry-redacted.v1",         typeof(AuditLogEntryRedactedIntegrationEvent)),
        ("analytics.trending.refreshed.v1",               typeof(TrendingRefreshedIntegrationEvent)),

        // Accounts — Provider (6)
        ("accounts.provider.registered.v1",               typeof(ProviderRegisteredIntegrationEvent)),
        ("accounts.provider.approved.v1",                 typeof(ProviderApprovedIntegrationEvent)),
        ("accounts.provider.rejected.v1",                 typeof(ProviderRejectedIntegrationEvent)),
        ("accounts.provider.suspended.v1",                typeof(ProviderSuspendedIntegrationEvent)),
        ("accounts.provider.reinstated.v1",               typeof(ProviderReinstatedIntegrationEvent)),
        ("accounts.provider.status-changed.v1",           typeof(ProviderStatusChangedIntegrationEvent)),

        // Accounts — Agency (3)
        ("accounts.agency.guide-affiliated.v1",           typeof(AgencyGuideAffiliatedIntegrationEvent)),
        ("accounts.agency.affiliation-created.v1",        typeof(AgencyAffiliationCreatedIntegrationEvent)),
        ("accounts.agency.affiliation-terminated.v1",     typeof(AgencyAffiliationTerminatedIntegrationEvent)),

        // Messaging (6)
        ("messaging.notification.delivered.v1",           typeof(NotificationDeliveredIntegrationEvent)),
        ("messaging.notification.failed.v1",              typeof(NotificationFailedIntegrationEvent)),
        ("messaging.ticket.created.v1",                   typeof(TicketCreatedIntegrationEvent)),
        ("messaging.ticket.assigned.v1",                  typeof(TicketAssignedIntegrationEvent)),
        ("messaging.ticket.resolved.v1",                  typeof(SupportTicketResolvedIntegrationEvent)),
        ("messaging.support-sla-breached.v1",             typeof(SupportSlaBreachedIntegrationEvent)),

        // Tracking (2)
        ("tracking.live-session.started.v1",              typeof(LiveTrackingSessionStartedIntegrationEvent)),
        ("tracking.live-session.ended.v1",                typeof(LiveTrackingSessionEndedIntegrationEvent)),
    ];

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void AllRegisteredTypes_ContainsEveryKnownMapping()
    {
        // Forward direction: every (key, type) the test knows about must be registered.
        var registered = IntegrationEventTypeRegistry.AllRegisteredTypes;

        foreach (var (_, type) in KnownMappings)
        {
            registered.Should().Contain(type, $"{type.Name} must be in AllRegisteredTypes");
        }
    }

    [Fact]
    public void AllRegisteredTypes_HasNoUnknownEntriesBeyondKnownMappings()
    {
        // Reverse direction: every registered type must also appear in KnownMappings.
        // If a developer adds a new event to the production registry, this assertion fails
        // until the corresponding test row is appended above.
        var registered = IntegrationEventTypeRegistry.AllRegisteredTypes;
        var known = KnownMappings.Select(m => m.Type).ToHashSet();

        registered.Should().OnlyContain(
            t => known.Contains(t),
            "every registered integration event must have a matching row in KnownMappings " +
            "(append the new (key, type) pair when you register a new event in production)");
    }

    [Fact]
    public void AllRegisteredTypes_CountMatchesKnownMappings()
    {
        // Count is derived (not hardcoded) so adding events does not require touching
        // a magic number. Together with the two checks above this pins exact parity.
        var registered = IntegrationEventTypeRegistry.AllRegisteredTypes;

        registered.Should().HaveCount(
            KnownMappings.Length,
            "registry size must equal the number of KnownMappings rows");
    }

    [Theory]
    [MemberData(nameof(GetKnownMappings))]
    public void GetName_ReturnsCorrectShortKey(string expectedKey, Type type)
    {
        var name = IntegrationEventTypeRegistry.GetName(type);
        name.Should().Be(expectedKey);
    }

    [Theory]
    [MemberData(nameof(GetKnownMappings))]
    public void TryGetType_ReturnsCorrectType(string key, Type expectedType)
    {
        var found = IntegrationEventTypeRegistry.TryGetType(key, out var type);

        found.Should().BeTrue();
        type.Should().Be(expectedType);
    }

    [Fact]
    public void GetName_UnregisteredType_Throws()
    {
        var act = () => IntegrationEventTypeRegistry.GetName(typeof(object));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not registered*");
    }

    [Fact]
    public void TryGetType_UnknownKey_ReturnsFalse()
    {
        var found = IntegrationEventTypeRegistry.TryGetType("nonexistent.event.v1", out var type);

        found.Should().BeFalse();
        type.Should().BeNull();
    }

    // ── MemberData helper ────────────────────────────────────────────────────

    public static IEnumerable<object[]> GetKnownMappings()
        => KnownMappings.Select(m => new object[] { m.Key, m.Type });
}
