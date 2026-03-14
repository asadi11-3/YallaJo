using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Category.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<DeleteCategoryCommand>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await categoryRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
        if (category is null)
            return Result.NotFound($"Category '{request.Id}' not found.");

        // Soft delete (Category is AuditableEntity)
        category.SoftDelete();
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
