using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Translation.BatchTranslate;

public sealed class BatchTranslateCommandHandler(
    ITranslationService translationService,
    ILogger<BatchTranslateCommandHandler> logger)
    : ICommandHandler<BatchTranslateCommand, BatchTranslateResult>
{
    public async Task<Result<BatchTranslateResult>> Handle(
        BatchTranslateCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await translationService.BatchTranslateAsync(
                request.Texts,
                request.FromLanguageCode,
                request.ToLanguageCode,
                cancellationToken);

            if (result.IsFailure)
                return Result<BatchTranslateResult>.Fail(result.Outcome, result.Errors.ToArray());

            var items = result.Value
                .Select(r => new BatchTranslateResultItem(
                    r.OriginalText,
                    r.TranslatedText,
                    r.FromLanguage,
                    r.ToLanguage,
                    r.Confidence))
                .ToList();

            logger.LogInformation(
                "BatchTranslate completed: {Count} items ({From}→{To})",
                items.Count, request.FromLanguageCode, request.ToLanguageCode);

            return Result<BatchTranslateResult>.Success(new BatchTranslateResult(items));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<BatchTranslateResult>.Failure(
                new Error("Translation.Timeout", "Translation request timed out."),
                Outcome.ServerError);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<BatchTranslateResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
