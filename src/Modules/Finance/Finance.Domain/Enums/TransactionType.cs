namespace Finance.Domain.Enums;

public enum TransactionType : byte
{
    Earned = 0,
    Redeemed = 1,
    Expired = 2,
    Adjusted = 3
}
