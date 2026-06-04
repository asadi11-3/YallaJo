namespace YallaJo.Web.Areas.Admin.Models.Businesses;

public sealed class BusinessesVm
{
    public Guid? PlaceId { get; set; }
    public string? StatusFilter { get; set; }
    public IReadOnlyList<BusinessRowVm> Businesses { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}

public sealed class BusinessRowVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string BusinessType { get; set; } = "";
    public string Status { get; set; } = "";
    public string? City { get; set; }
    public string? Country { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsVerified { get; set; }
    public bool IsFeatured { get; set; }
}
