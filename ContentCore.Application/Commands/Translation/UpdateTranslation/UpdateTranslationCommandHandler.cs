using ContentCore.Application.Interfaces;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

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
        try
        {
            var cached = await translationCacheRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
            if (cached is null)
                return Result<UpdateTranslationResult>.Failure(
                    new Error("Translation.NotFound", "Translation was not found."),
                    Outcome.NotFound);

            cached.UpdateTranslation(request.TranslatedText);
            translationCacheRepository.Update(cached);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<UpdateTranslationResult>.Conflict(
                    new Error("Translation.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            return Result<UpdateTranslationResult>.Success(
                new UpdateTranslationResult(cached.Id, cached.TranslatedText, cached.Status.ToString()));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<UpdateTranslationResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
