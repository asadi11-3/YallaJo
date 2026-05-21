namespace Messaging.Domain.Enums;

public enum TicketCategory : byte
{
    PaymentProblem      = 0,
    BookingIssue        = 1,
    ProviderComplaint   = 2,
    AccountHelp         = 3,
    BugReport           = 4,
    Other               = 5,
}
