namespace Finance.Domain.Enums;

public enum PaymentStatus : byte
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4,
    PartiallyRefunded = 5
}
