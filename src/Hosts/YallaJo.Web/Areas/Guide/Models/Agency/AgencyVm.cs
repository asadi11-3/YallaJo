using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Guide.Models.Agency;

public sealed class AgencyVm
{
    public IReadOnlyList<AgencyInvitationRowVm> Invitations { get; init; } = [];

    public ApplyToAgencyFormVm ApplyForm { get; set; } = new();

    public int PendingCount { get; init; }

    public bool HasInvitations => Invitations.Count > 0;
}

public sealed record AgencyInvitationRowVm(
    Guid Id,
    Guid AgencyUserId,
    string? Message,
    decimal ProposedCommissionPercentage,
    AgencyInvitationStatus Status,
    DateTime ExpiresAt,
    DateTime? RespondedAt,
    DateTime CreatedAt)
{
    public bool IsPending => Status == AgencyInvitationStatus.Pending;

    public bool IsExpired => ExpiresAt <= DateTime.UtcNow && Status == AgencyInvitationStatus.Pending;
}

public sealed class ApplyToAgencyFormVm
{
    [Required]
    [Display(Name = "Agency ID")]
    public Guid AgencyUserId { get; set; }

    [StringLength(1000)]
    [Display(Name = "Message (optional)")]
    public string? Message { get; set; }
}
