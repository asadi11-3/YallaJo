using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Helpers;

public static class SecurityGuard
{
    public static string NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();

    public static Result<T> GuardNotFound<T>(T? entity, string code, string message)
        where T : class =>
        entity is null
            ? Result<T>.Failure(Error.NotFound(code, message), Outcome.NotFound)
            : Result<T>.Success(entity);

    public static Result<T>? GuardConflict<T>(bool condition, string entity, string message) =>
        condition ? Result<T>.Conflict(Error.Conflict(entity, message)) : null;
}
