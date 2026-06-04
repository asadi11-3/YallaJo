namespace YallaJo.Web.Areas.Admin.Models.FlaggedReviews;

public sealed class FlaggedReviewsFilterRequest
{
    public Guid? AfterCursor { get; set; }
    public int PageSize { get; set; } = 20;
}
