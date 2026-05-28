namespace Messaging.Domain.Enums;

/// <summary>All notification types. Values 0-32 are stable; add new at the end.</summary>
public enum NotificationType : byte
{
    // ── General (0-6) ─────────────────────────────────────────────────────────
    WelcomeEmail                = 0,
    EmailVerification           = 1,   // CRITICAL
    PasswordResetRequested      = 2,
    PasswordChanged             = 3,   // CRITICAL
    ProfileUpdated              = 4,
    AccountDeactivated          = 5,
    AccountReactivated          = 6,

    // ── Booking (10-15) ───────────────────────────────────────────────────────
    BookingConfirmed            = 10,
    BookingCancelled            = 11,
    BookingCompleted            = 12,
    BookingReminderUpcoming     = 13,
    ProviderBookingReceived     = 14,
    ProviderBookingCancelled    = 15,

    // ── Payment (16-22) ───────────────────────────────────────────────────────
    PaymentCompleted            = 16,  // CRITICAL
    PaymentFailed               = 17,  // CRITICAL
    RefundInitiated             = 18,  // CRITICAL
    RefundCompleted             = 19,  // CRITICAL
    PayoutScheduled             = 20,
    PayoutCompleted             = 21,
    PayoutFailed                = 22,

    // ── Reviews & Social (23-26) ──────────────────────────────────────────────
    ReviewPosted                = 23,
    ReviewReplied               = 24,
    ReviewAutoHidden            = 25,
    ReportResolved              = 26,

    // ── Support Tickets (27-28) ───────────────────────────────────────────────
    SupportTicketCreated        = 27,
    SupportTicketResolved       = 28,

    // ── Security (29-32) ──────────────────────────────────────────────────────
    OtpDelivery                 = 29,  // CRITICAL
    SecurityAlert               = 30,  // CRITICAL
    LoginFromNewDevice          = 31,  // CRITICAL
    TwoFactorEnabled            = 32,

    // ── Business/Provider (33-37) — for ContentPlaces module events ───────────
    Business                    = 33,  // Generic business status notification (approved/rejected/suspended/reinstated)
    ProviderApproved            = 34,
    ProviderRejected            = 35,
    ProviderSuspended           = 36,
    ProviderReinstated          = 37,

    // ── TourGuide (40-49) ────────────────────────────────────────────────────
    GuideApplicationApproved    = 40,
    GuideApplicationRejected    = 41,
    TourProposalApproved        = 42,
    TourProposalRejected        = 43,
    NewGuideApplication         = 44,
    NewJoinRequest              = 45,
    JoinRequestApproved         = 46,
    JoinRequestRejected         = 47,
    AgencyAffiliationCreated    = 48,
    AgencyAffiliationApproved   = 49,
    GuideOfferingSuspended      = 50,
    GuideApplicationSubmitted   = 51,
}

/// <summary>Extension methods for NotificationType.</summary>
public static class NotificationTypeExtensions
{
    private static readonly HashSet<NotificationType> CriticalTypes =
    [
        NotificationType.EmailVerification,
        NotificationType.PasswordChanged,
        NotificationType.PaymentCompleted,
        NotificationType.PaymentFailed,
        NotificationType.RefundInitiated,
        NotificationType.RefundCompleted,
        NotificationType.OtpDelivery,
        NotificationType.SecurityAlert,
        NotificationType.LoginFromNewDevice,
    ];

    /// <summary>Critical notifications cannot be disabled by the user and are never auto-purged.</summary>
    public static bool IsCritical(this NotificationType type) => CriticalTypes.Contains(type);
}
