using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Admin.ReinstateProvider;

public sealed class ReinstateProviderCommandHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<ReinstateProviderCommandHandler> logger)
    : ICommandHandler<ReinstateProviderCommand, ReinstateProviderResult>
{
    public async Task<Result<ReinstateProviderResult>> Handle(
        ReinstateProviderCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<ReinstateProviderResult>.Failure(
                Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var adminId = currentUser.UserId.Value;

        var application = await providerApplicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return Result<ReinstateProviderResult>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);

        var reinstateResult = application.Reinstate(adminId);
        if (reinstateResult.IsFailure)
            return Result<ReinstateProviderResult>.Failure(reinstateResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.MyApplicationStatusTag(application.UserId), cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AdminProviderQueueTag, cancellationToken);

        logger.LogInformation("Provider application {ApplicationId} reinstated by admin {AdminId}",
            request.ApplicationId, adminId);

        return Result<ReinstateProviderResult>.Success(
            new ReinstateProviderResult(application.Id, application.UserId));
    }
}
