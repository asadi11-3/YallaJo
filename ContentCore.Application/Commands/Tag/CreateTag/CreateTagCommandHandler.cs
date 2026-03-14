using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Tag.CreateTag;

public sealed class CreateTagCommandHandler(
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork,
    IMemoryCache cache)
    : ICommandHandler<CreateTagCommand, CreateTagResult>
{
    public async Task<Result<CreateTagResult>> Handle(
        CreateTagCommand request,
        CancellationToken ct)
    {
        if (await tagRepository.SlugExistsAsync(request.Slug, ct))
            return Result<CreateTagResult>.Conflict(
                $"Tag with slug '{request.Slug}' already exists.");

        var tag = Domain.Entities.Tag.Create(request.Name, request.Slug);

        await tagRepository.AddAsync(tag, ct);
        await unitOfWork.SaveChangesAsync(ct);

        cache.Remove(ContentCoreCacheKeys.Tags(true));
        cache.Remove(ContentCoreCacheKeys.Tags(false));

        return Result<CreateTagResult>.Created(
            new CreateTagResult(tag.Id, tag.Name, tag.Slug));
    }
}
