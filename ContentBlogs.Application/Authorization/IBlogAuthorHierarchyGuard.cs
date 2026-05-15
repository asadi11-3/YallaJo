using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Authorization;

public interface IBlogAuthorHierarchyGuard
{
    Task<Result> EnsureCanManageBlogOwnedByAsync(
        Guid authorId,
        CancellationToken cancellationToken = default);
}
