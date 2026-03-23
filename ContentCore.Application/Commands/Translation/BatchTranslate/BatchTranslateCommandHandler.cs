using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Translation.BatchTranslate;

public sealed class BatchTranslateCommandHandler(ITranslationService translationService)
    : ICommandHandler<BatchTranslateCommand, BatchTranslateResult>
{
    public async Task<Result<BatchTranslateResult>> Handle(
        BatchTranslateCommand request,
        CancellationToken ct)
    {
        try
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
        catch (HttpRequestException)
        {
            return Result<BatchTranslateResult>.Failure(
                new Error("Translation.ServiceUnavailable", "Translation service is temporarily unavailable."),
                Outcome.ServerError);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return Result<BatchTranslateResult>.Failure(
                new Error("Translation.Timeout", "Translation request timed out."),
                Outcome.ServerError);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<BatchTranslateResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
