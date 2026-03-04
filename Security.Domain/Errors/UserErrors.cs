// Security.Domain/Errors/UserErrors.cs
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Domain.Errors;

public static class UserErrors
{
    public static readonly Error NotFound = new("NotFound.User", "The specified user was not found.");
}
