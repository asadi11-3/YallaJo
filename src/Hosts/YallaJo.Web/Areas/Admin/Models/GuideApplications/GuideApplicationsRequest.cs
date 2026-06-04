namespace YallaJo.Web.Areas.Admin.Models.GuideApplications;

public sealed class GuideApplicationsFilterRequest
{
    public Guid? TourId { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
}
