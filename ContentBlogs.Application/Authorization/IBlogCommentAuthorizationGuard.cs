using ContentBlogs.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Authorization;

/// <summary>
/// Row-level authorization for blog-comment mutations. Sits behind the coarse
/// endpoint-level permission checks (<c>MustHavePermissionAttribute</c>) and
/// enforces:
///   • ownership (owner can act on own comment),
///   • the 30-minute edit window for owners,
///   • the moderation hierarchy (Admin/SuperAdmin/Owner can act on comments
///     authored by strictly lower-privileged users) via the existing
///     <c>IBlogAuthorHierarchyGuard</c>.
/// </summary>
public interface IBlogCommentAuthorizationGuard
{
    /// <summary>
    /// Authorizes editing the supplied comment as the current user at <paramref name="utcNow"/>.
    /// Owner is allowed only within the 30-minute edit window; admin/superadmin/owner
    /// follow the hierarchy guard rules with no time restriction.
    /// </summary>
    Task<Result> ResolveEditAuthorizationAsync(
        BlogComment comment,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Authorizes deleting (soft-delete / redaction) the supplied comment as the
    /// current user. Owner may delete own comment at any time; admin/superadmin/owner
    /// follow the hierarchy guard rules.
    /// </summary>
    Task<Result> ResolveDeleteAuthorizationAsync(
        BlogComment comment,
        CancellationToken cancellationToken = default);
}
