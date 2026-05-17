using Analytics.Contracts.IntegrationEvents;
using Auth.Contracts.IntegrationEvents;
using Booking.Contracts.IntegrationEvents;
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

        // ── Booking (12 events) ──
        ["booking.tour-booking.created.v1"]              = typeof(TourBookingCreatedIntegrationEvent),
        ["booking.tour-booking.confirmed.v1"]            = typeof(TourBookingConfirmedIntegrationEvent),
        ["booking.tour-booking.cancelled.v1"]            = typeof(TourBookingCancelledIntegrationEvent),
        ["booking.tour-booking.completed.v1"]            = typeof(TourBookingCompletedIntegrationEvent),
        ["booking.tour-booking.rejected.v1"]             = typeof(TourBookingRejectedIntegrationEvent),
        ["booking.tour-booking.payment-expired.v1"]      = typeof(TourBookingPaymentExpiredIntegrationEvent),
        ["booking.slot-lock.created.v1"]                 = typeof(SlotLockCreatedIntegrationEvent),
        ["booking.slot-lock.released.v1"]                = typeof(SlotLockReleasedIntegrationEvent),
        ["booking.availability-slot.capacity-changed.v1"] = typeof(AvailabilitySlotCapacityChangedIntegrationEvent),
        ["booking.join-request.created.v1"]              = typeof(JoinRequestCreatedIntegrationEvent),
        ["booking.join-request.approved.v1"]             = typeof(JoinRequestApprovedIntegrationEvent),
        ["booking.join-request.rejected.v1"]             = typeof(JoinRequestRejectedIntegrationEvent),

        // ── Finance (10 events) ──
        ["finance.payment.succeeded.v1"]                 = typeof(PaymentSucceededIntegrationEvent),
        ["finance.payment.failed.v1"]                    = typeof(PaymentFailedIntegrationEvent),
        ["finance.payment.refunded.v1"]                  = typeof(PaymentRefundedIntegrationEvent),
        ["finance.payout.processed.v1"]                  = typeof(PayoutProcessedIntegrationEvent),
        ["finance.payout.failed.v1"]                     = typeof(PayoutFailedIntegrationEvent),
        ["finance.invoice.issued.v1"]                    = typeof(InvoiceIssuedIntegrationEvent),
        ["finance.invoice.paid.v1"]                      = typeof(InvoicePaidIntegrationEvent),
        ["finance.dispute.opened.v1"]                    = typeof(DisputeOpenedIntegrationEvent),
        ["finance.subscription.activated.v1"]            = typeof(SubscriptionActivatedIntegrationEvent),
        ["finance.subscription.cancelled.v1"]            = typeof(SubscriptionCancelledIntegrationEvent),

        // ── Social (5 events) ──
        ["social.review.created.v1"]                     = typeof(ReviewCreatedIntegrationEvent),
        ["social.review.deleted.v1"]                     = typeof(ReviewDeletedIntegrationEvent),
        ["social.favorite.added.v1"]                     = typeof(FavoriteAddedIntegrationEvent),
        ["social.report.created.v1"]                     = typeof(ReportCreatedIntegrationEvent),
        ["social.content.hidden.v1"]                     = typeof(ContentHiddenIntegrationEvent),

        // ── Analytics (3 events) ──
        ["analytics.popularity.refreshed.v1"]            = typeof(PopularityScoresRefreshedIntegrationEvent),
        ["analytics.recommendation-cache.expired.v1"]    = typeof(RecommendationCacheExpiredIntegrationEvent),
        ["analytics.audit.exported.v1"]                  = typeof(AnalyticsAuditExportedIntegrationEvent),

        // ── Messaging (6 events) ──
        ["messaging.notification.delivered.v1"]          = typeof(NotificationDeliveredIntegrationEvent),
        ["messaging.notification.failed.v1"]             = typeof(NotificationFailedIntegrationEvent),
        ["messaging.device-token.registered.v1"]         = typeof(DeviceTokenRegisteredIntegrationEvent),
        ["messaging.support-ticket.opened.v1"]           = typeof(SupportTicketOpenedIntegrationEvent),
        ["messaging.support-ticket.resolved.v1"]         = typeof(SupportTicketResolvedIntegrationEvent),
        ["messaging.notification-template.updated.v1"]   = typeof(NotificationTemplateUpdatedIntegrationEvent),

        // ── ContentSeo (4 events) ──
        ["content-seo.redirect.created.v1"]              = typeof(RedirectCreatedIntegrationEvent),
        ["content-seo.redirect.chain-flattened.v1"]      = typeof(RedirectChainFlattenedIntegrationEvent),
        ["content-seo.seo-metadata.changed.v1"]          = typeof(SeoMetadataChangedIntegrationEvent),
        ["content-seo.faq-item.changed.v1"]              = typeof(FaqItemChangedIntegrationEvent),
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
