namespace YallaJo.Web.Areas.Admin.Models.Bookings;

public static class BookingsMapper
{
    private static readonly HashSet<string> ForceRefundableStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending",
        "Confirmed",
        "InProgress",
        "AwaitingPayment",
        "PendingConfirmation",
        "NoShow",
    };

    public static BookingsVm ToVm(
        AdminBookingsPageResponse page,
        string? statusFilter,
        string? fromDate,
        string? toDate)
    {
        return new BookingsVm
        {
            NextCursor = page.NextCursor,
            TotalCount = page.TotalCount,
            StatusFilter = statusFilter,
            FromDate = fromDate,
            ToDate = toDate,
            Bookings = page.Items.Select(b => new BookingRowVm
            {
                Id = b.Id,
                Reference = b.Reference,
                Status = b.Status,
                UserId = b.UserId,
                TourId = b.TourId,
                ProviderId = b.ProviderId,
                ParticipantCount = b.ParticipantCount,
                TotalAmount = b.TotalAmount,
                Currency = b.Currency,
                RefundAmount = b.RefundAmount,
                CreatedAt = b.CreatedAt,
                CanForceRefund = ForceRefundableStatuses.Contains(b.Status),
            }).ToList(),
        };
    }

    public static string StatusColor(string status) => status switch
    {
        "Completed" => "success",
        "Confirmed" => "info",
        "InProgress" => "info",
        "Pending" => "warning",
        "AwaitingPayment" => "warning",
        "PendingConfirmation" => "warning",
        "Cancelled" => "danger",
        "Rejected" => "danger",
        "NoShow" => "danger",
        "Refunded" => "secondary",
        _ => "secondary",
    };
}
