namespace Finance.Domain.Enums;

public enum PayoutStatus : byte
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}
