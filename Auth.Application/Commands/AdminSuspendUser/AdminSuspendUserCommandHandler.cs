using Auth.Application.Interfaces;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.AdminSuspendUser;

public sealed class AdminSuspendUserCommandHandler(
    ISecurityService securityService,
    ISessionRevocationService sessionRevocation,
    IAuthUnitOfWork unitOfWork,
    IAdminAuditWriter adminAuditWriter,
    IRequestContext requestContext,
    ICurrentUser currentUser,
    ILogger<AdminSuspendUserCommandHandler> logger)
    : ICommandHandler<AdminSuspendUserCommand>
{
    public async Task<Result> Handle(AdminSuspendUserCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure(
                Error.Failure("Auth.Unauthenticated", "Admin actor is not authenticated."),
                Outcome.Unauthorized);
        }

        var actorId = currentUser.UserId.Value;

        var transition = await securityService.SuspendUserByAdminAsync(
            request.UserId,
            actorId,
            cancellationToken);

        if (transition.IsFailure)
            return transition;

        await sessionRevocation.RevokeAllForUserAsync(
            request.UserId,
            SessionRevocationReason.AccountSuspended,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Phase 4 — append the admin audit timeline row. Reached only
        // on the success path: authn / authz / lifecycle failures
        // returned above without writing a row.
        await adminAuditWriter.RecordAsync(
            new AdminAuditEntry(
                ActorUserId:  actorId,
                TargetUserId: request.UserId,
                Action:       AuditActions.AdminSuspendUser,
                Reason:       null,
                Metadata:     null,
                IpAddress:    requestContext.IpAddress),
            cancellationToken);

        logger.LogInformation(
            "Auth: Admin {AdminActorId} suspended user {TargetUserId}; active sessions and refresh tokens revoked.",
            actorId,
            request.UserId);

        return Result.Success();
    }
}
