using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Provider.AddDocument;

public sealed class AddProviderDocumentCommandHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<AddProviderDocumentCommandHandler> logger)
    : ICommandHandler<AddProviderDocumentCommand, AddProviderDocumentResult>
{
    public async Task<Result<AddProviderDocumentResult>> Handle(
        AddProviderDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<AddProviderDocumentResult>.Failure(
                Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var userId = currentUser.UserId.Value;

        var application = await providerApplicationRepository.GetWithDocumentsByUserIdAsync(userId, cancellationToken);
        if (application is null)
            return Result<AddProviderDocumentResult>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);

        var addResult = application.AddDocument(
            request.DocumentType,
            request.FileUrl,
            request.FileName,
            request.FileSizeBytes,
            request.ExpiresAt);

        if (addResult.IsFailure)
            return Result<AddProviderDocumentResult>.Failure(addResult.Error, Outcome.UnprocessableEntity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.MyApplicationStatusTag(userId), cancellationToken);

        logger.LogInformation("Document {DocumentType} added to application {ApplicationId} for user {UserId}",
            request.DocumentType, application.Id, userId);

        return Result<AddProviderDocumentResult>.Success(
            new AddProviderDocumentResult(addResult.Value.Id, addResult.Value.DocumentType, addResult.Value.FileUrl));
    }
}
