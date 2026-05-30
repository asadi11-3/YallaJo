using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.BanUser;

internal sealed class BanUserCommandHandler(
    IUserModerationRepository userModerationRepository,
    IContentModerationLogRepository moderationLogRepository,
    ISocialUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<BanUserCommandHandler> logger)
    : ICommandHandler<BanUserCommand>
{
    public async Task<Result> Handle(BanUserCommand command, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (command.ExpiresAt is not null && command.ExpiresAt <= now)
        {
            return Result.Failure(
                new Error("UserBan.InvalidExpiry", "Ban expiry must be in the future."),
                Outcome.Invalid);
        }

        var record = UserModerationRecord.Issue(
            command.UserId,
            command.EntityType,
            command.EntityId,
            ModerationAction.BanUser,
            command.Reason,
            command.ExpiresAt,
            command.AdminUserId,
            timeProvider);

        var log = ContentModerationLog.Create(
            command.AdminUserId,
            command.EntityType,
            command.EntityId,
            ModerationAction.BanUser,
            command.Reason,
            now);

        await userModerationRepository.AddAsync(record, ct);
        await moderationLogRepository.AddAsync(log, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync("moderation:logs", ct);

        logger.LogInformation(
            "Admin {AdminUserId} banned user {UserId} for {EntityType} {EntityId}",
            command.AdminUserId, command.UserId, command.EntityType, command.EntityId);

        return Result.Success();
    }
}
