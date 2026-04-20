using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Translation.BatchTranslate;

public sealed class BatchTranslateCommandHandler(
    ITranslationService translationService,
    IContentCoreUnitOfWork unitOfWork,
    ILogger<BatchTranslateCommandHandler> logger)
    : ICommandHandler<BatchTranslateCommand, BatchTranslateResult>
{
    public async Task<Result<BatchTranslateResult>> Handle(
        BatchTranslateCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = await translationService.BatchTranslateAsync(
                request.Texts,
                request.FromLanguageCode,
                request.ToLanguageCode,
                cancellationToken);

            // AutoSaveTranslationService stages cache entries but does not commit.
            // Commit here so all cache rows are persisted atomically with this operation.
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var items = results
                .Select(r => new BatchTranslateResultItem(
                    r.OriginalText,
                    r.TranslatedText,
                    r.FromLanguage,
                    r.ToLanguage,
                    r.Confidence))
                .ToList() as IReadOnlyList<BatchTranslateResultItem>;

            logger.LogInformation(
                "BatchTranslate completed: {Count} items ({From}→{To})",
                items.Count, request.FromLanguageCode, request.ToLanguageCode);

            return Result<BatchTranslateResult>.Success(new BatchTranslateResult(items));
        }
        catch (HttpRequestException)
        {
            return Result<BatchTranslateResult>.Failure(
                new Error("Translation.ServiceUnavailable", "Translation service is temporarily unavailable."),
                Outcome.ServerError);
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
