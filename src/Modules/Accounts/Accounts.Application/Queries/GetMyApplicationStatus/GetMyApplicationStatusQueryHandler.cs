using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.GetMyApplicationStatus;

public sealed class GetMyApplicationStatusQueryHandler(
    IProviderApplicationRepository providerApplicationRepository,
    ICurrentUser currentUser,
    ILogger<GetMyApplicationStatusQueryHandler> logger)
    : IQueryHandler<GetMyApplicationStatusQuery, GetMyApplicationStatusResult>
{
    public async Task<Result<GetMyApplicationStatusResult>> Handle(
        GetMyApplicationStatusQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<GetMyApplicationStatusResult>.Failure(
                Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        if (currentUser.UserId.Value != request.UserId)
            return Result<GetMyApplicationStatusResult>.Failure(
                Error.Forbidden("You may only access your own application."), Outcome.Forbidden);

        var application = await providerApplicationRepository.GetWithDocumentsByUserIdAsync(request.UserId, cancellationToken);
        if (application is null)
            return Result<GetMyApplicationStatusResult>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);

        var docs = application.Documents.Select(d => new DocumentSummary(
            d.Id, d.DocumentType, d.FileUrl, d.FileName, d.ExpiresAt)).ToList();

        logger.LogDebug("Fetched application status for user {UserId}, status: {Status}",
            request.UserId, application.Status);

        return Result<GetMyApplicationStatusResult>.Success(new GetMyApplicationStatusResult(
            ApplicationId: application.Id,
            Type: application.Type,
            BusinessName: application.BusinessName,
            Status: application.Status,
            SubmittedAt: application.SubmittedAt,
            ReviewedAt: application.ReviewedAt,
            RejectionReason: application.RejectionReason,
            SuspensionReason: application.SuspensionReason,
            ReapplicationCount: application.ReapplicationCount,
            CoolingPeriodEndsAt: application.CoolingPeriodEndsAt,
            Documents: docs));
    }
}
