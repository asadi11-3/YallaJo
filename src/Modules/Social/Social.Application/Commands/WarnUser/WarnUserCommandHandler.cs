using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.WarnUser;

internal sealed class WarnUserCommandHandler(
    IUserModerationRepository userModerationRepository,
    IContentModerationLogRepository moderationLogRepository,
    ISocialUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<WarnUserCommandHandler> logger)
    : ICommandHandler<WarnUserCommand>
{
    public async Task<Result> Handle(WarnUserCommand command, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var record = UserModerationRecord.Issue(
            command.UserId,
            command.EntityType,
            command.EntityId,
            ModerationAction.WarnUser,
            command.Reason,
            expiresAt: null,
            command.AdminUserId,
            timeProvider);

        var log = ContentModerationLog.Create(
            command.AdminUserId,
            command.EntityType,
            command.EntityId,
            ModerationAction.WarnUser,
            command.Reason,
            now);

        await userModerationRepository.AddAsync(record, ct);
        await moderationLogRepository.AddAsync(log, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync("moderation:logs", ct);

        logger.LogInformation(
            "Admin {AdminUserId} warned user {UserId} for {EntityType} {EntityId}",
            command.AdminUserId, command.UserId, command.EntityType, command.EntityId);

        return Result.Success();
    }
}
