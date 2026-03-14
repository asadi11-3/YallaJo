using ContentCore.Application.Commands.Category.DeactivateCategory;
using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Category.ReactivateCategory;

public sealed class ReactivateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<ReactivateCategoryCommand, ReactivateCategoryResult>
{
    public async Task<Result<ReactivateCategoryResult>> Handle(
        ReactivateCategoryCommand request,
        CancellationToken ct)
    {
        var category = await categoryRepository.GetByIdAsync(
            request.Id,
            ct,
            asNoTracking: false);

        if (category is null)
        {
            return Result<ReactivateCategoryResult>.NotFound(
                $"Category '{request.Id}' not found.");
        }

        // ملاحظة: إعادة التفعيل تعني IsActive = true
        category.Activate();

        await unitOfWork.SaveChangesAsync(ct);

        return Result<ReactivateCategoryResult>.Success(
            new ReactivateCategoryResult(category.Id, category.IsActive));
    }
}