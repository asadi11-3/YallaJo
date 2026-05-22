using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Admin.RequestMoreDocs;

public sealed class RequestMoreDocsCommandHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<RequestMoreDocsCommandHandler> logger)
    : ICommandHandler<RequestMoreDocsCommand, RequestMoreDocsResult>
{
    public async Task<Result<RequestMoreDocsResult>> Handle(
        RequestMoreDocsCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<RequestMoreDocsResult>.Failure(
                Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var adminId = currentUser.UserId.Value;

        var application = await providerApplicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return Result<RequestMoreDocsResult>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);

        var requestResult = application.RequestMoreDocs(adminId, request.MissingDocumentTypes);
        if (requestResult.IsFailure)
            return Result<RequestMoreDocsResult>.Failure(requestResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.MyApplicationStatusTag(application.UserId), cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.AdminProviderQueueTag, cancellationToken);

        logger.LogInformation("More docs requested for application {ApplicationId} by admin {AdminId}",
            request.ApplicationId, adminId);

        return Result<RequestMoreDocsResult>.Success(new RequestMoreDocsResult(application.Id));
    }
}
