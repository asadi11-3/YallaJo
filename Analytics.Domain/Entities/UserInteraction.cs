using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class UserInteraction : BaseEntity<long>
{
    private UserInteraction() { } // EF Core

    public Guid UserId { get; private set; }
    public InteractionType InteractionType { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public string? SessionId { get; private set; }
    public string? DeviceType { get; private set; }
    public int? DurationSeconds { get; private set; }
    public DateTime OccurredAt { get; private set; }
}
