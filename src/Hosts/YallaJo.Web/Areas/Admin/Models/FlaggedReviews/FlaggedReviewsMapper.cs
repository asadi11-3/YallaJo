namespace YallaJo.Web.Areas.Admin.Models.FlaggedReviews;

public static class FlaggedReviewsMapper
{
    public static FlaggedReviewsVm ToVm(ReviewPageResponse page, IReadOnlyDictionary<Guid, string>? reviewerEmails = null)
    {
        return new FlaggedReviewsVm
        {
            NextCursor = page.NextCursor,
            Reviews = page.Items.Select(r => new FlaggedReviewRowVm
            {
                Id = r.Id,
                UserId = r.UserId,
                ReviewerEmail = reviewerEmails is not null && reviewerEmails.TryGetValue(r.UserId, out var email) ? email : null,
                TargetType = r.TargetType,
                TargetId = r.TargetId,
                Rating = r.Rating,
                Title = r.Title,
                Content = r.Content,
                Status = r.Status,
                IsVerifiedBooking = r.IsVerifiedBooking,
                ProfanityFlagged = r.ProfanityFlagged,
                CurrentReportCount = r.CurrentReportCount,
                CreatedAt = r.CreatedAt,
                RowVersion = r.RowVersion,
            }).ToList(),
        };
    }

    public static string StatusColor(string status) => status switch
    {
        _ when string.Equals(status, "Published", StringComparison.OrdinalIgnoreCase) => "success",
        _ when string.Equals(status, "Flagged", StringComparison.OrdinalIgnoreCase) => "warning",
        _ when string.Equals(status, "AutoHidden", StringComparison.OrdinalIgnoreCase) => "info",
        _ when string.Equals(status, "Removed", StringComparison.OrdinalIgnoreCase) => "danger",
        _ => "secondary",
    };

    public static string StatusIcon(string status) => status switch
    {
        _ when string.Equals(status, "Published", StringComparison.OrdinalIgnoreCase) => "circle-check",
        _ when string.Equals(status, "Flagged", StringComparison.OrdinalIgnoreCase) => "flag",
        _ when string.Equals(status, "AutoHidden", StringComparison.OrdinalIgnoreCase) => "eye-slash",
        _ when string.Equals(status, "Removed", StringComparison.OrdinalIgnoreCase) => "circle-xmark",
        _ => "circle-info",
    };
}
