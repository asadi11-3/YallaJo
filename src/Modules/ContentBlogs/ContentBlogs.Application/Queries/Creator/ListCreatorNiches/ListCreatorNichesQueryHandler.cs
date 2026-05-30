using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.ListCreatorNiches;

public sealed class ListCreatorNichesQueryHandler(
    ICreatorNicheRepository nicheRepository,
    ILogger<ListCreatorNichesQueryHandler> logger)
    : IQueryHandler<ListCreatorNichesQuery, IReadOnlyList<CreatorNicheDto>>
{
    public async Task<Result<IReadOnlyList<CreatorNicheDto>>> Handle(
        ListCreatorNichesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var niches = await nicheRepository
                .GetAllActiveAsync(cancellationToken)
                .ConfigureAwait(false);

            var dtos = niches
                .Select(n => new CreatorNicheDto(
                    n.Id, n.Name, n.Slug, n.Description, n.SortOrder, n.IsActive))
                .ToList();

            return Result<IReadOnlyList<CreatorNicheDto>>.Success(dtos);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<IReadOnlyList<CreatorNicheDto>>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
