using Accounts.Domain.Enums;
using Accounts.Domain.Events.Agency;
using YallaJo.SharedKernel.Domain.Entities;

namespace Accounts.Domain.Entities;

/// <summary>
/// An invitation sent by an Agency to an IndependentGuide to join their roster.
/// Expires after 7 days if not responded to.
/// </summary>
public sealed class AgencyInvitation : AuditableEntity, IAggregateRoot
{
    private const int ExpiryDays = 7;

    private AgencyInvitation()
    {
    }

    public Guid AgencyUserId { get; private set; }
    public Guid GuideUserId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public decimal ProposedCommissionPercentage { get; private set; }
    public AgencyInvitationStatus Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }

    public static AgencyInvitation Create(
        Guid agencyUserId,
        Guid guideUserId,
        string message,
        decimal proposedCommissionPercentage)
    {
        var invitation = new AgencyInvitation
        {
            AgencyUserId = agencyUserId,
            GuideUserId = guideUserId,
            Message = message?.Trim() ?? string.Empty,
            ProposedCommissionPercentage = proposedCommissionPercentage,
            Status = AgencyInvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(ExpiryDays),
        };

        invitation.AddDomainEvent(
            new AgencyInvitationSentDomainEvent(invitation.Id, agencyUserId, guideUserId));

        return invitation;
    }

    public void Accept()
    {
        if (Status != AgencyInvitationStatus.Pending)
        {
            return;
        }

        Status = AgencyInvitationStatus.Accepted;
        RespondedAt = DateTime.UtcNow;
        MarkUpdated();

        AddDomainEvent(
            new AgencyInvitationAcceptedDomainEvent(Id, AgencyUserId, GuideUserId));
    }

    public void Decline()
    {
        if (Status != AgencyInvitationStatus.Pending)
        {
            return;
        }

        Status = AgencyInvitationStatus.Declined;
        RespondedAt = DateTime.UtcNow;
        MarkUpdated();

        AddDomainEvent(
            new AgencyInvitationDeclinedDomainEvent(Id, AgencyUserId, GuideUserId));
    }

    public void Expire()
    {
        if (Status != AgencyInvitationStatus.Pending)
        {
            return;
        }

        Status = AgencyInvitationStatus.Expired;
        MarkUpdated();
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt && Status == AgencyInvitationStatus.Pending;
}
