namespace ContentCore.Application.Queries.Specialization.ListSpecializations;

public sealed record SpecializationDto(
    Guid Id,
    string Name,
    string? Description,
    string? Icon,
    bool IsActive);
