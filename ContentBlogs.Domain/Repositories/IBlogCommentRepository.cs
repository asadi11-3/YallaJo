using ContentBlogs.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentBlogs.Domain.Repositories;

public interface IBlogCommentRepository : IRepository<BlogComment, Guid>
{
    /// <summary>
    /// Loads a comment with its parent chain eagerly populated (parent and
    /// parent-of-parent). Required by <c>BlogComment.Create</c> to validate the
    /// nesting-depth invariant without issuing extra round-trips.
    /// Tracking is enabled because callers typically follow up with a write.
    /// </summary>
    Task<BlogComment?> GetWithParentChainAsync(
        Guid commentId,
        CancellationToken cancellationToken = default);
}
