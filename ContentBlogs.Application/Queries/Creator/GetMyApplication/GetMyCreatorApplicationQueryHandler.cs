using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.GetMyApplication;

public sealed class GetMyCreatorApplicationQueryHandler(
    ICreatorApplicationRepository applicationRepository,
    ICurrentUser currentUser,
    ILogger<GetMyCreatorApplicationQueryHandler> logger)
    : IQueryHandler<GetMyCreatorApplicationQuery, CreatorApplicationDto?>
{
    public async Task<Result<CreatorApplicationDto?>> Handle(
        GetMyCreatorApplicationQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result.Failure<CreatorApplicationDto?>(
                    new Error("Auth.UserIdMissing", "Authenticated user id is missing."),
                    Outcome.Unauthorized);
            }

            var application = await applicationRepository
                .GetLatestByUserIdAsync(currentUser.UserId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (application is null)
            {
                return Result<CreatorApplicationDto?>.Success(null);
            }

            var dto = new CreatorApplicationDto(
                application.Id,
                application.ApplicantUserId,
                application.Bio,
                application.Status,
                application.Source,
                application.PortfolioUrls,
                application.SampleWorkUrls,
                application.NicheIds,
                application.FreeTags,
                application.LanguageIds,
                application.PreferredRegionIds,
                application.SocialHandles,
                application.AdminNote,
                application.ReapplicationCount,
                application.LastRejectedAt,
                application.CreatedAt,
                application.ReviewedAt);

            return Result<CreatorApplicationDto?>.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<CreatorApplicationDto?>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
