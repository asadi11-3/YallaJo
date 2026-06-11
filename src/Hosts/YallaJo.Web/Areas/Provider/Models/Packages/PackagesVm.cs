using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Provider.Models.Packages;

public sealed class PackagesIndexVm
{
    public IReadOnlyList<PackageRowVm> Packages { get; init; } = [];
    public CreatePackageFormVm Create { get; set; } = new();

    /// <summary>F10: provider's tours for the included-tours multi-select (replaces GUID-paste textarea). Empty = no options yet.</summary>
    public IReadOnlyList<PackageTourOptionVm> TourOptions { get; set; } = [];

    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public bool HasPrevious { get; init; }
    public bool HasNext { get; init; }

    public bool HasPackages => Packages.Count > 0;
}

/// <summary>F10 option row for the included-tours picker.</summary>
public sealed class PackageTourOptionVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
}

public sealed class PackageRowVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public decimal PriceAmount { get; init; }
    public string Currency { get; init; } = "";
    public int IncludedTourCount { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public DateTime CreatedAt { get; init; }

    public string PriceLabel => $"{PriceAmount:N3} {Currency}";
    public string ValidityLabel =>
        ValidFrom is null && ValidTo is null ? "—"
        : $"{ValidFrom?.ToString("d MMM yyyy") ?? "…"} – {ValidTo?.ToString("d MMM yyyy") ?? "…"}";
}

public sealed class CreatePackageFormVm
{
    [Required(ErrorMessage = "Package name is required.")]
    [StringLength(200, MinimumLength = 1)]
    [Display(Name = "Package name")]
    public string Name { get; set; } = "";

    [StringLength(2000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Range(0, 1_000_000)]
    [Display(Name = "Price")]
    public decimal Price { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    [Display(Name = "Currency")]
    public string Currency { get; set; } = "JOD";

    [Range(1, 100_000)]
    [Display(Name = "Max participants (optional)")]
    public int? MaxParticipants { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Valid from (optional)")]
    public DateTime? ValidFrom { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Valid to (optional)")]
    public DateTime? ValidTo { get; set; }

    // F10: bound from the SSR <select multiple> of the provider's own tours (no GUID paste).
    [Display(Name = "Included tours")]
    public List<Guid> IncludedTourIds { get; set; } = [];

    [Display(Name = "Inclusions (one per line)")]
    public string? Inclusions { get; set; }
}

public sealed class PackageManageVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public decimal PriceAmount { get; init; }
    public string Currency { get; init; } = "";
    public int? MaxParticipants { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<PackageIncludedTourResponse> IncludedTours { get; init; } = [];
    public IReadOnlyList<PackageInclusionResponse> Inclusions { get; init; } = [];

    public AddInclusionFormVm AddInclusion { get; set; } = new();

    public string PriceLabel => $"{PriceAmount:N3} {Currency}";
}

public sealed class AddInclusionFormVm
{
    [Required(ErrorMessage = "Enter an inclusion description.")]
    [StringLength(500, MinimumLength = 1)]
    [Display(Name = "Inclusion")]
    public string Description { get; set; } = "";
}
