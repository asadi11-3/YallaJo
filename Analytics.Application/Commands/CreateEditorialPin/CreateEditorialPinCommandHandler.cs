using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.CreateEditorialPin;

public sealed class CreateEditorialPinCommandHandler(
    IEditorialPinRepository pinRepository,
    IAnalyticsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<CreateEditorialPinCommandHandler> logger) : ICommandHandler<CreateEditorialPinCommand, CreateEditorialPinResult>
{
    public async Task<Result<CreateEditorialPinResult>> Handle(CreateEditorialPinCommand request, CancellationToken ct)
    {
        var pin = EditorialPin.Create(request.EntityKind, request.EntityId, request.Position, request.Context, request.BadgeText, request.ExpiresAt);
        pinRepository.Add(pin);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        await cache.RemoveByTagAsync($"analytics:pins:{request.Context}", ct).ConfigureAwait(false);
        logger.LogInformation("Created analytics editorial pin {PinId} for {EntityKind}/{EntityId}", pin.Id, request.EntityKind, request.EntityId);
        return Result<CreateEditorialPinResult>.Success(new CreateEditorialPinResult(pin.Id));
    }
}
