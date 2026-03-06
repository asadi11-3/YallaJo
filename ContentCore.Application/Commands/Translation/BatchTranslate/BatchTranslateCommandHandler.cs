using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Translation.BatchTranslate;

public sealed class BatchTranslateCommandHandler(ITranslationService translationService)
    : ICommandHandler<BatchTranslateCommand, BatchTranslateResult>
{
    public async Task<Result<BatchTranslateResult>> Handle(
        BatchTranslateCommand request,
        CancellationToken ct)
    {
        var results = await translationService.BatchTranslateAsync(
            request.Texts,
            request.FromLanguageCode,
            request.ToLanguageCode,
            ct);

        var items = results
            .Select(r => new BatchTranslateResultItem(
                r.OriginalText,
                r.TranslatedText,
                r.FromLanguage,
                r.ToLanguage,
                r.Confidence))
            .ToList() as IReadOnlyList<BatchTranslateResultItem>;

        return Result<BatchTranslateResult>.Success(new BatchTranslateResult(items));
    }
}
