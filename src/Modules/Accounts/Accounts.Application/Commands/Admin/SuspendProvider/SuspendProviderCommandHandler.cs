using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Admin.SuspendProvider;

public sealed class SuspendProviderCommandHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<SuspendProviderCommandHandler> logger)
    : ICommandHandler<SuspendProviderCommand, SuspendProviderResult>
{
    public async Task<Result<SuspendProviderResult>> Handle(
        SuspendProviderCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<SuspendProviderResult>.Failure(
                Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var adminId = currentUser.UserId.Value;

        var application = await providerApplicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return Result<SuspendProviderResult>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);

        var suspendResult = application.Suspend(adminId, request.Reason);
        if (suspendResult.IsFailure)
            return Result<SuspendProviderResult>.Failure(suspendResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.MyApplicationStatusTag(application.UserId), cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AdminProviderQueueTag, cancellationToken);

        logger.LogInformation("Provider application {ApplicationId} suspended by admin {AdminId}. Reason: {Reason}",
            request.ApplicationId, adminId, request.Reason);

        return Result<SuspendProviderResult>.Success(
            new SuspendProviderResult(application.Id, application.UserId));
    }
}
