using Analytics.Contracts.IntegrationEvents;
using Auth.Contracts.IntegrationEvents;
using Booking.Contracts.IntegrationEvents;
using ContentBlogs.Contracts.IntegrationEvents;
using ContentCore.Contracts.IntegrationEvents;
using ContentPlaces.Contracts.BusinessStaff;
using ContentPlaces.Contracts.IntegrationEvents;
using ContentSeo.Contracts.IntegrationEvents;
using ContentTours.Contracts;
using ContentTours.Contracts.IntegrationEvents;
using Finance.Contracts.IntegrationEvents;
using Messaging.Contracts.IntegrationEvents;
using Security.Contracts.IntegrationEvents;
using Social.Contracts.IntegrationEvents;

namespace YallaJo.SharedKernel.Infrastructure.Abstractions.Integration;

public static class IntegrationEventTypeRegistry
{
    private static readonly Dictionary<string, Type> NameToType = new(StringComparer.Ordinal)
    {
        // ── Security (6 events) ──
        ["security.user.created.v1"]              = typeof(UserCreatedIntegrationEvent),
        ["security.user.email-verified.v1"]       = typeof(EmailVerifiedIntegrationEvent),
        ["security.user.password-changed.v1"]     = typeof(PasswordChangedIntegrationEvent),
        ["security.user.password-reset.v1"]       = typeof(PasswordResetIntegrationEvent),
        ["security.user.phone-updated.v1"]        = typeof(PhoneNumberUpdatedIntegrationEvent),
        ["security.user.lifecycle-changed.v1"]    = typeof(UserLifecycleChangedIntegrationEvent),

        // ── Auth (2 events) ──
        ["auth.user.logged-in.v1"]                = typeof(UserLoggedInIntegrationEvent),
        ["auth.session.revoked.v1"]               = typeof(SessionRevokedIntegrationEvent),
        ["auth.user.registered.v1"]               = typeof(UserRegisteredIntegrationEvent),

        // ── ContentCore (8 events) ──
        ["content-core.language.activated.v1"]         = typeof(LanguageActivatedIntegrationEvent),
        ["content-core.language.deactivated.v1"]       = typeof(LanguageDeactivatedIntegrationEvent),
        ["content-core.attachment.uploaded.v1"]        = typeof(AttachmentUploadedIntegrationEvent),
        ["content-core.attachment.deleted.v1"]         = typeof(AttachmentDeletedIntegrationEvent),
        ["content-core.category.created.v1"]           = typeof(CategoryCreatedIntegrationEvent),
        ["content-core.category.updated.v1"]           = typeof(CategoryUpdatedIntegrationEvent),
        ["content-core.category.deleted.v1"]           = typeof(CategoryDeletedIntegrationEvent),
        ["content-core.category.restored.v1"]          = typeof(CategoryRestoredIntegrationEvent),

        // ── ContentPlaces — Places (3 events) ──
        ["content-places.place.created.v1"]              = typeof(PlaceCreatedIntegrationEvent),
        ["content-places.place.updated.v1"]              = typeof(PlaceUpdatedIntegrationEvent),
        ["content-places.place.deleted.v1"]              = typeof(PlaceDeletedIntegrationEvent),

        // ── ContentPlaces — Businesses (6 events) ──
        ["content-places.business.created.v1"]           = typeof(BusinessCreatedIntegrationEvent),
        ["content-places.business.approved.v1"]          = typeof(BusinessApprovedIntegrationEvent),
        ["content-places.business.rejected.v1"]          = typeof(BusinessRejectedIntegrationEvent),
        ["content-places.business.suspended.v1"]         = typeof(BusinessSuspendedIntegrationEvent),
        ["content-places.business.reinstated.v1"]        = typeof(BusinessReinstatedIntegrationEvent),
        ["content-places.business.resubmitted.v1"]       = typeof(BusinessResubmittedIntegrationEvent),

        // ── ContentPlaces — ServiceItems (2 events) ──
        ["content-places.service-item.created.v1"]       = typeof(ServiceItemCreatedIntegrationEvent),
        ["content-places.service-item.deleted.v1"]       = typeof(ServiceItemDeletedIntegrationEvent),

        // ── ContentPlaces — BusinessStaff (2 events) ──
        ["content-places.business-staff.added.v1"]       = typeof(BusinessStaffAddedIntegrationEvent),
        ["content-places.business-staff.removed.v1"]     = typeof(BusinessStaffRemovedIntegrationEvent),

        // ── ContentTours (14 events) ──
        ["content-tours.place.tour-count-updated.v1"]    = typeof(PlaceTourCountUpdatedIntegrationEvent),
        ["content-tours.schedule.changed.v1"]            = typeof(TourScheduleChangedIntegrationEvent),
        ["content-tours.pricing-tier.changed.v1"]        = typeof(TourPricingTierChangedIntegrationEvent),
        ["content-tours.tour.featured-changed.v1"]       = typeof(TourFeaturedChangedIntegrationEvent),
        ["content-tours.tour.deleted.v1"]                = typeof(TourDeletedIntegrationEvent),
        ["content-tours.tour.created.v1"]                = typeof(TourCreatedIntegrationEvent),
        ["content-tours.tour.updated.v1"]                = typeof(TourUpdatedIntegrationEvent),
        ["content-tours.tour.submitted.v1"]              = typeof(TourSubmittedIntegrationEvent),
        ["content-tours.tour.approved.v1"]               = typeof(TourApprovedIntegrationEvent),
        ["content-tours.tour.rejected.v1"]               = typeof(TourRejectedIntegrationEvent),
        ["content-tours.tour.suspended.v1"]              = typeof(TourSuspendedIntegrationEvent),
        ["content-tours.tour.reinstated.v1"]             = typeof(TourReinstatedIntegrationEvent),

        // ContentTours — Task 4B TourGuide assignment (2 events — Phase C)
        ["content-tours.tour-guide.assigned.v1"]         = typeof(TourGuideAssignedIntegrationEvent),
        ["content-tours.tour-guide.unassigned.v1"]       = typeof(TourGuideUnassignedIntegrationEvent),

        ["content-tours.package.created.v1"]             = typeof(TourPackageCreatedIntegrationEvent),
        ["content-tours.package.updated.v1"]             = typeof(TourPackageUpdatedIntegrationEvent),
        ["content-tours.package.deleted.v1"]             = typeof(TourPackageDeletedIntegrationEvent),

        // ── ContentBlogs (11 events) ──
        ["content-blogs.blog.created.v1"]       = typeof(BlogCreatedIntegrationEvent),
        ["content-blogs.blog.updated.v1"]       = typeof(BlogUpdatedIntegrationEvent),
        ["content-blogs.blog.deleted.v1"]       = typeof(BlogDeletedIntegrationEvent),
        ["content-blogs.blog.restored.v1"]      = typeof(BlogRestoredIntegrationEvent),
        ["content-blogs.blog.published.v1"]     = typeof(BlogPublishedIntegrationEvent),
        ["content-blogs.blog.unpublished.v1"]   = typeof(BlogUnpublishedIntegrationEvent),
        ["content-blogs.blog.archived.v1"]      = typeof(BlogArchivedIntegrationEvent),
        ["content-blogs.blog-tour.linked.v1"]   = typeof(BlogTourLinkedIntegrationEvent),
        ["content-blogs.blog-tour.unlinked.v1"] = typeof(BlogTourUnlinkedIntegrationEvent),
        ["content-blogs.blog.featured.v1"]      = typeof(BlogFeaturedIntegrationEvent),
        ["content-blogs.blog.unfeatured.v1"]    = typeof(BlogUnfeaturedIntegrationEvent),

        // ── ContentSeo (4 events) ──
        ["content-seo.faq.changed.v1"]                  = typeof(FaqItemChangedIntegrationEvent),
        ["content-seo.redirect.created.v1"]             = typeof(RedirectCreatedIntegrationEvent),
        ["content-seo.redirect.chain-flattened.v1"]     = typeof(RedirectChainFlattenedIntegrationEvent),
        ["content-seo.metadata.changed.v1"]             = typeof(SeoMetadataChangedIntegrationEvent),
        ["content-seo.weather.budget-exhausted.v1"]     = typeof(WeatherBudgetExhaustedIntegrationEvent),

        // ── Booking (15 events) ──
        ["booking.tour-booking.created.v1"]             = typeof(TourBookingCreatedIntegrationEvent),
        ["booking.tour-booking.confirmed.v1"]           = typeof(TourBookingConfirmedIntegrationEvent),
        ["booking.tour-booking.cancelled.v1"]           = typeof(TourBookingCancelledIntegrationEvent),
        ["booking.tour-booking.completed.v1"]           = typeof(TourBookingCompletedIntegrationEvent),
        ["booking.tour-booking.payment-expired.v1"]     = typeof(TourBookingPaymentExpiredIntegrationEvent),
        ["booking.tour-booking.rejected.v1"]            = typeof(TourBookingRejectedIntegrationEvent),
        ["booking.join-request.created.v1"]             = typeof(JoinRequestCreatedIntegrationEvent),
        ["booking.join-request.approved.v1"]            = typeof(JoinRequestApprovedIntegrationEvent),
        ["booking.join-request.rejected.v1"]            = typeof(JoinRequestRejectedIntegrationEvent),
        ["booking.slot-lock.created.v1"]                = typeof(SlotLockCreatedIntegrationEvent),
        ["booking.slot-lock.released.v1"]               = typeof(SlotLockReleasedIntegrationEvent),
        ["booking.availability-slot.capacity-changed.v1"] = typeof(AvailabilitySlotCapacityChangedIntegrationEvent),
        ["booking.provider-document.expiring.v1"]       = typeof(ProviderDocumentExpiringIntegrationEvent),
        ["booking.provider-document.expired.v1"]        = typeof(ProviderDocumentExpiredIntegrationEvent),
        ["booking.provider.suspended-doc-expired.v1"]   = typeof(ProviderSuspendedDocumentExpiredIntegrationEvent),

        // ── Finance (13 events) ──
        ["finance.payment.completed.v1"]                = typeof(PaymentCompletedIntegrationEvent),
        ["finance.payment.failed.v1"]                   = typeof(PaymentFailedIntegrationEvent),
        ["finance.refund.initiated.v1"]                 = typeof(RefundInitiatedIntegrationEvent),
        ["finance.refund.completed.v1"]                 = typeof(RefundCompletedIntegrationEvent),
        ["finance.refund.failed.v1"]                    = typeof(RefundFailedIntegrationEvent),
        ["finance.invoice.generated.v1"]                = typeof(InvoiceGeneratedIntegrationEvent),
        ["finance.payout.scheduled.v1"]                 = typeof(PayoutScheduledIntegrationEvent),
        ["finance.payout.completed.v1"]                 = typeof(PayoutCompletedIntegrationEvent),
        ["finance.commission-rule.upserted.v1"]         = typeof(CommissionRuleUpsertedIntegrationEvent),
        ["finance.commission-rule.deleted.v1"]          = typeof(CommissionRuleDeletedIntegrationEvent),
        ["finance.dispute.opened.v1"]                   = typeof(DisputeOpenedIntegrationEvent),
        ["finance.subscription.activated.v1"]           = typeof(SubscriptionActivatedIntegrationEvent),
        ["finance.subscription.cancelled.v1"]           = typeof(SubscriptionCancelledIntegrationEvent),

        // ── Social (5 events) ──
        ["social.review.published.v1"]                  = typeof(ReviewPublishedIntegrationEvent),
        ["social.review.deleted.v1"]                    = typeof(ReviewDeletedIntegrationEvent),
        ["social.favorite.added.v1"]                    = typeof(FavoriteAddedIntegrationEvent),
        ["social.report.resolved.v1"]                   = typeof(ReportResolvedIntegrationEvent),
        ["social.rating.recalculated.v1"]               = typeof(RatingRecalculatedIntegrationEvent),
        // -- Analytics (3 events) --
        ["analytics.popularity-scores.recalculated.v1"] = typeof(PopularityScoresRecalculatedIntegrationEvent),
        ["analytics.audit-log.entry-redacted.v1"]       = typeof(AuditLogEntryRedactedIntegrationEvent),
        ["analytics.trending.refreshed.v1"]             = typeof(TrendingRefreshedIntegrationEvent),
        

        // ── Messaging (6 events) ──────────────────────────────────────────────
        ["messaging.notification.delivered.v1"]         = typeof(NotificationDeliveredIntegrationEvent),
        ["messaging.notification.failed.v1"]            = typeof(NotificationFailedIntegrationEvent),
        ["messaging.ticket.created.v1"]                 = typeof(TicketCreatedIntegrationEvent),
        ["messaging.ticket.assigned.v1"]                = typeof(TicketAssignedIntegrationEvent),
        ["messaging.ticket.resolved.v1"]                = typeof(SupportTicketResolvedIntegrationEvent),
        ["messaging.support-sla-breached.v1"]           = typeof(SupportSlaBreachedIntegrationEvent),
    };

    private static readonly Dictionary<Type, string> TypeToName =
        NameToType
            .GroupBy(kv => kv.Value)
            .ToDictionary(g => g.Key, g => g.First().Key);

    /// <summary>Returns the stable short name for the given integration event type.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the type is not registered.</exception>
    public static string GetName(Type type)
        => TypeToName.TryGetValue(type, out var name)
            ? name
            : throw new InvalidOperationException(
                $"Integration event type '{type.FullName}' is not registered in " +
                $"{nameof(IntegrationEventTypeRegistry)}. Add it before publishing.");

    /// <summary>Tries to resolve the CLR type from a short registry name.</summary>
    public static bool TryGetType(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Type? type)
        => NameToType.TryGetValue(name, out type);

    /// <summary>All registered types — used by tests to verify completeness.</summary>
    public static IReadOnlyCollection<Type> AllRegisteredTypes => TypeToName.Keys;
}

