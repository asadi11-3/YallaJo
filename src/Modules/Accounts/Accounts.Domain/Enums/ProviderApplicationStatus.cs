namespace Accounts.Domain.Enums;

public enum ProviderApplicationStatus : byte
{
    Draft          = 0,
    Pending        = 1,
    MoreDocsNeeded = 2,
    Approved       = 3,
    Rejected       = 4,
    Suspended      = 5,
}
