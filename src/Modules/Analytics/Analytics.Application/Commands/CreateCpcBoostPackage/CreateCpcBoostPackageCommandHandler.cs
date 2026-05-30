using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.CreateCpcBoostPackage;

public sealed class CreateCpcBoostPackageCommandHandler(
    IBoostPackageRepository boostRepository,
    IAnalyticsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<CreateCpcBoostPackageCommandHandler> logger) : ICommandHandler<CreateCpcBoostPackageCommand, CreateCpcBoostPackageResult>
{
    public async Task<Result<CreateCpcBoostPackageResult>> Handle(CreateCpcBoostPackageCommand request, CancellationToken ct)
    {
        var bid = BoostPackage.CreateCpc(request.ProviderId, request.EntityKind, request.EntityId, request.BidPerClick, request.DailyBudgetCap, request.StartsAt, request.ExpiresAt);
        boostRepository.Add(bid);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        await cache.RemoveByTagAsync("analytics:boosts", ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync($"analytics:boosts:{request.EntityKind}:{request.EntityId:N}", ct).ConfigureAwait(false);
        logger.LogInformation("Created analytics CPC boost package {BoostId} for {EntityKind}/{EntityId}", bid.Id, request.EntityKind, request.EntityId);
        return Result<CreateCpcBoostPackageResult>.Success(new CreateCpcBoostPackageResult(bid.Id));
    }
}
