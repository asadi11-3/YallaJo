using ContentCore.Application.Queries.Tag.ListTags;
using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.Tag.GetTagById;

public sealed class GetTagByIdQueryHandler(ITagRepository tagRepository)
    : IQueryHandler<GetTagByIdQuery, TagDto>
{
    public async Task<Result<TagDto>> Handle(
        GetTagByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tag = await tagRepository.GetByIdAsync(request.Id, cancellationToken);
            if (tag is null) {
                return Result<TagDto>.Failure(
                   new Error("Tag.NotFound", $"Tag '{request.Id}' was not found."),
                   Outcome.NotFound);
            }

            var dto = new TagDto(tag.Id, tag.Name, tag.Slug, tag.IsActive);

            return Result<TagDto>.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<TagDto>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
