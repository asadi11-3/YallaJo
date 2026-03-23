using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Category.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<CreateCategoryCommand, CreateCategoryResult>
{
    public async Task<Result<CreateCategoryResult>> Handle(
        CreateCategoryCommand request,
        CancellationToken ct)
    {
        try
        {
            // Auto-generate slug from name when not provided
            var slug = request.Slug ?? GenerateSlug(request.Name);

            // Validate parent + enforce max 3-level depth
            if (request.ParentCategoryId.HasValue)
            {
                var parent = await categoryRepository.GetByIdAsync(request.ParentCategoryId.Value, ct);
                if (parent is null)
                    return Result<CreateCategoryResult>.Failure(
                        new Error("Category.NotFound",
                            $"Parent category '{request.ParentCategoryId}' was not found."),
                        Outcome.NotFound);

                if (parent.ParentCategoryId.HasValue)
                {
                    var grandparent = await categoryRepository.GetByIdAsync(parent.ParentCategoryId.Value, ct);
                    if (grandparent?.ParentCategoryId.HasValue == true)
                        return Result<CreateCategoryResult>.Failure(
                            new Error("Category.MaxDepthExceeded",
                                "Cannot create category: maximum depth of 3 levels exceeded."),
                            Outcome.Invalid);
                }
            }

            var category = Domain.Entities.Category.Create(
                request.Name, slug, request.SourceLanguageCode,
                request.ParentCategoryId, request.SortOrder);

            await categoryRepository.AddAsync(category, ct);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<CreateCategoryResult>.Conflict(
                    new Error("Category.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync("categories", ct);

            return Result<CreateCategoryResult>.Created(
                new CreateCategoryResult(category.Id, category.Name, category.Slug));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<CreateCategoryResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private static string GenerateSlug(string name) =>
        System.Text.RegularExpressions.Regex
            .Replace(name.Trim().ToLowerInvariant().Replace(' ', '-'), @"[^a-z0-9\-]", string.Empty)
            .Trim('-');
}
