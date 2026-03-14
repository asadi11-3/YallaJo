using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Specialization.CreateSpecialization;

public sealed class CreateSpecializationCommandHandler(
    ISpecializationRepository specializationRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<CreateSpecializationCommand, CreateSpecializationResult>
{
    public async Task<Result<CreateSpecializationResult>> Handle(
        CreateSpecializationCommand request,
        CancellationToken ct)
    {
        var specialization = Domain.Entities.Specialization.Create(
            request.Name,
            request.Description,
            request.Icon);

        await specializationRepository.AddAsync(specialization, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<CreateSpecializationResult>.Created(
            new CreateSpecializationResult(specialization.Id, specialization.Name));
    }
}
