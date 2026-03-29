using ContentCore.Domain.Entities;
using ContentCore.Domain.Exceptions;
using ContentCore.Domain.Repositories;
using ContentCore.Domain.Services;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Category.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    ICategoryHierarchyService hierarchyService,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
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

    private async Task<Result<ContentCore.Domain.Entities.Category>> GetCategoryForUpdateAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdWithTranslationsAsync(
            categoryId,
            cancellationToken);

        if (category is null)
        {
            return Result<ContentCore.Domain.Entities.Category>.Failure(
                new Error(
                    "Category.NotFound",
                    $"Category '{categoryId}' was not found."),
                Outcome.NotFound);
        }

        return Result<ContentCore.Domain.Entities.Category>.Success(category);
    }

    private async Task<Result<UpdateCategoryResult>?> EnsureSlugIsUniqueAsync(
        string slug,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (!await categoryRepository.SlugExistsAsync(slug, categoryId, cancellationToken))
        {
            return null;
        }

        return Result<UpdateCategoryResult>.Conflict(
            new Error(
                "Category.SlugConflict",
                $"A category with slug '{slug}' already exists."));
    }

    private async Task<Result<UpdateCategoryResult>?> ValidateParentChangeAsync(
        ContentCore.Domain.Entities.Category category,
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

        if (await IsDescendantAsync(
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

    private async Task<bool> IsDescendantAsync(
        Guid candidateParentId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var currentId = candidateParentId;

        for (var i = 0; i < 20; i++)
        {
            var current = await categoryRepository.GetByIdAsync(
                currentId,
                cancellationToken);

            if (current is null || !current.ParentCategoryId.HasValue)
            {
                return false;
            }

            if (current.ParentCategoryId.Value == categoryId)
            {
                return true;
            }

            currentId = current.ParentCategoryId.Value;
        }

        return false;
    }

    private static void ApplyUpdates(
        ContentCore.Domain.Entities.Category category,
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
        catch (ContentCoreConcurrencyException)
        {
            return Result<UpdateCategoryResult>.Conflict(
                new Error(
                    "Category.ConcurrencyConflict",
                    "This record was modified by another user. Please refresh and try again."));
        }
    }

    private static void ApplyTranslations(
        ContentCore.Domain.Entities.Category category,
        UpdateCategoryCommand request)
    {
        if (request.Translations is null)
        {
            return;
        }

        foreach (var translation in request.Translations)
        {
            var existing = category.Translations
                .FirstOrDefault(x => x.LanguageId == translation.LanguageId);

            if (existing is not null)
            {
                category.UpdateTranslation(
                    translation.LanguageId,
                    translation.Name,
                    translation.Slug);
            }
            else
            {
                category.AddTranslation(
                    translation.LanguageId,
                    translation.Name,
                    translation.Slug);
            }
        }
    }
}