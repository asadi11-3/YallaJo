namespace Analytics.Domain.Entities;

/// <summary>
/// Tracks which A/B experiment variant a user was assigned to.
/// Composite PK: (UserId, ExperimentId).
/// </summary>
public sealed class ExperimentAssignment
{
    private ExperimentAssignment() { }

    public Guid UserId { get; private set; }
    public Guid ExperimentId { get; private set; }
    public string VariantName { get; private set; } = string.Empty;
    public DateTime AssignedAt { get; private set; }

    public static ExperimentAssignment Create(Guid userId, Guid experimentId, string variantName)
        => new()
        {
            UserId = userId,
            ExperimentId = experimentId,
            VariantName = variantName,
            AssignedAt = DateTime.UtcNow
        };
}
