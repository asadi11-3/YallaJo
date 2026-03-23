using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.Tag.ListTags;

public sealed class ListTagsQueryHandler(ITagRepository tagRepository)
    : IQueryHandler<ListTagsQuery, IReadOnlyList<TagDto>>
{
    public async Task<Result<IReadOnlyList<TagDto>>> Handle(
        ListTagsQuery request,
        CancellationToken ct)
    {
        try
        {
            var tags = await tagRepository.GetAllAsync(
                filter: request.ActiveOnly ? t => t.IsActive : null,
                orderBy: q => q.OrderBy(t => t.Name),
                ct: ct);

            var dtos = tags
                .Select(t => new TagDto(t.Id, t.Name, t.Slug, t.IsActive))
                .ToList() as IReadOnlyList<TagDto>;

            return Result<IReadOnlyList<TagDto>>.Success(dtos);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<IReadOnlyList<TagDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
