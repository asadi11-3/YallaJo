using ContentCore.Domain.Exceptions;
using ContentCore.Domain.Repositories;
using ContentCore.Domain.Services;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Category.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    ICategoryHierarchyService hierarchyService,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<CreateCategoryCommand, CreateCategoryResult>
{
    public async Task<Result<CreateCategoryResult>> Handle(
      CreateCategoryCommand request,
      CancellationToken cancellationToken)
    {
        try
        {
            var slug = request.Slug ?? Domain.Entities.Category.GenerateSlug(request.Name);

            if (await categoryRepository.SlugExistsAsync(slug, cancellationToken))
            {
                return Result<CreateCategoryResult>.Conflict(
                    new Error(
                        "Category.SlugConflict",
                        $"A category with Slug '{slug}' already exists."));
            }

            var parentValidationResult = await ValidateParentAsync(request, cancellationToken);
            if (parentValidationResult is not null)
            {
                return parentValidationResult;
            }

            var category = Domain.Entities.Category.Create(
                request.Name,
                slug,
                request.SourceLanguageCode,
                request.ParentCategoryId,
                request.SortOrder);

            await categoryRepository.AddAsync(category, cancellationToken);

            var saveResult = await SaveChangesAsync(cancellationToken);
            if (saveResult is not null)
            {
                return saveResult;
            }

            await cache.RemoveByTagAsync("categories", cancellationToken);

            return Result<CreateCategoryResult>.Created(
                new CreateCategoryResult(category.Id, category.Name, category.Slug));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<CreateCategoryResult>.Failure(
                new Error(
                    "Request.Cancelled",
                    "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private async Task<Result<CreateCategoryResult>?> ValidateParentAsync(
        CreateCategoryCommand request,
        CancellationToken ct)
    {
        if (!request.ParentCategoryId.HasValue)
        {
            return null;
        }

        var parent = await categoryRepository.GetByIdAsync(request.ParentCategoryId.Value, ct);
        if (parent is null)
        {
            return Result<CreateCategoryResult>.Failure(
                new Error(
                    "Category.NotFound",
                    $"Parent category '{request.ParentCategoryId}' was not found."),
                Outcome.NotFound);
        }

        var parentDepth = await hierarchyService.GetDepthAsync(request.ParentCategoryId, ct);
        if (parentDepth + 1 > hierarchyService.MaxDepth)
        {
            return Result<CreateCategoryResult>.Failure(
                new Error(
                    "Category.MaxDepthExceeded",
                    $"Cannot create category: maximum depth of {hierarchyService.MaxDepth} levels exceeded."),
                Outcome.Invalid);
        }

        return null;
    }

    private async Task<Result<CreateCategoryResult>?> SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return null;
        }
        catch (ContentCoreConcurrencyException)
        {
            return Result<CreateCategoryResult>.Conflict(
                new Error(
                    "Category.ConcurrencyConflict",
                    "This record was modified by another user. Please refresh and try again."));
        }
    }
}
