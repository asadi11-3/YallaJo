using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class ProviderBankAccount : AuditableEntity, IAggregateRoot
{
    private ProviderBankAccount() { } // EF Core

    public Guid UserId { get; private set; }
    public string BankName { get; private set; } = string.Empty;
    public string AccountHolderName { get; private set; } = string.Empty;
    public string AccountNumber { get; private set; } = string.Empty;
    public string? Iban { get; private set; }
    public string? SwiftCode { get; private set; }
    public string Currency { get; private set; } = "JOD";
    public bool IsDefault { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
}
