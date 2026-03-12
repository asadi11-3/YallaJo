using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Category.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<UpdateCategoryCommand, UpdateCategoryResult>
{
    public async Task<Result<UpdateCategoryResult>> Handle(
        UpdateCategoryCommand request,
        CancellationToken ct)
    {
        // 1. Load entity (tracked for ChangeTracker)
        var category = await categoryRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);

        if (category is null)
            return Result<UpdateCategoryResult>.NotFound(
                $"Category '{request.Id}' not found.");

        // 2. Update translatable content (raises CategoryUpdatedDomainEvent)
        category.Update(request.Name, request.Slug, request.SourceLanguageCode);

        
        
        // 3. Update optional properties
        
       /* if (request.Icon is not null)
            category.SetIcon(request.Icon);

        if (request.SortOrder.HasValue)
            category.SetSortOrder(request.SortOrder.Value);
        */

        
        // Update parent category if changed
        if (request.ParentCategoryId.HasValue &&
            request.ParentCategoryId != category.ParentCategoryId)
        {
            category.ChangeParent(request.ParentCategoryId);
        }

        // 3. Update optional properties
        if (request.Icon is not null)
            category.SetIcon(request.Icon);

        if (request.SortOrder.HasValue)
            category.SetSortOrder(request.SortOrder.Value);


        // 4. Persist (UoW dispatches domain events during SaveChanges)

        /* await unitOfWork.SaveChangesAsync(ct);

       return Result<UpdateCategoryResult>.Success(
           new UpdateCategoryResult(category.Id, category.Name, category.Slug));
       */


        // Update translations if provided
        if (request.Translations is not null)
        {
            foreach (var translation in request.Translations)
            {
                try
                {
                    category.UpdateTranslation(
                        translation.LanguageId,
                        translation.Name,
                        translation.Slug);
                }
                catch
                {
                    category.AddTranslation(
                        translation.LanguageId,
                        translation.Name,
                        translation.Slug);
                }
            }
        }

        // 4. Persist (UoW dispatches domain events during SaveChanges)
        await unitOfWork.SaveChangesAsync(ct);

        return Result<UpdateCategoryResult>.Success(
            new UpdateCategoryResult(category.Id, category.Name, category.Slug));
    }
}