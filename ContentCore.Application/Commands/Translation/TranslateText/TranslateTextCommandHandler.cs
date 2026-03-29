using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Translation.TranslateText;

public sealed class TranslateTextCommandHandler(
    ITranslationService translationService,
    IContentCoreUnitOfWork unitOfWork)
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

            // AutoSaveTranslationService stages the cache entry but does not commit.
            // Commit here so the cache row is persisted atomically with this operation.
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<TranslateTextResult>.Success(
                new TranslateTextResult(
                    result.OriginalText,
                    result.TranslatedText,
                    result.FromLanguage,
                    result.ToLanguage,
                    result.Confidence));
        }
        catch (HttpRequestException)
        {
            return Result<TranslateTextResult>.Failure(
                new Error("Translation.ServiceUnavailable", "Translation service is temporarily unavailable."),
                Outcome.ServerError);
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
