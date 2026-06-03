using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Provider.Models.TourPricing;

public sealed class TourPricingFormVm
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(200, ErrorMessage = "Name must not exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price must be zero or greater.")]
    public decimal Price { get; set; }

    // Locked to the tour currency (rendered read-only); posted back so the API receives it.
    [Required(ErrorMessage = "Currency is required.")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Use a 3-letter currency code.")]
    public string Currency { get; set; } = string.Empty;

    [Required(ErrorMessage = "Participant type is required.")]
    [Display(Name = "Participant type")]
    public string ParticipantType { get; set; } = "Adult";

    [Display(Name = "Min participants")]
    [Range(1, int.MaxValue, ErrorMessage = "Minimum participants must be at least 1.")]
    public int MinParticipants { get; set; } = 1;

    [Display(Name = "Max participants")]
    [Range(1, int.MaxValue, ErrorMessage = "Maximum participants must be at least 1.")]
    public int? MaxParticipants { get; set; }

    // Edit-only: backend creates tiers active by default, so Create has no toggle.
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // ── Context (not posted as data) ────────────────────────────────────────────
    public Guid TourId { get; set; }
    public Guid? TierId { get; set; }
    public bool IsEdit => TierId.HasValue;
    public string? TourName { get; set; }

    /// <summary>Participant type options (exact backend enum names).</summary>
    public IReadOnlyList<string> ParticipantTypeOptions { get; } =
        ["Adult", "Child", "Senior", "Student", "Infant", "Group", "Family", "Other"];
}
