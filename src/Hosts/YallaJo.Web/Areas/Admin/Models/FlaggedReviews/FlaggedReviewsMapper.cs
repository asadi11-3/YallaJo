namespace YallaJo.Web.Areas.Admin.Models.FlaggedReviews;

public static class FlaggedReviewsMapper
{
    public static FlaggedReviewsVm ToVm(ReviewPageResponse page)
    {
        return new FlaggedReviewsVm
        {
            NextCursor = page.NextCursor,
            Reviews = page.Items.Select(r => new FlaggedReviewRowVm
            {
                Id = r.Id,
                UserId = r.UserId,
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
}
