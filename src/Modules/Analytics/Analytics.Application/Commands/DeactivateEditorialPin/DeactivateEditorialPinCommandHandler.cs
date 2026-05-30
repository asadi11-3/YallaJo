using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.DeactivateEditorialPin;

public sealed class DeactivateEditorialPinCommandHandler(
    IEditorialPinRepository pinRepository,
    IAnalyticsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DeactivateEditorialPinCommandHandler> logger) : ICommandHandler<DeactivateEditorialPinCommand>
{
    public async Task<Result> Handle(DeactivateEditorialPinCommand request, CancellationToken ct)
    {
        var pin = await pinRepository.GetByIdAsync(request.PinId, ct).ConfigureAwait(false);
        if (pin is null)
            return Result.Failure(new Error("EditorialPin.NotFound", "Editorial pin was not found."), Outcome.NotFound);

        pin.Deactivate();
        pinRepository.Update(pin);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        await cache.RemoveByTagAsync($"analytics:pins:{pin.Context}", ct).ConfigureAwait(false);
        logger.LogInformation("Deactivated analytics editorial pin {PinId}", request.PinId);
        return Result.Success();
    }
}
