using Auth.Application.Interfaces;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.AdminArchiveUser;

public sealed class AdminArchiveUserCommandHandler(
    ISecurityService securityService,
    ISessionRevocationService sessionRevocation,
    IAuthUnitOfWork unitOfWork,
    IAdminAuditWriter adminAuditWriter,
    IRequestContext requestContext,
    ICurrentUser currentUser,
    ILogger<AdminArchiveUserCommandHandler> logger)
    : ICommandHandler<AdminArchiveUserCommand>
{
    public async Task<Result> Handle(AdminArchiveUserCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure(
                Error.Failure("Auth.Unauthenticated", "Admin actor is not authenticated."),
                Outcome.Unauthorized);
        }

        var actorId = currentUser.UserId.Value;

        var transition = await securityService.ArchiveUserByAdminAsync(
            request.UserId,
            actorId,
            cancellationToken);

        if (transition.IsFailure)
            return transition;

        await sessionRevocation.RevokeAllForUserAsync(
            request.UserId,
            SessionRevocationReason.AccountArchived,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Phase 4 — append the admin audit timeline row. Reached only
        // on the success path.
        await adminAuditWriter.RecordAsync(
            new AdminAuditEntry(
                ActorUserId:  actorId,
                TargetUserId: request.UserId,
                Action:       AuditActions.AdminArchiveUser,
                Reason:       null,
                Metadata:     null,
                IpAddress:    requestContext.IpAddress),
            cancellationToken);

        logger.LogInformation(
            "Auth: Admin {AdminActorId} archived user {TargetUserId}; active sessions and refresh tokens revoked.",
            actorId,
            request.UserId);

        return Result.Success();
    }
}
