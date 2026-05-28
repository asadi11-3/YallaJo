using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.UnbanUser;

internal sealed class UnbanUserCommandHandler(
    IUserModerationRepository userModerationRepository,
    IContentModerationLogRepository moderationLogRepository,
    ISocialUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<UnbanUserCommandHandler> logger)
    : ICommandHandler<UnbanUserCommand>
{
    public async Task<Result> Handle(UnbanUserCommand command, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var activeBan = await userModerationRepository.GetActiveBanAsync(command.UserId, now, ct);
        if (activeBan is null)
        {
            return Result.Failure(
                new Error("UserBan.NotFound", "No active ban exists for this user."),
                Outcome.NotFound);
        }

        activeBan.SoftDelete();

        var log = ContentModerationLog.Create(
            command.AdminUserId,
            activeBan.EntityType,
            activeBan.EntityId,
            ModerationAction.UnbanUser,
            $"Lifted ban for user {command.UserId}",
            now);

        await moderationLogRepository.AddAsync(log, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync("moderation:logs", ct);

        logger.LogInformation(
            "Admin {AdminUserId} lifted ban for user {UserId}",
            command.AdminUserId, command.UserId);

        return Result.Success();
    }
}
