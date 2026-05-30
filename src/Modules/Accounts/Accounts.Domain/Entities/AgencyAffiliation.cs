using Accounts.Domain.Enums;
using Accounts.Domain.Events.Agency;
using YallaJo.SharedKernel.Domain.Entities;

namespace Accounts.Domain.Entities;

/// <summary>
/// Represents an active or terminated affiliation between an Agency provider and
/// an IndependentGuide. Created when an invitation is accepted or an application
/// is approved.
/// </summary>
public sealed class AgencyAffiliation : AuditableEntity, IAggregateRoot
{
    private AgencyAffiliation()
    {
    }

    public Guid AgencyUserId { get; private set; }
    public Guid GuideUserId { get; private set; }
    public decimal CommissionPercentage { get; private set; }
    public AgencyAffiliationStatus Status { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? TerminatedAt { get; private set; }
    public Guid? TerminatedByUserId { get; private set; }
    public string? TerminationReason { get; private set; }

    public static AgencyAffiliation Create(
        Guid agencyUserId,
        Guid guideUserId,
        decimal commissionPercentage)
    {
        var affiliation = new AgencyAffiliation
        {
            AgencyUserId = agencyUserId,
            GuideUserId = guideUserId,
            CommissionPercentage = commissionPercentage,
            Status = AgencyAffiliationStatus.Active,
            JoinedAt = DateTime.UtcNow,
        };

        affiliation.AddDomainEvent(
            new AgencyAffiliationCreatedDomainEvent(affiliation.Id, agencyUserId, guideUserId));

        return affiliation;
    }

    public void Terminate(Guid terminatedByUserId, string reason)
    {
        if (Status == AgencyAffiliationStatus.Terminated)
        {
            return;
        }

        Status = AgencyAffiliationStatus.Terminated;
        TerminatedAt = DateTime.UtcNow;
        TerminatedByUserId = terminatedByUserId;
        TerminationReason = reason.Trim();
        MarkUpdated();

        AddDomainEvent(
            new AgencyAffiliationTerminatedDomainEvent(Id, AgencyUserId, GuideUserId, terminatedByUserId, reason));
    }
}
