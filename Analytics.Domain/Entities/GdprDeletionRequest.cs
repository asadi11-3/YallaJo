using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

/// <summary>
/// 30-day soft-delete window for GDPR delete-my-data requests.
/// After window expires, hard-delete all user PII.
/// </summary>
public sealed class GdprDeletionRequest : BaseEntity, IAggregateRoot
{
    private GdprDeletionRequest() { }

    public Guid UserId { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime ScheduledHardDeleteAt { get; private set; }
    public bool IsCancelled { get; private set; }
    public bool IsExecuted { get; private set; }
    public DateTime? ExecutedAt { get; private set; }

    public static GdprDeletionRequest Create(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(userId));

        var now = DateTime.UtcNow;
        return new GdprDeletionRequest
        {
            UserId = userId,
            RequestedAt = now,
            ScheduledHardDeleteAt = now.AddDays(30),
            IsCancelled = false,
            IsExecuted = false
        };
    }

    public void Cancel()
    {
        if (IsExecuted) throw new InvalidOperationException("Cannot cancel an executed deletion.");
        IsCancelled = true;
    }

    public void MarkExecuted()
    {
        IsExecuted = true;
        ExecutedAt = DateTime.UtcNow;
    }

    public bool IsReadyForExecution(DateTime now) => !IsCancelled && !IsExecuted && now >= ScheduledHardDeleteAt;
}
