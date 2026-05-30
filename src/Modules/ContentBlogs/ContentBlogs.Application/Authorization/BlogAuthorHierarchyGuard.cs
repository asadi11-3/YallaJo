using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Authorization;

public sealed class BlogAuthorHierarchyGuard(
    ICurrentUser currentUser,
    IUserPrivilegeLevelReader privilegeLevelReader,
    ILogger<BlogAuthorHierarchyGuard> logger)
    : IBlogAuthorHierarchyGuard
{
    public async Task<Result> EnsureCanManageBlogOwnedByAsync(
        Guid authorId,
        CancellationToken cancellationToken = default)
    {
        // ── 1. Self-management is always allowed ──────────────────────────────
        // The endpoint's MustHavePermissionAttribute has already verified the
        // actor holds the Blog/{Update|Delete|Approve|Read} permission.  An
        // author with that permission may manage their OWN content regardless
        // of their privilege level — including a Standard author editing their
        // own blog (something the user-management hierarchy explicitly forbids,
        // which is why we do NOT delegate to EnsureCanManageUserAsync).
        if (currentUser.UserId!.Value == authorId)
        {
            return Result.Success();
        }

        // ── 2. Acting level must be strictly above Standard ───────────────────
        var actingLevel = AppRoles.HighestPrivilegeLevel(currentUser.Roles);
        if (actingLevel <= RolePrivilegeLevel.Standard)
        {
            logger.LogWarning(
                "BlogAuthorHierarchyGuard: user {ActorId} (level {ActorLevel}) cannot " +
                "manage content by another user {AuthorId} from the Standard tier.",
                currentUser.UserId, actingLevel, authorId);
            return Result.Failure(
                new Error(
                    "Blog.AuthorHierarchyForbidden",
                    "You cannot manage content created by a user at the same or higher privilege level."),
                Outcome.Forbidden);
        }

        // ── 3. Author level must be strictly below acting level ───────────────
        var targetLevel = await privilegeLevelReader
            .GetPrivilegeLevelAsync(authorId, cancellationToken)
            .ConfigureAwait(false);

        if (actingLevel <= targetLevel)
        {
            logger.LogWarning(
                "BlogAuthorHierarchyGuard: user {ActorId} (level {ActorLevel}) cannot " +
                "manage content by {AuthorId} (level {AuthorLevel}) — actor must strictly outrank the author.",
                currentUser.UserId, actingLevel, authorId, targetLevel);
            return Result.Failure(
                new Error(
                    "Blog.AuthorHierarchyForbidden",
                    "You cannot manage content created by a user at the same or higher privilege level."),
                Outcome.Forbidden);
        }

        return Result.Success();
    }
}
