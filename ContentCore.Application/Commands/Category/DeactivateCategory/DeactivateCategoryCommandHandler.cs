using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Category.DeactivateCategory;

public sealed class DeactivateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<DeactivateCategoryCommand, DeactivateCategoryResult>
{
    public async Task<Result<DeactivateCategoryResult>> Handle(
        DeactivateCategoryCommand request,
        CancellationToken ct)
    {
        // ملاحظة: نجيب الكاتيجوري tracked حتى نقدر نعدّل عليه
        var category = await categoryRepository.GetByIdAsync(
            request.Id,
            ct,
            asNoTracking: false);

        if (category is null)
        {
            return Result<DeactivateCategoryResult>.NotFound(
                $"Category '{request.Id}' not found.");
        }

        // ملاحظة: هون التعطيل منطقيًا يعني IsActive = false
        category.Deactivate();

        await unitOfWork.SaveChangesAsync(ct);

        return Result<DeactivateCategoryResult>.Success(
            new DeactivateCategoryResult(category.Id, category.IsActive));
    }
}