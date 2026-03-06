using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Language.CreateLanguage;

public sealed class CreateLanguageCommandHandler(
    ILanguageRepository languageRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<CreateLanguageCommand, CreateLanguageResult>
{
    public async Task<Result<CreateLanguageResult>> Handle(
        CreateLanguageCommand request,
        CancellationToken ct)
    {
        var normalizedCode = request.Code.Trim().ToLowerInvariant();

        if (await languageRepository.CodeExistsAsync(normalizedCode, ct))
            return Result<CreateLanguageResult>.Conflict(
                Error.Conflict("Language", $"Language with code '{normalizedCode}' already exists."));

        var language = Domain.Entities.Language.Create(
            normalizedCode,
            request.Name,
            request.NativeName,
            request.IsRtl);

        await languageRepository.AddAsync(language, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<CreateLanguageResult>.Created(
            new CreateLanguageResult(language.Id, language.Code, language.Name));
    }
}
