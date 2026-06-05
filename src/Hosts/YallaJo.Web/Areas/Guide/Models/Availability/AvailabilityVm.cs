using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Guide.Models.Availability;

public sealed class AvailabilityVm
{
    public IReadOnlyList<AvailabilityBlockRowVm> Blocks { get; set; } = [];

    public AddAvailabilityBlockFormVm Form { get; set; } = new();

    public bool HasBlocks => Blocks.Count > 0;
}

public sealed record AvailabilityBlockRowVm(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Reason,
    DateTime CreatedAt,
    bool IsActive);

public sealed class AddAvailabilityBlockFormVm
{
    [Required]
    [Display(Name = "Start date")]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Required]
    [Display(Name = "End date")]
    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [StringLength(256)]
    [Display(Name = "Reason (optional)")]
    public string? Reason { get; set; }
}
