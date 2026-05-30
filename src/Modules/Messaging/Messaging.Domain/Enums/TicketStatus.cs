namespace Messaging.Domain.Enums;

public enum TicketStatus : byte
{
    Open           = 0,
    Assigned       = 1,
    InProgress     = 2,
    AwaitingUser   = 3,
    Resolved       = 4,
    Closed         = 5,
}
