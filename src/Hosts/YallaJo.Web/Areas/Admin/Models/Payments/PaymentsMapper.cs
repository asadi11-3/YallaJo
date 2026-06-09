namespace YallaJo.Web.Areas.Admin.Models.Payments;

public static class PaymentsMapper
{
    public static PaymentsVm ToVm(
        AdminFinanceDashboardResponse dashboard,
        PaymentPageResponse page,
        string? statusFilter = null,
        string? typeFilter = null)
        => new()
        {
            GrossRevenue = dashboard.GrossRevenue,
            PlatformCommissionEstimate = dashboard.PlatformCommissionEstimate,
            ProviderNetEstimate = dashboard.ProviderNetEstimate,
            CompletedPaymentCount = dashboard.CompletedPaymentCount,
            OpenDisputeCount = dashboard.OpenDisputeCount,
            Currency = dashboard.Currency,
            NextCursor = page.NextCursor,
            TotalCount = page.TotalCount,
            StatusFilter = statusFilter,
            TypeFilter = typeFilter,
            Payments = page.Items
                .Select(p => new PaymentRowVm
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    ProviderId = p.ProviderId,
                    BookingId = p.BookingId,
                    Amount = p.Amount,
                    Currency = p.Currency,
                    PaymentMethod = p.PaymentMethod,
                    PaymentType = p.PaymentType,
                    Status = p.Status,
                    GatewayProvider = p.GatewayProvider,
                    GatewayTransactionId = p.GatewayTransactionId,
                    RefundedTotal = p.RefundedTotal,
                    PaidAt = p.PaidAt,
                    CreatedAt = p.CreatedAt
                })
                .ToList()
        };

    // Maps a payment status to a UI-UX token color (used for status badges).
    public static string StatusColor(string status) => status?.ToLowerInvariant() switch
    {
        "succeeded" or "completed" or "paid" or "captured" => "success",
        "pending" or "processing" or "initiated" or "authorized" => "warning",
        "failed" or "cancelled" or "canceled" or "expired" => "danger",
        "refunded" or "partiallyrefunded" => "info",
        _ => "secondary"
    };

    // A11Y5 — pairs each status with a Font Awesome icon so the badge never relies on colour alone.
    public static string StatusIcon(string status) => status?.ToLowerInvariant() switch
    {
        "succeeded" or "completed" or "paid" or "captured" => "circle-check",
        "pending" or "processing" or "initiated" or "authorized" => "clock",
        "failed" or "cancelled" or "canceled" or "expired" => "circle-xmark",
        "refunded" => "rotate-left",
        "partiallyrefunded" => "circle-half-stroke",
        _ => "circle-question"
    };
}
