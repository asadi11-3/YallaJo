using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Provider.ReapplyProvider;

public sealed class ReapplyProviderCommandHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<ReapplyProviderCommandHandler> logger)
    : ICommandHandler<ReapplyProviderCommand, ReapplyProviderResult>
{
    public async Task<Result<ReapplyProviderResult>> Handle(
        ReapplyProviderCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<ReapplyProviderResult>.Failure(
                Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var userId = currentUser.UserId.Value;

        var application = await providerApplicationRepository.GetWithDocumentsByUserIdAsync(userId, cancellationToken);
        if (application is null)
            return Result<ReapplyProviderResult>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);

        var reapplyResult = application.Reapply();
        if (reapplyResult.IsFailure)
            return Result<ReapplyProviderResult>.Failure(reapplyResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<ReapplyProviderResult>.Failure(
                new Error("ProviderApplication.ConcurrencyConflict", "The provider application was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(AccountsCacheKeys.MyApplicationStatusTag(userId), cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AdminProviderQueueTag, cancellationToken);

        logger.LogInformation("Provider application reapplied for user {UserId}, ApplicationId: {ApplicationId}, ReapplicationCount: {Count}",
            userId, application.Id, application.ReapplicationCount);

        return Result<ReapplyProviderResult>.Success(new ReapplyProviderResult(application.Id, DateTime.UtcNow));
    }
}
