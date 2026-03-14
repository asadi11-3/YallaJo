using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Tag.UpdateTag;

public sealed class UpdateTagCommandHandler(
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<UpdateTagCommand, UpdateTagResult>
{
    public async Task<Result<UpdateTagResult>> Handle(
        UpdateTagCommand request,
        CancellationToken ct)
    {
        var tag = await tagRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
        if (tag is null)
            return Result<UpdateTagResult>.NotFound($"Tag '{request.Id}' not found.");

        var slugExists = await tagRepository.SlugExistsAsync(request.Slug, request.Id, ct);
        if (slugExists)
            return Result<UpdateTagResult>.Conflict($"Tag with slug '{request.Slug}' already exists.");

        tag.Update(request.Name, request.Slug);

        await unitOfWork.SaveChangesAsync(ct);

        return Result<UpdateTagResult>.Success(
            new UpdateTagResult(tag.Id, tag.Name, tag.Slug));
    }
}
