namespace YallaJo.Web.Areas.Admin.Models.Businesses;

public sealed class BusinessPageResponse
{
    public IReadOnlyList<BusinessItemResponse> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

public sealed class BusinessItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string BusinessType { get; set; } = "";
    public string Status { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsVerified { get; set; }
    public bool IsFeatured { get; set; }
    public string? PrimaryImageUrl { get; set; }
}
