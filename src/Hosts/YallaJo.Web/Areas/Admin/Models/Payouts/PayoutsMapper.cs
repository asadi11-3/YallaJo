namespace YallaJo.Web.Areas.Admin.Models.Payouts;

public static class PayoutsMapper
{
    public static PayoutsVm ToVm(PayoutPageResponse page)
    {
        return new PayoutsVm
        {
            NextCursor = page.NextCursor,
            Payouts = page.Items.Select(p => new PayoutRowVm
            {
                Id = p.Id,
                ProviderId = p.ProviderId,
                Currency = p.Currency,
                BatchPeriodStart = p.BatchPeriodStart,
                BatchPeriodEnd = p.BatchPeriodEnd,
                GrossAmount = p.GrossAmount,
                CommissionAmount = p.CommissionAmount,
                NetAmount = p.NetAmount,
                Status = p.Status,
                ApprovedAt = p.ApprovedAt,
                FailureReason = p.FailureReason,
                ItemCount = p.ItemCount,
                CreatedAt = p.CreatedAt,
                CanApprove = string.Equals(p.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(p.Status, "Hold", StringComparison.OrdinalIgnoreCase),
            }).ToList(),
        };
    }

    public static string StatusColor(string status) => status switch
    {
        "Completed" => "success",
        "ReadyForPayout" => "info",
        "Pending" => "warning",
        "Hold" => "warning",
        "Failed" => "danger",
        "ManuallyResolved" => "secondary",
        _ => "secondary",
    };

    // A11Y5: status conveyed via colour + icon + text (never colour alone).
    public static string StatusIcon(string status) => status switch
    {
        "Completed" => "circle-check",
        "ReadyForPayout" => "money-bill-transfer",
        "Pending" => "clock",
        "Hold" => "circle-pause",
        "Failed" => "circle-xmark",
        "ManuallyResolved" => "user-gear",
        _ => "circle-question",
    };
}
