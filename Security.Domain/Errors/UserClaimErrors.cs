using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Domain.Errors;

public static class UserClaimErrors
{
    public static readonly Error NotFound = new(
        "NotFound.UserClaim",
        "The specified claim was not found on this user.");

    public static readonly Error Duplicate = new(
        "UserClaim.Duplicate",
        "This claim already exists on the user.");
}
