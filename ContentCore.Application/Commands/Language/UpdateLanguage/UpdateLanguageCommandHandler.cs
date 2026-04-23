using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Language.UpdateLanguage;

public sealed class UpdateLanguageCommandHandler(
    ILanguageRepository languageRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UpdateLanguageCommandHandler> logger)
    : ICommandHandler<UpdateLanguageCommand, UpdateLanguageResult>
{
    public async Task<Result<UpdateLanguageResult>> Handle(
        UpdateLanguageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var language = await languageRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);
            if (language is null) {
                return Result<UpdateLanguageResult>.Failure(
                  new Error("Language.NotFound", $"Language '{request.Id}' was not found."),
                  Outcome.NotFound);
            }

            language.Update(request.Name, request.NativeName, request.IsRtl);

            // Guard: only call Activate/Deactivate when state ACTUALLY changes.
            // Language.Activate() always raises LanguageActivatedDomainEvent → outbox write.
            // Calling it when already active causes duplicate integration events downstream.
            if (request.IsActive && !language.IsActive)
                language.Activate();
            else if (!request.IsActive && language.IsActive)
                language.Deactivate();

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<UpdateLanguageResult>.Conflict(
                    new Error(
                        "Language.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync("languages", cancellationToken);
            await cache.RemoveByTagAsync($"language:{language.Id}", cancellationToken);

            logger.LogInformation(
                "Language updated: {LanguageId} (Code={Code}, IsActive={IsActive})",
                language.Id, language.Code, language.IsActive);

            return Result<UpdateLanguageResult>.Success(
                new UpdateLanguageResult(language.Id, language.Name, language.NativeName, language.IsRtl, language.IsActive));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<UpdateLanguageResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
