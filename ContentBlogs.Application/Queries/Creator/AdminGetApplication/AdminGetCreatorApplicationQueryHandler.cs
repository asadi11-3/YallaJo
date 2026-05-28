using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.AdminGetApplication;

public sealed class AdminGetCreatorApplicationQueryHandler(
    ICreatorApplicationRepository applicationRepository,
    ILogger<AdminGetCreatorApplicationQueryHandler> logger)
    : IQueryHandler<AdminGetCreatorApplicationQuery, CreatorApplicationDto>
{
    public async Task<Result<CreatorApplicationDto>> Handle(
        AdminGetCreatorApplicationQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var application = await applicationRepository
                .GetByIdAsync(request.ApplicationId, cancellationToken)
                .ConfigureAwait(false);

            if (application is null)
            {
                return Result.Failure<CreatorApplicationDto>(
                    CreatorApplicationErrors.NotFound, Outcome.NotFound);
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

            return Result<CreatorApplicationDto>.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<CreatorApplicationDto>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
