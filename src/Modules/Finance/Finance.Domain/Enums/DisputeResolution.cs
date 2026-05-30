namespace Finance.Domain.Enums;

public enum DisputeResolution : byte
{
    RefundFull = 0,
    RefundPartial = 1,
    NoRefund = 2,
    Compromise = 3
}
