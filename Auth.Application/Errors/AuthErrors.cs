using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Errors;

public static class AuthErrors
{
    public static readonly Error AdminUnauthenticated = new(
        "Auth.Unauthenticated",
        "Admin actor is not authenticated.");

    public static readonly Error UserNotFound = new(
        "NotFound.User",
        "No account found with this email.");
}
