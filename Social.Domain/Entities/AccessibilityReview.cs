using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

public sealed class AccessibilityReview : AuditableEntity
{
    private AccessibilityReview() { } // EF Core

    public Guid UserId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public bool? WheelchairAccessible { get; private set; }
    public bool? VisualAidAvailable { get; private set; }
    public bool? HearingAidAvailable { get; private set; }
    public decimal? AccessibilityRating { get; private set; }
    public string? Comments { get; private set; }
    public DateOnly? VisitDate { get; private set; }
}
