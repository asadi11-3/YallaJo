using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Guide.Models.MyTours;

public sealed class MyToursVm
{
    public IReadOnlyList<MyTourRowVm> Tours { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public bool HasTours => Tours.Count > 0;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 1;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public sealed record MyTourRowVm(
    Guid TourId,
    string Title,
    string? Slug,
    bool IsProposer,
    string? OfferingStatus,
    bool OffersPrivateTour,
    DateTime? AssignedAt);

public sealed class OfferingDetailVm
{
    public Guid TourId { get; init; }
    public Guid GuideId { get; init; }
    public string TourTitle { get; init; } = "";
    public string Status { get; init; } = "";
    public bool IsProposer { get; init; }
    public DateTime? AssignedAt { get; init; }

    public bool OffersPrivateTour { get; init; }
    public decimal? PrivateTourPriceMultiplier { get; init; }
    public decimal? PrivateTourFlatPrice { get; init; }

    public IReadOnlyList<ScheduleRowVm> Schedules { get; init; } = [];
    public IReadOnlyList<PricingTierRowVm> PricingTiers { get; init; } = [];

    public AddScheduleFormVm ScheduleForm { get; set; } = new();
    public AddPricingTierFormVm PricingTierForm { get; set; } = new();
    public PrivateTourFormVm PrivateTourForm { get; set; } = new();

    public static IReadOnlyList<DayOptionVm> DayOptions { get; } =
    [
        new(0, "Sunday"),
        new(1, "Monday"),
        new(2, "Tuesday"),
        new(3, "Wednesday"),
        new(4, "Thursday"),
        new(5, "Friday"),
        new(6, "Saturday"),
    ];

    public static string DayName(byte day) =>
        DayOptions.FirstOrDefault(d => d.Value == day)?.Label ?? day.ToString();
}

public sealed record DayOptionVm(byte Value, string Label);

public sealed record ScheduleRowVm(
    Guid Id,
    byte DayOfWeek,
    string StartTime,
    string? EndTime,
    bool IsActive);

public sealed record PricingTierRowVm(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int MinParticipants,
    int MaxParticipants,
    bool IsActive);

public sealed class AddScheduleFormVm
{
    [Range(0, 6)]
    [Display(Name = "Day of week")]
    public byte DayOfWeek { get; set; }

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "Start time")]
    public string StartTime { get; set; } = "";

    [DataType(DataType.Time)]
    [Display(Name = "End time")]
    public string? EndTime { get; set; }
}

public sealed class AddPricingTierFormVm
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [Range(0.0, 1_000_000.0)]
    public decimal Price { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "JOD";

    [Range(1, 1000)]
    [Display(Name = "Min participants")]
    public int MinParticipants { get; set; } = 1;

    [Range(1, 1000)]
    [Display(Name = "Max participants")]
    public int MaxParticipants { get; set; } = 1;

    [StringLength(500)]
    public string? Description { get; set; }
}

public sealed class PrivateTourFormVm
{
    [Range(0.0, 100.0)]
    [Display(Name = "Price multiplier")]
    public decimal? Multiplier { get; set; }

    [Range(0.0, 1_000_000.0)]
    [Display(Name = "Flat price")]
    public decimal? FlatPrice { get; set; }
}
