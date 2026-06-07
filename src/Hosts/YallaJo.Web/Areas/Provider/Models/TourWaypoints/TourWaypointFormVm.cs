using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Provider.Models.TourWaypoints;

public sealed class TourWaypointFormVm
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(200, ErrorMessage = "Name must not exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Description must not exceed 1000 characters.")]
    public string? Description { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double Longitude { get; set; }

    [Display(Name = "Meeting point")]
    public bool IsMeetingPoint { get; set; }

    [Display(Name = "Stop duration (minutes)")]
    [Range(0, int.MaxValue, ErrorMessage = "Stop duration must be zero or greater.")]
    public int? StopDurationMinutes { get; set; }

    // ── Context (not posted as data) ────────────────────────────────────────────
    public Guid TourId { get; set; }
    public Guid? WaypointId { get; set; }
    public bool IsEdit => WaypointId.HasValue;
    public string? TourName { get; set; }
}
