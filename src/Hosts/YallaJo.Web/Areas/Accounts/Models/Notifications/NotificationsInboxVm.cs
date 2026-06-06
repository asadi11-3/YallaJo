namespace YallaJo.Web.Areas.Accounts.Models.Notifications;

/// <summary>Filter inputs for the notifications inbox (FE-1B). All optional.</summary>
public sealed class NotificationInboxFilterVm
{
    /// <summary>"all" | "unread" | "read" (defaults to "all").</summary>
    public string Status { get; init; } = "all";

    /// <summary>NotificationType enum name, or null for all types.</summary>
    public string? Type { get; init; }

    public string? FromDate { get; init; }
    public string? ToDate { get; init; }

    /// <summary>Status options surfaced in the filter dropdown.</summary>
    public static readonly IReadOnlyList<string> StatusOptions = ["all", "unread", "read"];

    /// <summary>
    /// NotificationType enum names grouped for the &lt;optgroup&gt; dropdown. Mirrors
    /// the backend Messaging.Domain.Enums.NotificationType groupings. Kept as a flat
    /// curated set; unknown values from the backend still round-trip via the query string.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> TypeGroups =
        new Dictionary<string, string[]>
        {
            ["General"]  = ["WelcomeEmail", "EmailVerification", "PasswordChanged", "ProfileUpdated", "AccountDeactivated", "AccountReactivated"],
            ["Booking"]  = ["BookingConfirmed", "BookingCancelled", "BookingCompleted", "BookingReminderUpcoming", "ProviderBookingReceived", "ProviderBookingCancelled"],
            ["Payment"]  = ["PaymentCompleted", "PaymentFailed", "RefundInitiated", "RefundCompleted", "PayoutScheduled", "PayoutCompleted", "PayoutFailed"],
            ["Reviews"]  = ["ReviewPosted", "ReviewReplied", "ReviewAutoHidden", "ReportResolved"],
            ["Support"]  = ["SupportTicketCreated", "SupportTicketResolved"],
            ["Security"] = ["OtpDelivery", "SecurityAlert", "LoginFromNewDevice", "TwoFactorEnabled"],
            ["Disputes"] = ["DisputeOpened", "BookingDisputed", "BookingDisputeResolved", "DisputeResolved", "DisputeEscalated"],
        };

    /// <summary>Maps the Status string to the backend IsRead filter (null = all).</summary>
    public bool? ToIsRead() => Status?.ToLowerInvariant() switch
    {
        "unread" => false,
        "read"   => true,
        _        => null,
    };
}

/// <summary>One row in the inbox list.</summary>
public sealed record NotificationInboxRowVm(
    Guid Id,
    string Type,
    string Title,
    string Body,
    bool IsRead,
    DateTime CreatedAt,
    string? LinkUrl);

/// <summary>Full inbox page view model.</summary>
public sealed class NotificationsInboxVm
{
    public NotificationInboxFilterVm Filter { get; init; } = new();
    public IReadOnlyList<NotificationInboxRowVm> Items { get; init; } = [];
    public Guid? NextCursor { get; init; }
    public int UnreadCount { get; init; }

    /// <summary>Non-null when the API call degraded; the view shows a banner but still renders.</summary>
    public string? LoadError { get; init; }

    public bool HasItems => Items.Count > 0;
    public bool HasNextPage => NextCursor is { } c && c != Guid.Empty;
}

/// <summary>
/// Resolves a notification's (EntityType, EntityId) to a safe, owner-scoped local route.
/// FE-1B allow-list: Booking only. All other entity types render without a link rather
/// than inventing a URL that may not exist or may not be owner-readable.
/// </summary>
public static class NotificationLinkResolver
{
    public static string? Resolve(string? entityType, Guid? entityId)
    {
        if (entityId is not { } id || id == Guid.Empty || string.IsNullOrWhiteSpace(entityType))
        {
            return null;
        }

        return entityType.Trim().ToLowerInvariant() switch
        {
            "booking"     => $"/accounts/bookings/{id}",
            "tourbooking" => $"/accounts/bookings/{id}",
            _             => null,
        };
    }
}
