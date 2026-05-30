using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.CreateBoostPackage;

public sealed class CreateBoostPackageCommandHandler(
    IBoostPackageRepository boostRepository,
    IAnalyticsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<CreateBoostPackageCommandHandler> logger) : ICommandHandler<CreateBoostPackageCommand, CreateBoostPackageResult>
{
    public async Task<Result<CreateBoostPackageResult>> Handle(CreateBoostPackageCommand request, CancellationToken ct)
    {
        var boost = BoostPackage.Create(request.ProviderId, request.EntityKind, request.EntityId, request.Multiplier, request.StartsAt, request.ExpiresAt);
        boostRepository.Add(boost);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        await cache.RemoveByTagAsync("analytics:boosts", ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync($"analytics:boosts:{request.EntityKind}:{request.EntityId:N}", ct).ConfigureAwait(false);
        logger.LogInformation("Created analytics boost package {BoostId} for {EntityKind}/{EntityId}", boost.Id, request.EntityKind, request.EntityId);
        return Result<CreateBoostPackageResult>.Success(new CreateBoostPackageResult(boost.Id));
    }
}
