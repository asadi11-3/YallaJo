namespace YallaJo.Web.Areas.Admin.Models.Businesses;

public sealed class BusinessesFilterRequest
{
    public Guid? PlaceId { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
}
