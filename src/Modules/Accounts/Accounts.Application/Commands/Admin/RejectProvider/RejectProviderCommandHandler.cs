using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Admin.RejectProvider;

public sealed class RejectProviderCommandHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<RejectProviderCommandHandler> logger)
    : ICommandHandler<RejectProviderCommand, RejectProviderResult>
{
    public async Task<Result<RejectProviderResult>> Handle(
        RejectProviderCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<RejectProviderResult>.Failure(
                Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var adminId = currentUser.UserId.Value;

        var application = await providerApplicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return Result<RejectProviderResult>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);

        var rejectResult = application.Reject(adminId, request.Reason);
        if (rejectResult.IsFailure)
            return Result<RejectProviderResult>.Failure(rejectResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.MyApplicationStatusTag(application.UserId), cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AdminProviderQueueTag, cancellationToken);

        logger.LogInformation("Provider application {ApplicationId} rejected by admin {AdminId}. Reason: {Reason}",
            request.ApplicationId, adminId, request.Reason);

        return Result<RejectProviderResult>.Success(
            new RejectProviderResult(application.Id, application.UserId, application.ReviewedAt!.Value, application.CoolingPeriodEndsAt));
    }
}
