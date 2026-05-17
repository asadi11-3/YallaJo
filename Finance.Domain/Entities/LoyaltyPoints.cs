using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class LoyaltyPoints : AuditableEntity, IAggregateRoot
{
    private readonly List<LoyaltyTransaction> _transactions = [];

    private LoyaltyPoints() { } // EF Core

    public Guid UserId { get; private set; }
    public int TotalPoints { get; private set; }
    public int AvailablePoints { get; private set; }
    public int LifetimePoints { get; private set; }

    public IReadOnlyCollection<LoyaltyTransaction> Transactions => _transactions.AsReadOnly();
}
