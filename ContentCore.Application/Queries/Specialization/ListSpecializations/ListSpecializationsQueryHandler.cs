using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Queries.Specialization.ListSpecializations;

public sealed class ListSpecializationsQueryHandler(ISpecializationRepository specializationRepository)
    : IQueryHandler<ListSpecializationsQuery, IReadOnlyList<SpecializationDto>>
{
    public async Task<Result<IReadOnlyList<SpecializationDto>>> Handle(
        ListSpecializationsQuery request,
        CancellationToken ct)
    {
        var specializations = await specializationRepository.GetAllAsync(
            filter: request.ActiveOnly ? s => s.IsActive : null,
            orderBy: q => q.OrderBy(s => s.Name),
            ct: ct);

        var dtos = specializations
            .Select(s => new SpecializationDto(
                s.Id,
                s.Name,
                s.Description,
                s.Icon,
                s.IsActive))
            .ToList() as IReadOnlyList<SpecializationDto>;

        return Result<IReadOnlyList<SpecializationDto>>.Success(dtos);
    }
}
