using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Business.Models.Services;

public sealed class ServicesVm
{
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = "";
    public string Status { get; set; } = "";
    public IReadOnlyList<ServiceRowVm> Services { get; set; } = [];
    public AddServiceFormVm Form { get; set; } = new();
    public bool HasServices => Services.Count > 0;
}

public sealed record ServiceRowVm(
    Guid Id,
    string Name,
    decimal Price,
    int DurationMinutes,
    string Currency);

public sealed class AddServiceFormVm
{
    [Required]
    [StringLength(150, MinimumLength = 1)]
    [Display(Name = "Service name")]
    public string Name { get; set; } = "";

    [Range(0, 1_000_000)]
    [Display(Name = "Price")]
    public decimal Price { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    [Display(Name = "Currency")]
    public string Currency { get; set; } = "JOD";

    [Range(0, 100_000)]
    [Display(Name = "Duration (minutes)")]
    public int DurationMinutes { get; set; } = 60;

    [Range(0, 100_000)]
    [Display(Name = "Max capacity")]
    public int MaxCapacity { get; set; } = 1;

    [Required]
    [Display(Name = "Category")]
    public ServiceCategory Category { get; set; } = ServiceCategory.Other;

    [StringLength(1000)]
    [Display(Name = "Description (optional)")]
    public string? Description { get; set; }

    [Range(0, 1000)]
    [Display(Name = "Sort order")]
    public int SortOrder { get; set; }
}
