using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Provider.Models.Tours;

public sealed class CreateTourVm
{
    [Required, StringLength(150)]
    [Display(Name = "Tour name")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(160)]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Use lowercase letters, numbers and hyphens only.")]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public string Difficulty { get; set; } = "Easy";

    [Range(1, 100000)]
    [Display(Name = "Duration (minutes)")]
    public int DurationMinutes { get; set; } = 60;

    [Range(1, 1000)]
    [Display(Name = "Max group size")]
    public int MaxGroupSize { get; set; } = 10;

    [Range(0, 1000000)]
    [Display(Name = "Base price")]
    public decimal BasePrice { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "USD";

    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(300)]
    [Display(Name = "Short description")]
    public string? ShortDescription { get; set; }

    [Range(0, 120)]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Display(Name = "Child friendly")]
    public bool IsChildFriendly { get; set; }

    [Display(Name = "Wheelchair accessible")]
    public bool IsAccessible { get; set; }

    [Display(Name = "Instant booking")]
    public bool IsInstantBooking { get; set; }

    [Range(0, 720)]
    [Display(Name = "Free cancellation window (hours)")]
    public int CancellationPolicyHours { get; set; } = 24;

    public static IReadOnlyList<string> DifficultyOptions { get; } =
        ["Easy", "Moderate", "Hard", "Expert"];
}

public sealed class ScheduleRowVm
{
    public Guid Id { get; init; }
    public string DayName { get; init; } = string.Empty;
    public TimeOnly StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public bool IsActive { get; init; }
}

public sealed class PricingRowVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ParticipantType { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public int MinParticipants { get; init; }
    public int? MaxParticipants { get; init; }
    public bool IsActive { get; init; }
}

public sealed class WaypointRowVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? WaypointType { get; init; }
    public int SortOrder { get; init; }
    public int? DurationMinutes { get; init; }
}

public sealed class ManageTourVm
{
    public Guid TourId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Status { get; init; }
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public int ReviewCount { get; init; }
    public int BookingCount { get; init; }

    public IReadOnlyList<ScheduleRowVm> Schedules { get; init; } = [];
    public IReadOnlyList<PricingRowVm> PricingTiers { get; init; } = [];
    public IReadOnlyList<WaypointRowVm> Waypoints { get; init; } = [];
    public ChildrenInfoVm ChildrenInfo { get; init; } = new();

    public AddScheduleVm NewSchedule { get; init; } = new();
    public AddPricingVm NewPricing { get; init; } = new();
    public AddWaypointVm NewWaypoint { get; init; } = new();

    public static IReadOnlyList<string> ParticipantTypeOptions { get; } =
        ["Adult", "Child", "Senior", "Student", "Infant", "Group", "Family", "Other"];

    public static IReadOnlyList<string> WaypointTypeOptions { get; } =
        ["Start", "Stop", "Meal", "Photo", "LandMark", "RestStop", "End"];

    public static IReadOnlyList<(byte Value, string Name)> DayOptions { get; } =
        [(0, "Sunday"), (1, "Monday"), (2, "Tuesday"), (3, "Wednesday"), (4, "Thursday"), (5, "Friday"), (6, "Saturday")];
}

public sealed class AddScheduleVm
{
    [Range(0, 6)]
    public byte DayOfWeek { get; set; } = 1;

    [Required]
    [Display(Name = "Start time")]
    public TimeOnly StartTime { get; set; } = new(9, 0);

    [Display(Name = "End time")]
    public TimeOnly? EndTime { get; set; }
}

public sealed class AddPricingVm
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string ParticipantType { get; set; } = "Adult";

    [Range(0, 1000000)]
    public decimal Price { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "USD";

    [Range(1, 1000)]
    [Display(Name = "Min participants")]
    public int MinParticipants { get; set; } = 1;

    [Range(1, 1000)]
    [Display(Name = "Max participants")]
    public int? MaxParticipants { get; set; }
}

public sealed class AddWaypointVm
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Type")]
    public string WaypointType { get; set; } = "Stop";

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Range(0, 100000)]
    [Display(Name = "Duration (minutes)")]
    public int? DurationMinutes { get; set; }
}

public sealed class ChildrenInfoVm
{
    [Display(Name = "Allows children")]
    public bool AllowsChildren { get; set; }

    [Range(0, 18)]
    [Display(Name = "Min child age")]
    public int? MinChildAge { get; set; }

    [Range(0, 18)]
    [Display(Name = "Max child age")]
    public int? MaxChildAge { get; set; }

    [StringLength(500)]
    [Display(Name = "Child facilities")]
    public string? ChildFacilities { get; set; }
}
