using Auth.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.UnlinkExternalProvider;

public sealed class UnlinkExternalProviderCommandHandler(
    IExternalProviderRepository externalProviderRepository,
    IAuthUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<UnlinkExternalProviderCommand>
{
    public async Task<Result> Handle(UnlinkExternalProviderCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Unauthorized("Authentication is required.");

        // 1. Load provider link (tracked — will be mutated)
        var externalProvider = await externalProviderRepository.GetByIdAsync(
            request.ExternalProviderId,
            ct: ct,
            asNoTracking: false);

        if (externalProvider is null)
            return Result.NotFound($"External provider link {request.ExternalProviderId} was not found.");

        // 2. Security: caller may only unlink their own providers
        if (externalProvider.UserId != currentUser.UserId.Value)
            return Result.Forbidden("You are not authorized to unlink this external provider.");

        // 3. Deactivate (idempotent — already-inactive is fine)
        if (externalProvider.IsActive)
            externalProvider.Deactivate();

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
