using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Provider.Models.Tours;

public sealed class ProviderTourFormVm
{
    public string? RowVersion { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(300, ErrorMessage = "Name must not exceed 300 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug is required.")]
    [MaxLength(300, ErrorMessage = "Slug must not exceed 300 characters.")]
    [RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Slug must contain only lowercase letters, digits, and hyphens.")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Difficulty is required.")]
    public string Difficulty { get; set; } = "Easy";

    [Display(Name = "Duration (minutes)")]
    [Range(1, int.MaxValue, ErrorMessage = "Duration must be greater than 0.")]
    public int DurationMinutes { get; set; } = 60;

    [Display(Name = "Max group size")]
    [Range(1, int.MaxValue, ErrorMessage = "Max group size must be greater than 0.")]
    public int MaxGroupSize { get; set; } = 10;

    [Display(Name = "Base price")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Base price must be greater than 0.")]
    public decimal BasePrice { get; set; }

    [Required(ErrorMessage = "Currency is required.")]
    [MaxLength(3, ErrorMessage = "Use a 3-letter currency code.")]
    public string Currency { get; set; } = "JOD";

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal Longitude { get; set; }

    // Optional v1 fields.
    [Display(Name = "Short description")]
    [MaxLength(1000, ErrorMessage = "Short description must not exceed 1000 characters.")]
    public string? ShortDescription { get; set; }

    [MaxLength(4000, ErrorMessage = "Description must not exceed 4000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please select a place.")]
    [Display(Name = "Place")]
    public Guid? PlaceId { get; set; }

    [Display(Name = "Child friendly")]
    public bool IsChildFriendly { get; set; }

    [Display(Name = "Accessible")]
    public bool IsAccessible { get; set; }

    [Display(Name = "Cancellation policy (hours)")]
    [Range(0, int.MaxValue, ErrorMessage = "Cancellation hours cannot be negative.")]
    public int CancellationPolicyHours { get; set; } = 24;

    // Meeting point — both or neither (required for submit readiness; optional at create).
    [Display(Name = "Meeting point latitude")]
    [Range(-90, 90, ErrorMessage = "Meeting point latitude must be between -90 and 90.")]
    public decimal? MeetingPointLatitude { get; set; }

    [Display(Name = "Meeting point longitude")]
    [Range(-180, 180, ErrorMessage = "Meeting point longitude must be between -180 and 180.")]
    public decimal? MeetingPointLongitude { get; set; }

    // Context for the view (not posted as data).
    public Guid? TourId { get; set; }
    public bool IsEdit => TourId.HasValue;
    public string? Status { get; set; }
    public string? StatusLabel { get; set; }

    public bool CanSubmit => ProviderToursMapper.CanSubmit(Status ?? string.Empty);
    public bool CanArchive => ProviderToursMapper.CanArchive(Status ?? string.Empty);

    public IReadOnlyList<string> DifficultyOptions { get; } = ["Easy", "Moderate", "Hard", "Expert"];

    // Place picker (view-context only; not posted). Populated by the controller
    // from the places lookup. When loading fails, this stays empty and
    // PlaceOptionsLoadError carries a friendly message so the form stays usable.
    public IReadOnlyList<PlaceOptionVm> PlaceOptions { get; set; } = [];
    public string? PlaceOptionsLoadError { get; set; }

    // Submit-readiness derived from persisted data (edit mode only). Drives the
    // wizard check marks and the Review & Submit readiness panel. Null on create
    // or when readiness could not be resolved.
    public TourReadinessVm? Readiness { get; set; }
}
