using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Translation.TranslateText;

public sealed class TranslateTextCommandHandler(ITranslationService translationService)
    : ICommandHandler<TranslateTextCommand, TranslateTextResult>
{
    public async Task<Result<TranslateTextResult>> Handle(
        TranslateTextCommand request,
        CancellationToken ct)
    {
        var result = await translationService.TranslateAsync(
            request.Text,
            request.FromLanguageCode,
            request.ToLanguageCode,
            ct);

        return Result<TranslateTextResult>.Success(
            new TranslateTextResult(
                result.OriginalText,
                result.TranslatedText,
                result.FromLanguage,
                result.ToLanguage,
                result.Confidence));
    }
}
