namespace Messaging.Domain.Enums;

public enum TicketStatus : byte
{
    Open = 0,
    InProgress = 1,
    WaitingOnCustomer = 2,
    Resolved = 3,
    Closed = 4
}
