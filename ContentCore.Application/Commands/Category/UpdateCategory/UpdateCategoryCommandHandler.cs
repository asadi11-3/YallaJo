using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Category.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<UpdateCategoryCommand, UpdateCategoryResult>
{
    public async Task<Result<UpdateCategoryResult>> Handle(
        UpdateCategoryCommand request,
        CancellationToken ct)
    {
        try
        {
            var category = await categoryRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);

            if (category is null)
                return Result<UpdateCategoryResult>.Failure(
                    new Error("Category.NotFound", $"Category '{request.Id}' was not found."),
                    Outcome.NotFound);

            // Validate depth if parent is being changed
            if (request.ParentCategoryId.HasValue &&
                request.ParentCategoryId != category.ParentCategoryId)
            {
                var parent = await categoryRepository.GetByIdAsync(request.ParentCategoryId.Value, ct);
                if (parent is null)
                    return Result<UpdateCategoryResult>.Failure(
                        new Error("Category.NotFound",
                            $"Parent category '{request.ParentCategoryId}' was not found."),
                        Outcome.NotFound);

                if (parent.ParentCategoryId.HasValue)
                {
                    var grandparent = await categoryRepository.GetByIdAsync(parent.ParentCategoryId.Value, ct);
                    if (grandparent?.ParentCategoryId.HasValue == true)
                        return Result<UpdateCategoryResult>.Failure(
                            new Error("Category.MaxDepthExceeded",
                                "Cannot move category: maximum depth of 3 levels exceeded."),
                            Outcome.Invalid);
                }

                category.ChangeParent(request.ParentCategoryId);
            }

            // Update name, slug, source language (raises CategoryUpdatedDomainEvent for auto-translation)
            category.Update(request.Name, request.Slug, request.SourceLanguageCode);

            if (request.Icon is not null)
                category.SetIcon(request.Icon);

            if (request.SortOrder.HasValue)
                category.SetSortOrder(request.SortOrder.Value);

            // Apply manual translation overrides if provided
            if (request.Translations is not null)
            {
                foreach (var t in request.Translations)
                {
                    var existing = category.Translations
                        .FirstOrDefault(x => x.LanguageId == t.LanguageId);

                    if (existing is not null)
                        category.UpdateTranslation(t.LanguageId, t.Name, t.Slug);
                    else
                        category.AddTranslation(t.LanguageId, t.Name, t.Slug);
                }
            }

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<UpdateCategoryResult>.Conflict(
                    new Error("Category.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync("categories", ct);

            return Result<UpdateCategoryResult>.Success(
                new UpdateCategoryResult(category.Id, category.Name, category.Slug));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<UpdateCategoryResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
