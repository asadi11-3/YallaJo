using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Tag.UpdateTag;

public sealed class UpdateTagCommandHandler(
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork,
    IMemoryCache cache)
    : ICommandHandler<UpdateTagCommand, UpdateTagResult>
{
    public async Task<Result<UpdateTagResult>> Handle(
        UpdateTagCommand request,
        CancellationToken ct)
    {
        var tag = await tagRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
        if (tag is null)
            return Result<UpdateTagResult>.NotFound($"Tag '{request.Id}' not found.");

        if (await tagRepository.SlugExistsAsync(request.Slug, request.Id, ct))
            return Result<UpdateTagResult>.Conflict(
                $"Tag with slug '{request.Slug}' already exists.");

        tag.Update(request.Name, request.Slug);
        await unitOfWork.SaveChangesAsync(ct);

        cache.Remove(ContentCoreCacheKeys.Tags(true));
        cache.Remove(ContentCoreCacheKeys.Tags(false));
        cache.Remove(ContentCoreCacheKeys.Tag(request.Id));

        return Result<UpdateTagResult>.Success(
            new UpdateTagResult(tag.Id, tag.Name, tag.Slug));
    }
}
