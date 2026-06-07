namespace YallaJo.Web.Areas.Guide.Models.AgencyRoster;

public static class AgencyRosterMapper
{
    public static AgencyGuideRowVm ToRowVm(AgencyGuideResponse g) => new(
        g.AffiliationId,
        g.GuideUserId,
        GuideHandle(g.GuideUserId),
        g.CommissionPercentage,
        g.JoinedAt);

    public static AgencyApplicationRowVm ToRowVm(AgencyApplicationResponse a) => new(
        a.Id,
        a.GuideUserId,
        GuideHandle(a.GuideUserId),
        a.Message,
        a.Status,
        ApplicationBadge(a.Status),
        a.RejectionReason,
        a.ReviewedAt,
        a.CreatedAt);

    public static AgencySentInvitationRowVm ToRowVm(AgencySentInvitationResponse i) => new(
        i.Id,
        i.GuideUserId,
        GuideHandle(i.GuideUserId),
        i.Message,
        i.ProposedCommissionPercentage,
        i.Status,
        InvitationBadge(i.Status),
        i.ExpiresAt,
        i.RespondedAt,
        i.CreatedAt);

    /// <summary>Privacy-safe short guide handle (no name/email). E.g. "Guide b0000000".</summary>
    public static string GuideHandle(Guid guideUserId) => $"Guide {guideUserId.ToString("N")[..8]}";

    public static string ApplicationBadge(AgencyApplicationStatus status) => status switch
    {
        AgencyApplicationStatus.Pending => "bg-warning text-dark",
        AgencyApplicationStatus.Approved => "bg-success",
        AgencyApplicationStatus.Rejected => "bg-danger",
        _ => "bg-secondary",
    };

    public static string InvitationBadge(RosterInvitationStatus status) => status switch
    {
        RosterInvitationStatus.Pending => "bg-warning text-dark",
        RosterInvitationStatus.Accepted => "bg-success",
        RosterInvitationStatus.Declined => "bg-danger",
        RosterInvitationStatus.Expired => "bg-secondary",
        _ => "bg-secondary",
    };
}
