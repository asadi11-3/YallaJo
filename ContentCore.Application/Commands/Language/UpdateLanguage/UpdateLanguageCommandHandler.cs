using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Language.UpdateLanguage;

public sealed class UpdateLanguageCommandHandler(
    ILanguageRepository languageRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<UpdateLanguageCommand, UpdateLanguageResult>
{
    public async Task<Result<UpdateLanguageResult>> Handle(
        UpdateLanguageCommand request,
        CancellationToken ct)
    {
        var language = await languageRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
        if (language is null)
            return Result<UpdateLanguageResult>.NotFound("Language not found.");

        language.Update(request.Name, request.NativeName, request.IsRtl);

        if (request.IsActive)
            language.Activate();
        else
            language.Deactivate();

        languageRepository.Update(language);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<UpdateLanguageResult>.Success(
            new UpdateLanguageResult(language.Id, language.Name, language.NativeName, language.IsRtl, language.IsActive));
    }
}
