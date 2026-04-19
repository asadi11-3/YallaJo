namespace Security.Application.Queries.Dtos
{
    public sealed record UserDto(
        Guid Id,
        string Email,
        bool IsActive,
        IReadOnlyList<string> Roles,
        IReadOnlyList<UserClaimDto> Claims);

    public sealed record UserClaimDto(
        Guid Id,
        string ClaimType,
        string ClaimValue);
}
