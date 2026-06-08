using Accounts.Domain.Enums;
using Accounts.Domain.Events.Agency;
using YallaJo.SharedKernel.Domain.Entities;

namespace Accounts.Domain.Entities;

/// <summary>
/// An application submitted by an IndependentGuide to join an Agency's roster.
/// The Agency owner reviews and approves or rejects.
/// </summary>
public sealed class AgencyApplication : AuditableEntity, IAggregateRoot
{
    private AgencyApplication()
    {
    }

    public Guid GuideUserId { get; private set; }
    public Guid AgencyUserId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public AgencyApplicationStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? RespondedAt { get; private set; }

    public static AgencyApplication Create(
        Guid guideUserId,
        Guid agencyUserId,
        string? message)
    {
        var application = new AgencyApplication
        {
            GuideUserId = guideUserId,
            AgencyUserId = agencyUserId,
            // Message is optional (the apply form labels it "optional" and sends null for blank).
            // Null-coalesce mirrors AgencyInvitation.Create and prevents a NullReferenceException.
            Message = message?.Trim() ?? string.Empty,
            Status = AgencyApplicationStatus.Pending,
        };

        application.AddDomainEvent(
            new AgencyApplicationSubmittedDomainEvent(application.Id, guideUserId, agencyUserId));

        return application;
    }

    public void Approve()
    {
        if (Status != AgencyApplicationStatus.Pending)
        {
            return;
        }

        Status = AgencyApplicationStatus.Approved;
        RespondedAt = DateTime.UtcNow;
        MarkUpdated();

        AddDomainEvent(
            new AgencyApplicationApprovedDomainEvent(Id, GuideUserId, AgencyUserId));
    }

    public void Reject(string reason)
    {
        if (Status != AgencyApplicationStatus.Pending)
        {
            return;
        }

        Status = AgencyApplicationStatus.Rejected;
        RejectionReason = reason.Trim();
        RespondedAt = DateTime.UtcNow;
        MarkUpdated();

        AddDomainEvent(
            new AgencyApplicationRejectedDomainEvent(Id, GuideUserId, AgencyUserId, RejectionReason));
    }
}
