using Booking.Application.Commands.Common;
using Booking.Application.Commands.UploadProviderDocument;
using Booking.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetProviderDocuments;

public sealed class GetProviderDocumentsQueryHandler(
    IProviderDocumentRepository documentRepository,
    ICurrentUser currentUser,
    ILogger<GetProviderDocumentsQueryHandler> logger)
    : IQueryHandler<GetProviderDocumentsQuery, IReadOnlyList<ProviderDocumentDto>>
{
    public async Task<Result<IReadOnlyList<ProviderDocumentDto>>> Handle(
        GetProviderDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (!currentUser.IsAuthenticated
                || currentUser.UserId is null
                || currentUser.UserId.Value != request.UserId)
            {
                return Result.Failure<IReadOnlyList<ProviderDocumentDto>>(
                    new Error("ProviderDocument.Unauthorized", "Authentication is required."),
                    Outcome.Unauthorized);
            }

            var ownership = await ProviderDocumentOwnership
                .ResolveTourGuideIdAsync(currentUser, documentRepository, cancellationToken)
                .ConfigureAwait(false);

            if (!ownership.IsSuccess)
            {
                return Result.Failure<IReadOnlyList<ProviderDocumentDto>>(ownership.Errors[0], ownership.Outcome);
            }

            var documents = await documentRepository
                .GetForTourGuideAsync(ownership.Value, cancellationToken)
                .ConfigureAwait(false);

            var dtos = documents
                .Select(UploadProviderDocumentCommandHandler.ToDto)
                .ToList();

            logger.LogDebug(
                "GET /provider/documents returned {Count} docs for tour guide {TourGuideId}",
                dtos.Count,
                ownership.Value);

            return Result.Success<IReadOnlyList<ProviderDocumentDto>>(dtos);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<IReadOnlyList<ProviderDocumentDto>>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }
}
