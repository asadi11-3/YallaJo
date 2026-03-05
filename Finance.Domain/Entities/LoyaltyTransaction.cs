using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class LoyaltyTransaction : BaseEntity
{
    private LoyaltyTransaction() { } // EF Core

    public Guid LoyaltyPointsId { get; private set; }
    public int Points { get; private set; }
    public TransactionType TransactionType { get; private set; }
    public string? Description { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public LoyaltyPoints LoyaltyPoints { get; private set; } = default!;
}
