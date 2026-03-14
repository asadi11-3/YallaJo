using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Tag.CreateTag;

public sealed class CreateTagCommandHandler(
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<CreateTagCommand, CreateTagResult>
{
    public async Task<Result<CreateTagResult>> Handle(
        CreateTagCommand request,
        CancellationToken ct)
    {
        var slugExists = await tagRepository.SlugExistsAsync(request.Slug, ct);
        if (slugExists)
            return Result<CreateTagResult>.Conflict($"Tag with slug '{request.Slug}' already exists.");

        var tag = Domain.Entities.Tag.Create(request.Name, request.Slug);

        await tagRepository.AddAsync(tag, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<CreateTagResult>.Created(
            new CreateTagResult(tag.Id, tag.Name, tag.Slug));
    }
}
