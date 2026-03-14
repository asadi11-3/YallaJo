using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Tag.DeleteTag;

public sealed class DeleteTagCommandHandler(
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<DeleteTagCommand>
{
    public async Task<Result> Handle(DeleteTagCommand request, CancellationToken ct)
    {
        var tag = await tagRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
        if (tag is null)
            return Result.NotFound($"Tag '{request.Id}' not found.");

        tagRepository.Remove(tag);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
