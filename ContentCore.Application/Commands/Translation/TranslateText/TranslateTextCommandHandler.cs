using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Translation.TranslateText;

public sealed class TranslateTextCommandHandler(
    ITranslationService translationService,
    ILogger<TranslateTextCommandHandler> logger)
    : ICommandHandler<TranslateTextCommand, TranslateTextResult>
{
    public async Task<Result<TranslateTextResult>> Handle(
        TranslateTextCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await translationService.TranslateAsync(
                request.Text,
                request.FromLanguageCode,
                request.ToLanguageCode,
                cancellationToken);

            if (result.IsFailure)
                return Result<TranslateTextResult>.Fail(result.Outcome, result.Errors.ToArray());

            var value = result.Value;
            logger.LogInformation(
                "TranslateText: {From}→{To}, confidence={Confidence}",
                value.FromLanguage, value.ToLanguage, value.Confidence);

            return Result<TranslateTextResult>.Success(
                new TranslateTextResult(
                    value.OriginalText,
                    value.TranslatedText,
                    value.FromLanguage,
                    value.ToLanguage,
                    value.Confidence));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<TranslateTextResult>.Failure(
                new Error("Translation.Timeout", "Translation request timed out."),
                Outcome.ServerError);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<TranslateTextResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
