namespace Security.Application.Queries.Dtos;

public sealed record RoleDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    IReadOnlyList<RoleClaimDto> Claims);


