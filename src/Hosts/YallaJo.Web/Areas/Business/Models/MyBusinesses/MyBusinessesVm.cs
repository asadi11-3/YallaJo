using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace YallaJo.Web.Areas.Business.Models.MyBusinesses;

public sealed class MyBusinessesVm
{
    public IReadOnlyList<BusinessRowVm> Businesses { get; set; } = [];
    public bool HasBusinesses => Businesses.Count > 0;

    // D-15/D1: simple page-shape paging (no TotalPages). HasNext is a heuristic
    // because the API returns a plain list with no total count.
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; }
    public bool HasPrev { get; set; }
    public bool HasNext { get; set; }
}

/// <summary>
/// Page model for the "Register a business" form. Carries the bound form plus
/// the option lists for the BusinessType and Place selectors. Both lists are
/// rebuilt by the controller on every GET/redisplay so they survive validation
/// round-trips.
/// </summary>
public sealed class RegisterBusinessVm
{
    public RegisterBusinessFormVm Form { get; set; } = new();
    public IReadOnlyList<SelectListItem> BusinessTypes { get; set; } = [];
    public IReadOnlyList<SelectListItem> Places { get; set; } = [];

    /// <summary>
    /// Coordinates per place id (as the option value string) so the form can
    /// pre-fill latitude/longitude when a place is chosen.
    /// </summary>
    public IReadOnlyDictionary<string, PlaceCoord> PlaceCoordinates { get; set; }
        = new Dictionary<string, PlaceCoord>();

    /// <summary>True when at least one place exists to attach the business to.</summary>
    public bool HasPlaces => Places.Count > 0;
}

public readonly record struct PlaceCoord(decimal Lat, decimal Lng);

public sealed class RegisterBusinessFormVm
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    [Display(Name = "Business name")]
    public string Name { get; set; } = "";

    [Required]
    [Display(Name = "Business type")]
    public string BusinessType { get; set; } = "";

    [Required(ErrorMessage = "Choose the place this business belongs to.")]
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

    [StringLength(20)]
    [Display(Name = "Postal code")]
    public string? PostalCode { get; set; }

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

    [StringLength(100)]
    [Display(Name = "License number")]
    public string? LicenseNumber { get; set; }

    [StringLength(100)]
    [Display(Name = "Tax ID")]
    public string? TaxId { get; set; }

    [Display(Name = "Halal")]
    public bool IsHalal { get; set; }

    [Display(Name = "Vegetarian options")]
    public bool HasVegetarianOptions { get; set; }

    [Display(Name = "Alcohol-free area")]
    public bool HasAlcoholFreeArea { get; set; }
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
