using ContentCore.Application.Interfaces;
using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Translation.ApproveTranslation;

public sealed class ApproveTranslationCommandHandler(
    ITranslationCacheRepository translationCacheRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<ApproveTranslationCommand>
{
    public async Task<Result> Handle(
        ApproveTranslationCommand request,
        CancellationToken ct)
    {
        var cached = await translationCacheRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
        if (cached is null)
            return Result.NotFound("Translation not found.");

        cached.Approve();
        translationCacheRepository.Update(cached);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
