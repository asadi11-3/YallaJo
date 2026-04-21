using Microsoft.AspNetCore.Authorization;

namespace YallaJo.SharedKernel.Presentation.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
