using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Provider.Models.TourAvailability;

/// <summary>The availability-management page for one tour.</summary>
public sealed class TourAvailabilityIndexVm
{
    public Guid TourId { get; init; }
    public string TourName { get; init; } = string.Empty;
    public string TourStatusLabel { get; init; } = string.Empty;
    public IReadOnlyList<AvailabilitySlotRowVm> Slots { get; init; } = [];

    public bool HasSlots => Slots.Count > 0;
}

public sealed class AvailabilitySlotRowVm
{
    public Guid Id { get; init; }
    public DateOnly Date { get; init; }
    public string DateLabel { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public int MaxCapacity { get; init; }
    public int AvailableCount { get; init; }
    public bool IsActive { get; init; }
    public string RowVersion { get; init; } = string.Empty;

    /// <summary>True when seats are held (booked/locked) so delete will be blocked server-side.</summary>
    public bool HasHeldSeats => AvailableCount < MaxCapacity;
}

/// <summary>Create/edit form for an availability slot.</summary>
public sealed class AvailabilitySlotFormVm
{
    public Guid TourId { get; set; }
    public Guid SlotId { get; set; }

    public string TourName { get; set; } = string.Empty;

    /// <summary>True when editing an existing slot (only capacity is editable).</summary>
    public bool IsEdit { get; set; }

    [Required(ErrorMessage = "Please choose a date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "Please enter a start time.")]
    [DataType(DataType.Time)]
    [Display(Name = "Start time")]
    public TimeOnly? StartTime { get; set; }

    [Required(ErrorMessage = "Please enter an end time.")]
    [DataType(DataType.Time)]
    [Display(Name = "End time")]
    public TimeOnly? EndTime { get; set; }

    [Range(1, 1000, ErrorMessage = "Capacity must be at least 1.")]
    [Display(Name = "Capacity")]
    public int MaxCapacity { get; set; } = 1;

    /// <summary>RowVersion for optimistic concurrency on edit (hidden field).</summary>
    public string? RowVersion { get; set; }
}

/// <summary>Bulk recurring-slot generation form.</summary>
public sealed class BulkAvailabilitySlotFormVm
{
    public Guid TourId { get; set; }

    [Required(ErrorMessage = "Please choose a start date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Start date")]
    public DateOnly? StartDate { get; set; }

    [Required(ErrorMessage = "Please choose an end date.")]
    [DataType(DataType.Date)]
    [Display(Name = "End date")]
    public DateOnly? EndDate { get; set; }

    [Required]
    [Display(Name = "Recurrence")]
    public string Recurrence { get; set; } = "Daily";

    [Display(Name = "Days of week")]
    public List<string> DaysOfWeek { get; set; } = [];

    [Required(ErrorMessage = "Please enter a start time.")]
    [DataType(DataType.Time)]
    [Display(Name = "Start time")]
    public TimeOnly? StartTime { get; set; }

    [Required(ErrorMessage = "Please enter an end time.")]
    [DataType(DataType.Time)]
    [Display(Name = "End time")]
    public TimeOnly? EndTime { get; set; }

    [Range(1, 1000, ErrorMessage = "Capacity must be at least 1.")]
    [Display(Name = "Capacity per slot")]
    public int MaxCapacity { get; set; } = 1;

    [Display(Name = "Skip dates that already have a slot")]
    public bool SkipExisting { get; set; } = true;

    public static IReadOnlyList<string> RecurrenceOptions { get; } = ["Daily", "Weekly", "Custom"];

    // (form value, short label) — value is the full DayOfWeek name the API expects.
    public static IReadOnlyList<(string Value, string Label)> DayOptions { get; } =
    [
        ("Monday", "Mon"), ("Tuesday", "Tue"), ("Wednesday", "Wed"), ("Thursday", "Thu"),
        ("Friday", "Fri"), ("Saturday", "Sat"), ("Sunday", "Sun"),
    ];
}

/// <summary>[Backend] B3 — month-grid calendar VM (CAL1).</summary>
public sealed class AvailabilityCalendarVm
{
    public Guid TourId { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public IReadOnlyList<AvailabilityCalendarDayVm> Days { get; init; } = [];

    public AvailabilityCalendarDayVm? For(DateOnly date) => Days.FirstOrDefault(d => d.Date == date);
}

public sealed class AvailabilityCalendarDayVm
{
    public DateOnly Date { get; init; }
    public int SlotCount { get; init; }
    public int TotalCapacity { get; init; }
    public int BookedSeats { get; init; }
    public bool HasOpenSlots { get; init; }
}
