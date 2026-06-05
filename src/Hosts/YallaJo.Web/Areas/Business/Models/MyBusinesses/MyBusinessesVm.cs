using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Business.Models.MyBusinesses;

public sealed class MyBusinessesVm
{
    public IReadOnlyList<BusinessRowVm> Businesses { get; set; } = [];
    public bool HasBusinesses => Businesses.Count > 0;
}

public sealed record BusinessRowVm(
    Guid Id,
    string Name,
    string Slug,
    string BusinessType,
    string Status,
    string? City,
    string? Country,
    decimal AverageRating,
    int ReviewCount,
    bool IsVerified);

public sealed class ManageBusinessVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string BusinessType { get; set; } = "";
    public string Status { get; set; } = "";
    public string? RejectionReason { get; set; }
    public bool IsVerified { get; set; }
    public int ServiceItemCount { get; set; }
    public int StaffCount { get; set; }
    public int AmenityCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public EditBusinessFormVm Form { get; set; } = new();

    /// <summary>Resubmit allowed only when the business was rejected.</summary>
    public bool CanResubmit => string.Equals(Status, "Rejected", StringComparison.OrdinalIgnoreCase);
}

public sealed class EditBusinessFormVm
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    [Display(Name = "Business name")]
    public string Name { get; set; } = "";

    [Required]
    [Display(Name = "Place")]
    public Guid PlaceId { get; set; }

    [Range(-90, 90)]
    [Display(Name = "Latitude")]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    [Display(Name = "Longitude")]
    public decimal Longitude { get; set; }

    [StringLength(2000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(300)]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [StringLength(100)]
    [Display(Name = "City")]
    public string? City { get; set; }

    [StringLength(100)]
    [Display(Name = "Country")]
    public string? Country { get; set; }

    [StringLength(40)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(2048)]
    [Display(Name = "Website")]
    public string? Website { get; set; }

    [Display(Name = "Halal")]
    public bool IsHalal { get; set; }

    [Display(Name = "Vegetarian options")]
    public bool HasVegetarianOptions { get; set; }

    [Display(Name = "Alcohol-free area")]
    public bool HasAlcoholFreeArea { get; set; }
}
