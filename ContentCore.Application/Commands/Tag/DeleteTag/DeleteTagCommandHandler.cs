using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Tag.DeleteTag;

public sealed class DeleteTagCommandHandler(
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork,
    IMemoryCache cache)
    : ICommandHandler<DeleteTagCommand>
{
    public async Task<Result> Handle(DeleteTagCommand request, CancellationToken ct)
    {
        var tag = await tagRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
        if (tag is null)
            return Result.NotFound($"Tag '{request.Id}' not found.");

        tagRepository.Remove(tag);
        await unitOfWork.SaveChangesAsync(ct);

        cache.Remove(ContentCoreCacheKeys.Tags(true));
        cache.Remove(ContentCoreCacheKeys.Tags(false));
        cache.Remove(ContentCoreCacheKeys.Tag(request.Id));

        return Result.Success();
    }
}
