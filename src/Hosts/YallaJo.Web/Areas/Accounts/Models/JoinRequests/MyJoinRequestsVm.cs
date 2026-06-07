using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Accounts.Models.JoinRequests;

public sealed class MyJoinRequestsVm
{
    public IReadOnlyList<JoinRequestRowVm> Requests { get; init; } = [];

    public bool HasRequests => Requests.Count > 0;
}

/// <summary>
/// Form to request joining an existing confirmed group booking (§3.2).
/// Mirrors the API <c>SubmitJoinRequestRequest(TourBookingId, AvailabilitySlotId, ParticipantCount, Message)</c>.
/// GUIDs are nullable so <c>[Required]</c> actually rejects a missing value (a non-nullable
/// Guid binds to <c>Guid.Empty</c> and would pass [Required]); <see cref="IValidatableObject"/>
/// additionally rejects an explicit empty GUID.
/// </summary>
public sealed class JoinRequestFormVm : IValidatableObject
{
    [Required(ErrorMessage = "Enter the booking reference you were given.")]
    [Display(Name = "Booking reference")]
    public Guid? TourBookingId { get; set; }

    [Required(ErrorMessage = "Choose the date & time slot.")]
    [Display(Name = "Date & time")]
    public Guid? AvailabilitySlotId { get; set; }

    [Range(1, 50, ErrorMessage = "Number of participants must be between 1 and 50.")]
    [Display(Name = "Participants")]
    public int ParticipantCount { get; set; } = 1;

    [StringLength(500)]
    [Display(Name = "Message to the organiser (optional)")]
    public string? Message { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TourBookingId == Guid.Empty)
        {
            yield return new ValidationResult("Enter a valid booking reference.", [nameof(TourBookingId)]);
        }

        if (AvailabilitySlotId == Guid.Empty)
        {
            yield return new ValidationResult("Choose a valid date & time slot.", [nameof(AvailabilitySlotId)]);
        }
    }
}

public sealed class JoinRequestRowVm
{
    public Guid Id { get; init; }

    public string Status { get; init; } = "Pending";

    public int ParticipantCount { get; init; }

    public string? Message { get; init; }

    public DateTime ExpiresAt { get; init; }

    public DateTime? RespondedAt { get; init; }

    public string? ResponseMessage { get; init; }

    public Guid? ResultingBookingId { get; init; }

    public DateTime CreatedAt { get; init; }

    public bool IsPending => string.Equals(Status, "Pending", StringComparison.OrdinalIgnoreCase);

    public bool IsApproved => string.Equals(Status, "Approved", StringComparison.OrdinalIgnoreCase);
}
