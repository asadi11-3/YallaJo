using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Provider.SubmitApplication;

public sealed class SubmitApplicationCommandHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<SubmitApplicationCommandHandler> logger)
    : ICommandHandler<SubmitApplicationCommand, SubmitApplicationResult>
{
    public async Task<Result<SubmitApplicationResult>> Handle(
        SubmitApplicationCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<SubmitApplicationResult>.Failure(
                Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var userId = currentUser.UserId.Value;

        var application = await providerApplicationRepository.GetWithDocumentsByUserIdAsync(userId, cancellationToken);
        if (application is null)
            return Result<SubmitApplicationResult>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);

        var submitResult = application.Submit();
        if (submitResult.IsFailure)
            return Result<SubmitApplicationResult>.Failure(submitResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.MyApplicationStatusTag(userId), cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AdminProviderQueueTag, cancellationToken);

        logger.LogInformation("Provider application submitted for user {UserId}, ApplicationId: {ApplicationId}",
            userId, application.Id);

        return Result<SubmitApplicationResult>.Success(new SubmitApplicationResult(application.Id, application.SubmittedAt!.Value));
    }
}
