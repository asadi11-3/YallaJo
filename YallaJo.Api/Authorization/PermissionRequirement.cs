// YallaJo.Api/Authorization/PermissionRequirement.cs
using Microsoft.AspNetCore.Authorization;

namespace YallaJo.Api.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
