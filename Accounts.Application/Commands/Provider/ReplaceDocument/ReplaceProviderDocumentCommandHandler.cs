using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Provider.ReplaceDocument;

public sealed class ReplaceProviderDocumentCommandHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<ReplaceProviderDocumentCommandHandler> logger)
    : ICommandHandler<ReplaceProviderDocumentCommand, ReplaceProviderDocumentResult>
{
    public async Task<Result<ReplaceProviderDocumentResult>> Handle(
        ReplaceProviderDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<ReplaceProviderDocumentResult>.Failure(
                Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var userId = currentUser.UserId.Value;

        var application = await providerApplicationRepository.GetWithDocumentsByUserIdAsync(userId, cancellationToken);
        if (application is null)
            return Result<ReplaceProviderDocumentResult>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);

        var replaceResult = application.ReplaceDocument(
            request.DocumentId,
            request.FileUrl,
            request.FileName,
            request.FileSizeBytes,
            request.ExpiresAt);

        if (replaceResult.IsFailure)
            return Result<ReplaceProviderDocumentResult>.Failure(replaceResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.MyApplicationStatusTag(userId), cancellationToken);

        logger.LogInformation("Document {DocumentId} replaced in application {ApplicationId} for user {UserId}",
            request.DocumentId, application.Id, userId);

        return Result<ReplaceProviderDocumentResult>.Success(
            new ReplaceProviderDocumentResult(request.DocumentId, request.FileUrl));
    }
}
