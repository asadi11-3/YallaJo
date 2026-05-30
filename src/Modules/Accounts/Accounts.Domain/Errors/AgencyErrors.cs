using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Domain.Errors;

public static class AgencyErrors
{
    public static readonly Error AffiliationNotFound =
        new("Agency.AffiliationNotFound", "The agency affiliation was not found.");

    public static readonly Error InvitationNotFound =
        new("Agency.InvitationNotFound", "The agency invitation was not found.");

    public static readonly Error ApplicationNotFound =
        new("Agency.ApplicationNotFound", "The agency application was not found.");

    public static readonly Error GuideAlreadyAffiliated =
        new("Agency.GuideAlreadyAffiliated", "This guide is already affiliated with an agency.");

    public static readonly Error PendingInvitationExists =
        new("Agency.PendingInvitationExists", "A pending invitation already exists for this guide.");

    public static readonly Error PendingApplicationExists =
        new("Agency.PendingApplicationExists", "You already have a pending application to this agency.");

    public static readonly Error NotAnAgency =
        new("Agency.NotAnAgency", "Your provider account is not registered as an agency.");

    public static readonly Error NotAGuide =
        new("Agency.NotAGuide", "Only independent guides can perform this action.");

    public static readonly Error NotInvited =
        new("Agency.NotInvited", "This invitation was not sent to you.");

    public static readonly Error NotOwner =
        new("Agency.NotOwner", "You do not own this resource.");

    public static readonly Error InvitationExpired =
        new("Agency.InvitationExpired", "This invitation has expired.");

    public static readonly Error InvitationNotPending =
        new("Agency.InvitationNotPending", "This invitation is no longer in a pending state.");

    public static readonly Error ApplicationNotPending =
        new("Agency.ApplicationNotPending", "This application is no longer in a pending state.");

    public static readonly Error NotAffiliated =
        new("Agency.NotAffiliated", "This guide is not affiliated with your agency.");

    public static readonly Error NoActiveAffiliation =
        new("Agency.NoActiveAffiliation", "You are not currently affiliated with any agency.");
}
