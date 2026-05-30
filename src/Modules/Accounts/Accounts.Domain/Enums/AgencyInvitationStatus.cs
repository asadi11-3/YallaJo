namespace Accounts.Domain.Enums;

public enum AgencyInvitationStatus : byte
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Expired = 3,
}
