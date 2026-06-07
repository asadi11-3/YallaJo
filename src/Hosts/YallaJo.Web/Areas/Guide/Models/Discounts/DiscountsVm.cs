using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Guide.Models.Discounts;

public sealed class DiscountsVm
{
    public IReadOnlyList<DiscountRowVm> Discounts { get; init; } = [];

    public CreateDiscountFormVm Form { get; set; } = new();

    public bool HasDiscounts => Discounts.Count > 0;
}

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
