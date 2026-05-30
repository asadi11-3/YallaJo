// Security.Domain/Errors/RoleErrors.cs
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Domain.Errors;

public static class RoleErrors
{
    public static readonly Error NotFound       = new("NotFound.Role",          "The specified role was not found.");
    public static readonly Error AlreadyExists  = new("Role.Conflict",          "A role with that name already exists.");
    public static readonly Error Protected       = new("Role.Protected",         "This role is protected and cannot be modified or deleted.");
    public static readonly Error OwnerOnly       = new("Role.OwnerOnly",         "Only the Owner can assign or remove this role.");
    public static readonly Error OwnerSingleton  = new("Role.OwnerSingleton",   "Only one user may hold the Owner role at a time.");
    public static readonly Error AlreadyAssigned = new("Role.AlreadyAssigned",  "The user already has this role assigned.");
    public static readonly Error NotAssigned     = new("Role.NotAssigned",      "The user does not have this role assigned.");
    public static readonly Error Inactive        = new("Role.Inactive",         "The specified role is not active.");
}
