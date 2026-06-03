using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Guide.Models;

public sealed class GuideScheduleVm
{
    public bool IsGuide { get; set; }
    public IReadOnlyList<AvailabilityBlockRowVm> Blocks { get; set; } = [];
    public AddBlockVm NewBlock { get; set; } = new();
    public bool HasBlocks => Blocks.Count > 0;
}

public sealed class AvailabilityBlockRowVm
{
    public Guid Id { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public string? Reason { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class AddBlockVm
{
    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "From")]
    public DateOnly StartDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "To")]
    public DateOnly EndDate { get; set; }

    [StringLength(200)]
    [Display(Name = "Reason")]
    public string? Reason { get; set; }
}
