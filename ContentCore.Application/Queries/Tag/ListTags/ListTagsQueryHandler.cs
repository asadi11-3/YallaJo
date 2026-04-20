using ContentCore.Application.Queries.Tag.Common;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.Tag.ListTags;

public sealed class ListTagsQueryHandler(
    ITagRepository tagRepository,
    ILogger<ListTagsQueryHandler> logger)
    : IQueryHandler<ListTagsQuery, IReadOnlyList<TagDto>>
{
    public async Task<Result<IReadOnlyList<TagDto>>> Handle(
        ListTagsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tags = await tagRepository.GetAllAsync(
                filter: request.ActiveOnly ? t => t.IsActive : null,
                orderBy: q => q.OrderBy(t => t.Name),
                ct: cancellationToken);

            var dtos = tags
                .Select(t => new TagDto(t.Id, t.Name, t.Slug, t.IsActive))
                .ToList() as IReadOnlyList<TagDto>;

            logger.LogDebug("ListTags returned {Count} tags", dtos.Count);

            return Result<IReadOnlyList<TagDto>>.Success(dtos);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyList<TagDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
