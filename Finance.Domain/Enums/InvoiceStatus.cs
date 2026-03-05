namespace Finance.Domain.Enums;

public enum InvoiceStatus : byte
{
    Draft = 0,
    Sent = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4
}
