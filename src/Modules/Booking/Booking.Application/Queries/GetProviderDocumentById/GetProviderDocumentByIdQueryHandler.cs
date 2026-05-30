using Booking.Application.Commands.Common;
using Booking.Application.Commands.UploadProviderDocument;
using Booking.Contracts.Authorization;
using Booking.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetProviderDocumentById;

public sealed class GetProviderDocumentByIdQueryHandler(
    IProviderDocumentRepository documentRepository,
    ICurrentUser currentUser,
    ILogger<GetProviderDocumentByIdQueryHandler> logger)
    : IQueryHandler<GetProviderDocumentByIdQuery, ProviderDocumentDto>
{
    public async Task<Result<ProviderDocumentDto>> Handle(
        GetProviderDocumentByIdQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            {
                return Result.Failure<ProviderDocumentDto>(
                    new Error("ProviderDocument.Unauthorized", "Authentication is required."),
                    Outcome.Unauthorized);
            }

            var isAdmin = currentUser.HasPermission(
                $"{BookingFeatures.AdminBookingDashboard}.{AppAction.Read}");

            if (isAdmin)
            {
                var adminLoaded = await documentRepository
                    .GetByIdAsync(request.Id, cancellationToken)
                    .ConfigureAwait(false);

                if (adminLoaded is null)
                {
                    return Result.Failure<ProviderDocumentDto>(
                        new Error("ProviderDocument.NotFound", "Provider document was not found."),
                        Outcome.NotFound);
                }

                logger.LogDebug(
                    "GET /provider/documents/{Id} served via admin override for user {UserId}",
                    request.Id,
                    currentUser.UserId);

                return Result.Success<ProviderDocumentDto>(UploadProviderDocumentCommandHandler.ToDto(adminLoaded));
            }

            var tourGuideId = await documentRepository
                .GetTourGuideIdByUserIdAsync(currentUser.UserId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (tourGuideId is null)
            {
                return Result.Failure<ProviderDocumentDto>(
                    new Error("ProviderDocument.NotFound", "Provider document was not found."),
                    Outcome.NotFound);
            }

            var document = await documentRepository
                .GetByIdForTourGuideAsync(request.Id, tourGuideId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (document is null)
            {
                return Result.Failure<ProviderDocumentDto>(
                    new Error("ProviderDocument.NotFound", "Provider document was not found."),
                    Outcome.NotFound);
            }

            return Result.Success<ProviderDocumentDto>(UploadProviderDocumentCommandHandler.ToDto(document));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<ProviderDocumentDto>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }
}
