using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.LinkExternalProvider;

public sealed class LinkExternalProviderCommandHandler(
    IExternalProviderRepository externalProviderRepository,
    IAuthUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<LinkExternalProviderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(LinkExternalProviderCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Failure<Guid>(
                Error.Unauthorized("Authentication is required."),
                Outcome.Unauthorized);

        var userId = currentUser.UserId.Value;
        var normalizedProvider = request.Provider.Trim().ToLowerInvariant();

        // 1. Guard: this user already has an active link for this provider
        var alreadyLinked = await externalProviderRepository.AnyAsync(
            ep => ep.UserId == userId
               && ep.Provider == normalizedProvider
               && ep.IsActive,
            ct);

        if (alreadyLinked)
            return Result<Guid>.Conflict(
                Error.Conflict(
                    "ExternalProvider.AlreadyLinked",
                    $"An active {request.Provider} account is already linked to your profile."));

        // 2. Guard: this provider account isn't already claimed by another user
        var takenByOther = await externalProviderRepository.AnyAsync(
            ep => ep.ProviderUserId == request.ProviderUserId
               && ep.Provider == normalizedProvider
               && ep.IsActive,
            ct);

        if (takenByOther)
            return Result<Guid>.Conflict(
                Error.Conflict(
                    "ExternalProvider.ProviderIdTaken",
                    $"This {request.Provider} account is already linked to another user."));

        // 3. Create and persist
        var externalProvider = ExternalProvider.Create(
            userId,
            normalizedProvider,
            request.ProviderUserId,
            request.ProviderEmail);

        await externalProviderRepository.AddAsync(externalProvider, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(externalProvider.Id);
    }
}
