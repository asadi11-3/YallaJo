using ContentCore.Domain.Repositories;
using ContentCore.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using CategoryEntity = ContentCore.Domain.Entities.Category;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Category.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    ICategoryHierarchyService hierarchyService,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UpdateCategoryCommandHandler> logger)
    : ICommandHandler<UpdateCategoryCommand, UpdateCategoryResult>
{
    public async Task<Result<UpdateCategoryResult>> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var categoryResult = await GetCategoryForUpdateAsync(request.Id, cancellationToken);
            if (categoryResult.IsFailure)
            {
                return Result<UpdateCategoryResult>.Failure(
                    categoryResult.Error!,
                    categoryResult.Outcome);
            }

            var category = categoryResult.Value!;

            var slugResult = await EnsureSlugIsUniqueAsync(
                request.Slug,
                request.Id,
                cancellationToken);

            if (slugResult is not null)
            {
                return slugResult;
            }

            var parentValidationResult = await ValidateParentChangeAsync(
                category,
                request,
                cancellationToken);

            if (parentValidationResult is not null)
            {
                return parentValidationResult;
            }

            ApplyUpdates(category, request);

            var saveResult = await SaveChangesAsync(cancellationToken);
            if (saveResult is not null)
            {
                return saveResult;
            }

            await cache.RemoveByTagAsync("categories", cancellationToken);

            logger.LogInformation("Category updated: {CategoryId} (Slug={Slug})", category.Id, category.Slug);

            return Result<UpdateCategoryResult>.Success(
                new UpdateCategoryResult(
                    category.Id,
                    category.Name,
                    category.Slug));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<UpdateCategoryResult>.Failure(
                new Error(
                    "Request.Cancelled",
                    "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private async Task<Result<CategoryEntity>> GetCategoryForUpdateAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetAsync(
           filter: c => c.Id == categoryId,
           include: q => q.Include(c => c.Translations),
           asNoTracking: false,
           ct: cancellationToken);

        if (category is null)
        {
            return Result<CategoryEntity>.Failure(
                new Error(
                    "Category.NotFound",
                    $"Category '{categoryId}' was not found."),
                Outcome.NotFound);
        }

        return Result<CategoryEntity>.Success(category);
    }

    private async Task<Result<UpdateCategoryResult>?> EnsureSlugIsUniqueAsync(
        string slug,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (!await categoryRepository.AnyAsync(
                c => c.Slug == slug && c.Id != categoryId,
                cancellationToken))
        {
            return null;
        }

        return Result<UpdateCategoryResult>.Conflict(
            new Error(
                "Category.SlugConflict",
                $"A category with slug '{slug}' already exists."));
    }

    private async Task<Result<UpdateCategoryResult>?> ValidateParentChangeAsync(
        CategoryEntity category,
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.ParentCategoryId.HasValue ||
            request.ParentCategoryId == category.ParentCategoryId)
        {
            return null;
        }

        if (request.ParentCategoryId.Value == category.Id)
        {
            return Result<UpdateCategoryResult>.Failure(
                new Error(
                    "Category.InvalidParent",
                    "A category cannot be its own parent."),
                Outcome.Invalid);
        }

        var parent = await categoryRepository.GetByIdAsync(
            request.ParentCategoryId.Value,
            cancellationToken);

        if (parent is null)
        {
            return Result<UpdateCategoryResult>.Failure(
                new Error(
                    "Category.NotFound",
                    $"Parent category '{request.ParentCategoryId}' was not found."),
                Outcome.NotFound);
        }

        if (await hierarchyService.IsAncestorAsync(
                request.ParentCategoryId.Value,
                category.Id,
                cancellationToken))
        {
            return Result<UpdateCategoryResult>.Failure(
                new Error(
                    "Category.InvalidParent",
                    "A category cannot be moved under one of its descendants."),
                Outcome.Invalid);
        }

        var parentDepth = await hierarchyService.GetDepthAsync(
            request.ParentCategoryId,
            cancellationToken);

        var subtreeHeight = await hierarchyService.GetSubtreeHeightAsync(
            category.Id,
            cancellationToken);

        if (parentDepth + 1 + subtreeHeight > hierarchyService.MaxDepth)
        {
            return Result<UpdateCategoryResult>.Failure(
                new Error(
                    "Category.MaxDepthExceeded",
                    $"Cannot move category: maximum depth of {hierarchyService.MaxDepth} levels exceeded."),
                Outcome.Invalid);
        }

        category.ChangeParent(request.ParentCategoryId);
        return null;
    }

    private static void ApplyUpdates(
        CategoryEntity category,
        UpdateCategoryCommand request)
    {
        category.Update(
            request.Name,
            request.Slug,
            request.SourceLanguageCode);

        if (request.Icon is not null)
        {
            category.SetIcon(request.Icon);
        }

        if (request.SortOrder.HasValue)
        {
            category.SetSortOrder(request.SortOrder.Value);
        }

        ApplyTranslations(category, request);
    }

    private async Task<Result<UpdateCategoryResult>?> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<UpdateCategoryResult>.Conflict(
                new Error(
                    "Category.ConcurrencyConflict",
                    "This record was modified by another user. Please refresh and try again."));
        }
    }

    private static void ApplyTranslations(
        CategoryEntity category,
        UpdateCategoryCommand request)
    {
        if (request.Translations is null)
        {
            return;
        }

        foreach (var translation in request.Translations)
        {
            // Manual updates from admin UI are human-reviewed translations.
            // Mark as HumanReviewed so auto-translation handlers don't overwrite them.
            category.UpsertHumanReviewedTranslation(
                translation.LanguageId,
                translation.Name,
                translation.Slug);
        }
    }
}
