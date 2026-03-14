using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Specialization.UpdateSpecialization;

public sealed class UpdateSpecializationCommandHandler(
    ISpecializationRepository specializationRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<UpdateSpecializationCommand, UpdateSpecializationResult>
{
    public async Task<Result<UpdateSpecializationResult>> Handle(
        UpdateSpecializationCommand request,
        CancellationToken ct)
    {
        var specialization = await specializationRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);

        if (specialization is null)
            return Result<UpdateSpecializationResult>.NotFound($"Specialization '{request.Id}' not found.");

        specialization.Update(request.Name, request.Description, request.Icon);

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                specialization.Activate();
            else
                specialization.Deactivate();
        }

        await unitOfWork.SaveChangesAsync(ct);

        return Result<UpdateSpecializationResult>.Success(
            new UpdateSpecializationResult(specialization.Id, specialization.Name));
    }
}
