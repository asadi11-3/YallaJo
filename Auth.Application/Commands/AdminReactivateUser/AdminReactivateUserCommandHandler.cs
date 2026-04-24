using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.AdminReactivateUser;

public sealed class AdminReactivateUserCommandHandler(
    ISecurityService securityService,
    IAdminAuditWriter adminAuditWriter,
    IRequestContext requestContext,
    ICurrentUser currentUser,
    ILogger<AdminReactivateUserCommandHandler> logger)
    : ICommandHandler<AdminReactivateUserCommand>
{
    public async Task<Result> Handle(AdminReactivateUserCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure(
                Error.Failure("Auth.Unauthenticated", "Admin actor is not authenticated."),
                Outcome.Unauthorized);
        }

        var actorId = currentUser.UserId.Value;

        var transition = await securityService.ReactivateUserByAdminAsync(
            request.UserId,
            actorId,
            cancellationToken);

        if (transition.IsFailure)
            return transition;

        // Phase 4 — append the admin audit timeline row. Reached only
        // on the success path.
        await adminAuditWriter.RecordAsync(
            new AdminAuditEntry(
                ActorUserId:  actorId,
                TargetUserId: request.UserId,
                Action:       AuditActions.AdminReactivateUser,
                Reason:       null,
                Metadata:     null,
                IpAddress:    requestContext.IpAddress),
            cancellationToken);

        logger.LogInformation(
            "Auth: Admin {AdminActorId} reactivated user {TargetUserId}.",
            actorId,
            request.UserId);

        return Result.Success();
    }
}
