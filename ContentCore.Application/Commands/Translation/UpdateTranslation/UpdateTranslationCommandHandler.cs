using ContentCore.Application.Interfaces;
using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Translation.UpdateTranslation;

public sealed class UpdateTranslationCommandHandler(
    ITranslationCacheRepository translationCacheRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<UpdateTranslationCommand, UpdateTranslationResult>
{
    public async Task<Result<UpdateTranslationResult>> Handle(
        UpdateTranslationCommand request,
        CancellationToken ct)
    {
        var cached = await translationCacheRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
        if (cached is null)
            return Result<UpdateTranslationResult>.NotFound("Translation not found.");

        cached.UpdateTranslation(request.TranslatedText);
        translationCacheRepository.Update(cached);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<UpdateTranslationResult>.Success(
            new UpdateTranslationResult(cached.Id, cached.TranslatedText, cached.Status.ToString()));
    }
}
