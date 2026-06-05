namespace YallaJo.Web.Areas.Business.Models.Amenities;

public sealed class BusinessAmenityItemResponse
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = "";
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
}

public sealed record AddBusinessAmenityApiRequest(string Name, string? Icon, int SortOrder);
