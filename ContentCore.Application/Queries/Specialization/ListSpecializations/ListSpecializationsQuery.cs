using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Specialization.ListSpecializations;

public sealed record SpecializationDto(
    Guid Id,
    string Name,
    string? Description,
    string? Icon,
    bool IsActive);

public sealed record ListSpecializationsQuery(bool ActiveOnly = false) : IQuery<IReadOnlyList<SpecializationDto>>;
