using ContentCore.Application.Queries.Tag.ListTags;
using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Queries.Tag.GetTagById;

public sealed class GetTagByIdQueryHandler(ITagRepository tagRepository)
    : IQueryHandler<GetTagByIdQuery, TagDto>
{
    public async Task<Result<TagDto>> Handle(
        GetTagByIdQuery request,
        CancellationToken ct)
    {
        var tag = await tagRepository.GetByIdAsync(request.Id, ct);
        if (tag is null)
            return Result<TagDto>.NotFound($"Tag '{request.Id}' not found.");

        var dto = new TagDto(tag.Id, tag.Name, tag.Slug, tag.IsActive);

        return Result<TagDto>.Success(dto);
    }
}
