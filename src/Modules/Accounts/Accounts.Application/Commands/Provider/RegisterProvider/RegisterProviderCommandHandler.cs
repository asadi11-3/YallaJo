using Accounts.Application.Caching;
using Accounts.Domain.Entities;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Provider.RegisterProvider;

public sealed class RegisterProviderCommandHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<RegisterProviderCommandHandler> logger)
    : ICommandHandler<RegisterProviderCommand, RegisterProviderResult>
{
    public async Task<Result<RegisterProviderResult>> Handle(
        RegisterProviderCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<RegisterProviderResult>.Failure(
                Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var userId = currentUser.UserId.Value;

        // Check if user already has an application
        var existing = await providerApplicationRepository.GetByUserIdAsync(userId, cancellationToken);
        if (existing is not null)
            return Result<RegisterProviderResult>.Failure(
                ProviderApplicationErrors.AlreadyExists, Outcome.Conflict);

        var registerResult = ProviderApplication.Register(
            userId,
            request.Type,
            request.BusinessName,
            request.ContactEmail,
            request.ContactPhone,
            request.Address,
            request.Description,
            request.TypeSpecificDataJson);

        if (registerResult.IsFailure)
            return Result<RegisterProviderResult>.Failure(
                registerResult.Error ?? Error.Failure("ProviderApplication.Register", "Unknown error occurred."),
                Outcome.UnprocessableEntity);

        var application = registerResult.Value;
        if (application is null)
            return Result<RegisterProviderResult>.Failure(
                Error.Failure("ProviderApplication.Register", "Unknown error occurred."),
                Outcome.UnprocessableEntity);

        await providerApplicationRepository.AddAsync(application, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.MyApplicationStatusTag(userId), cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AdminProviderQueueTag, cancellationToken);

        logger.LogInformation("Provider application registered for user {UserId}, ApplicationId: {ApplicationId}",
            userId, application.Id);

        return Result<RegisterProviderResult>.Success(new RegisterProviderResult(application.Id));
    }
}
