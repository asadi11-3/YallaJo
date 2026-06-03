using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Provider.Models.TourSchedules;

public sealed class TourScheduleFormVm : IValidatableObject
{
    [Display(Name = "Day of week")]
    [Range(0, 6, ErrorMessage = "Please choose a day of the week.")]
    public byte DayOfWeek { get; set; }

    [Required(ErrorMessage = "Start time is required.")]
    [Display(Name = "Start time")]
    [DataType(DataType.Time)]
    public TimeOnly? StartTime { get; set; }

    [Display(Name = "End time")]
    [DataType(DataType.Time)]
    public TimeOnly? EndTime { get; set; }

    // Edit-only: backend creates schedules active by default, so Create has no toggle.
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // ── Context (not posted as data) ────────────────────────────────────────────
    public Guid TourId { get; set; }
    public Guid? ScheduleId { get; set; }
    public bool IsEdit => ScheduleId.HasValue;
    public string? TourName { get; set; }

    /// <summary>Day-of-week options (value = backend byte, 0=Sunday … 6=Saturday).</summary>
    public static IReadOnlyList<(byte Value, string Label)> DayOptions { get; } =
    [
        (0, "Sunday"), (1, "Monday"), (2, "Tuesday"), (3, "Wednesday"),
        (4, "Thursday"), (5, "Friday"), (6, "Saturday"),
    ];

    /// <summary>Cross-field rule: EndTime must be after StartTime when both are present.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (StartTime.HasValue && EndTime.HasValue && EndTime.Value <= StartTime.Value)
            yield return new ValidationResult("End time must be after start time.", [nameof(EndTime)]);
    }
}
