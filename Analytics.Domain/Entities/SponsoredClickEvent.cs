using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class SponsoredClickEvent : BaseEntity<long>
{
    private SponsoredClickEvent() { }

    public Guid BidId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? SessionId { get; private set; }
    public EntityType SourceKind { get; private set; }
    public Guid SourceId { get; private set; }
    public int Position { get; private set; }
    public decimal ChargedAmount { get; private set; }
    public DateTime ClickedAt { get; private set; }
    public bool IsFraudulent { get; private set; }

    public static SponsoredClickEvent Record(
        Guid bidId, Guid? userId, string? sessionId,
        EntityType sourceKind, Guid sourceId, int position,
        decimal chargedAmount, DateTime clickedAt)
        => new()
        {
            BidId = bidId,
            UserId = userId,
            SessionId = sessionId,
            SourceKind = sourceKind,
            SourceId = sourceId,
            Position = position,
            ChargedAmount = chargedAmount,
            ClickedAt = clickedAt
        };

    public void MarkFraudulent() => IsFraudulent = true;
}
