using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using LanguageEntity = ContentCore.Domain.Entities.Language;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Language.CreateLanguage;

public sealed class CreateLanguageCommandHandler(
    ILanguageRepository languageRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<CreateLanguageCommandHandler> logger)
    : ICommandHandler<CreateLanguageCommand, CreateLanguageResult>
{
    public async Task<Result<CreateLanguageResult>> Handle(
        CreateLanguageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedCode = request.Code.Trim().ToLowerInvariant();

            if (await languageRepository.AnyAsync(l => l.Code == normalizedCode, cancellationToken)) {
                return Result<CreateLanguageResult>.Conflict(
                   Error.Conflict("Language", $"Language with code '{normalizedCode}' already exists."));
            }

            var language = LanguageEntity.Create(
                normalizedCode, request.Name, request.NativeName, request.IsRtl);

            await languageRepository.AddAsync(language, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<CreateLanguageResult>.Conflict(
                    new Error(
                        "Language.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync("languages", cancellationToken);

            logger.LogInformation(
                "Language created: {LanguageId} (Code={Code}, Name={Name})",
                language.Id, language.Code, language.Name);

            return Result<CreateLanguageResult>.Created(
                new CreateLanguageResult(language.Id, language.Code, language.Name));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<CreateLanguageResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
