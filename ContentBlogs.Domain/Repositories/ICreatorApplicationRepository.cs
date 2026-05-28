using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentBlogs.Domain.Repositories;

public interface ICreatorApplicationRepository : IRepository<CreatorApplication, Guid>
{
    /// <summary>
    /// Returns the most-recent application for the given user, regardless of status.
    /// Used to enforce single-active-application and cooling-period invariants.
    /// </summary>
    Task<CreatorApplication?> GetLatestByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the user already has an application in a non-terminal state
    /// (Draft, Pending, MoreInfoNeeded or Approved).
    /// </summary>
    Task<bool> HasActiveApplicationAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts how many times the user has applied (any status). Used to enforce
    /// the <see cref="CreatorApplication.MaxReapplications"/> cap.
    /// </summary>
    Task<int> CountByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
