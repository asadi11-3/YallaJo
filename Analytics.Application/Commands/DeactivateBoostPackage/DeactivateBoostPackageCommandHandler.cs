using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.DeactivateBoostPackage;

public sealed class DeactivateBoostPackageCommandHandler(
    IBoostPackageRepository boostRepository,
    IAnalyticsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DeactivateBoostPackageCommandHandler> logger) : ICommandHandler<DeactivateBoostPackageCommand>
{
    public async Task<Result> Handle(DeactivateBoostPackageCommand request, CancellationToken ct)
    {
        var boost = await boostRepository.GetByIdAsync(request.BoostId, ct).ConfigureAwait(false);
        if (boost is null)
            return Result.Failure(new Error("BoostPackage.NotFound", "Boost package was not found."), Outcome.NotFound);

        boost.Deactivate();
        boostRepository.Update(boost);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        await cache.RemoveByTagAsync("analytics:boosts", ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync($"analytics:boosts:{boost.EntityKind}:{boost.EntityId:N}", ct).ConfigureAwait(false);
        logger.LogInformation("Deactivated analytics boost package {BoostId}", request.BoostId);
        return Result.Success();
    }
}
