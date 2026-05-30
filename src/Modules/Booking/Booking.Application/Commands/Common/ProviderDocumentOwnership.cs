using Booking.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Commands.Common;

internal static class ProviderDocumentOwnership
{
    public const string UnauthorizedErrorCode = "ProviderDocument.Unauthorized";
    public const string NotAProviderErrorCode = "ProviderDocument.NotAProvider";
    public static async Task<Result<Guid>> ResolveTourGuideIdAsync(
        ICurrentUser currentUser,
        IProviderDocumentRepository documentRepository,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure<Guid>(
                new Error(UnauthorizedErrorCode, "Authentication is required."),
                Outcome.Unauthorized);
        }

        var tourGuideId = await documentRepository
            .GetTourGuideIdByUserIdAsync(currentUser.UserId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (tourGuideId is null)
        {
            return Result.Failure<Guid>(
                new Error(NotAProviderErrorCode, "Current user is not registered as a tour guide."),
                Outcome.Forbidden);
        }

        return Result.Success<Guid>(tourGuideId.Value);
    }
}
