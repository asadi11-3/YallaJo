using Social.Domain.Entities;
using Social.Domain.Enums;

namespace Social.Domain.Repositories;

/// <summary>Append-only query interface for <see cref="ContentModerationLog"/> (no IRepository base — not IAggregateRoot).</summary>
public interface IContentModerationLogRepository
{
    /// <summary>Appends a new log entry.</summary>
    Task AddAsync(ContentModerationLog entry, CancellationToken ct = default);

    /// <summary>Returns all moderation log entries (admin, cursor-paginated).</summary>
    Task<(IReadOnlyList<ContentModerationLog> Items, Guid? NextCursor)> GetPageAsync(
        Guid? afterId, int pageSize, CancellationToken ct = default);

    /// <summary>Returns moderation log entries for a specific entity.</summary>
    Task<IReadOnlyList<ContentModerationLog>> GetByEntityAsync(
        ReportableEntityType entityType, Guid entityId, CancellationToken ct = default);
}
