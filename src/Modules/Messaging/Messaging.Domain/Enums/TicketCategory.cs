namespace Messaging.Domain.Enums;

public enum TicketCategory : byte
{
    PaymentProblem      = 0,
    BookingIssue        = 1,
    ProviderComplaint   = 2,
    AccountHelp         = 3,
    BugReport           = 4,
    Other               = 5,
    PayoutIssue         = 6,
    CommissionDispute   = 7,
    GuideScheduleIssue  = 8,
    TourApprovalHelp    = 9,
    DocumentVerification = 10,
}
