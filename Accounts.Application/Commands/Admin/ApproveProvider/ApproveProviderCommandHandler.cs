using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Admin.ApproveProvider;

public sealed class ApproveProviderCommandHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<ApproveProviderCommandHandler> logger)
    : ICommandHandler<ApproveProviderCommand, ApproveProviderResult>
{
    public async Task<Result<ApproveProviderResult>> Handle(
        ApproveProviderCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<ApproveProviderResult>.Failure(
                Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var adminId = currentUser.UserId.Value;

        var application = await providerApplicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return Result<ApproveProviderResult>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);

        var approveResult = application.Approve(adminId);
        if (approveResult.IsFailure)
            return Result<ApproveProviderResult>.Failure(approveResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.MyApplicationStatusTag(application.UserId), cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AdminProviderQueueTag, cancellationToken);

        logger.LogInformation("Provider application {ApplicationId} approved by admin {AdminId}",
            request.ApplicationId, adminId);

        return Result<ApproveProviderResult>.Success(
            new ApproveProviderResult(application.Id, application.UserId, application.ReviewedAt!.Value));
    }
}
