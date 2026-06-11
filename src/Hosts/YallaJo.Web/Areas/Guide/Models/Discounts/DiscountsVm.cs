using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Guide.Models.Discounts;

public sealed class DiscountsVm
{
    public IReadOnlyList<DiscountRowVm> Discounts { get; init; } = [];

    public CreateDiscountFormVm Form { get; set; } = new();

    /// <summary>When an edit POST fails validation, holds the user's submitted values so the row re-renders with input preserved (no data loss).</summary>
    public EditDiscountFormVm? EditForm { get; set; }

    /// <summary>Id of the discount whose edit row should render expanded (set on failed edit re-render).</summary>
    public Guid? OpenEditId { get; set; }

    /// <summary>F10: the guide's own tours, feeding the create-form tour picker (best-effort; may be empty).</summary>
    public IReadOnlyList<TourOptionVm> TourOptions { get; init; } = [];

    public bool HasDiscounts => Discounts.Count > 0;
}

/// <summary>F10 picker option — the guide's own tour shown by name, submitting the id.</summary>
public sealed record TourOptionVm(Guid TourId, string Title);

public sealed record DiscountRowVm(
    Guid Id,
    string Name,
    string? Description,
    GuideDiscountType DiscountType,
    decimal DiscountValue,
    string Currency,
    DateTime ValidFrom,
    DateTime? ValidUntil,
    int? MaxUsageCount,
    int CurrentUsageCount,
    bool IsActive);

public sealed class CreateDiscountFormVm
{
    [Display(Name = "Tour (optional)")]
    public Guid? TourId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    [Display(Name = "Discount name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Discount type")]
    public GuideDiscountType DiscountType { get; set; } = GuideDiscountType.Percentage;

    [Range(0, 1_000_000)]
    [Display(Name = "Discount value")]
    public decimal DiscountValue { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    [Display(Name = "Currency")]
    public string Currency { get; set; } = "JOD";

    [DataType(DataType.Date)]
    [Display(Name = "Valid from")]
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow.Date;

    [DataType(DataType.Date)]
    [Display(Name = "Valid until")]
    public DateTime? ValidUntil { get; set; }

    [Range(1, 100_000)]
    [Display(Name = "Max usage count")]
    public int? MaxUsageCount { get; set; }
}

/// <summary>Edit form for an existing discount. Type/Currency/Tour are immutable after creation.</summary>
public sealed class EditDiscountFormVm
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [Display(Name = "Discount name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Range(0, 1_000_000)]
    [Display(Name = "Discount value")]
    public decimal DiscountValue { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Valid from")]
    public DateTime ValidFrom { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Valid until")]
    public DateTime? ValidUntil { get; set; }

    [Range(1, 100_000)]
    [Display(Name = "Max usage count")]
    public int? MaxUsageCount { get; set; }
}
