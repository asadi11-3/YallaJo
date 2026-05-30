namespace Finance.Domain.Enums;

public enum PaymentMethod : byte
{
    CreditCard = 0,
    DebitCard = 1,
    BankTransfer = 2,
    Wallet = 3,
    Cash = 4
}
