using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Domain.Errors;

public static class RoleClaimErrors
{
    public static readonly Error NotFound = new(
        "NotFound.RoleClaim",
        "The specified claim was not found on this role.");

    public static readonly Error Duplicate = new(
        "RoleClaim.Duplicate",
        "This claim already exists on the role.");
}
