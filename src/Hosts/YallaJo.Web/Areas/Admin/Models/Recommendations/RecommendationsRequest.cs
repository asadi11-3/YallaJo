namespace YallaJo.Web.Areas.Admin.Models.Recommendations;

public sealed class CreateBoostRequest
{
    public Guid ProviderId { get; set; }
    public string EntityKind { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public decimal Multiplier { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public sealed class CreatePinRequest
{
    public string EntityKind { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public int Position { get; set; }
    public string Context { get; set; } = string.Empty;
    public string? BadgeText { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
