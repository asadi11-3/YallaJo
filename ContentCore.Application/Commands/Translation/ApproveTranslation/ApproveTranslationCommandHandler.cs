using ContentCore.Application.Interfaces;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Translation.ApproveTranslation;

public sealed class ApproveTranslationCommandHandler(
    ITranslationCacheRepository translationCacheRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<ApproveTranslationCommand>
{
    public async Task<Result> Handle(
        ApproveTranslationCommand request,
        CancellationToken ct)
    {
        try
        {
            var cached = await translationCacheRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
            if (cached is null)
                return Result.Failure(
                    new Error("Translation.NotFound", "Translation was not found."),
                    Outcome.NotFound);

            cached.Approve();
            translationCacheRepository.Update(cached);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Translation.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
