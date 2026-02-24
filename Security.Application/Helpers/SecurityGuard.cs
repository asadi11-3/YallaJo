using Security.Application.Interfaces;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Helpers;

/// <summary>
/// Application-layer guard and utility helpers for the Security module.
///
/// Rules:
///   ✔ May reference Security.Application interfaces (IJwtTokenService, IPasswordHasher, ClaimEntry).
///   ✔ May reference SharedKernel domain types (Result&lt;T&gt;, Error, Outcome).
///   ✗ Must NOT reference EF Core, ASP.NET Identity, JWT libraries, or any Infrastructure type.
/// </summary>
public static class SecurityGuard
{
    /// <summary>
    /// Returns the canonical form of an email address: trimmed whitespace + lowercase.
    /// Throws nothing — returns empty string for null/whitespace input.
    /// </summary>
    public static string NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();

    /// <summary>
    /// Returns <see cref="Result{T}.Failure"/> (NotFound) when <paramref name="entity"/> is null;
    /// otherwise wraps the value in a success result.
    /// </summary>
    /// <example>
    /// <code>
    /// var user = await repo.GetByEmailWithDetailsAsync(email, ct);
    /// return SecurityGuard.GuardNotFound(user, "User.NotFound", "No account with that email.");
    /// </code>
    /// </example>
    public static Result<T> GuardNotFound<T>(T? entity, string code, string message)
        where T : class =>
        entity is null
            ? Result<T>.Failure(Error.NotFound(code, message), Outcome.NotFound)
            : Result<T>.Success(entity);

    /// <summary>
    /// Returns a Conflict failure result when <paramref name="condition"/> is true; otherwise null.
    ///
    /// Pattern (early-return on conflict):
    /// <code>
    /// if (SecurityGuard.GuardConflict&lt;RegisterResult&gt;(emailExists, "User.Email", "Email taken") is { } conflict)
    ///     return conflict;
    /// </code>
    /// </summary>
    public static Result<T>? GuardConflict<T>(bool condition, string entity, string message) =>
        condition ? Result<T>.Conflict(Error.Conflict(entity, message)) : null;

    /// <summary>
    /// Deduplicates JWT claim entries by (Type, Value) pair, preserving first-occurrence order.
    /// </summary>
    public static IReadOnlyList<ClaimEntry> DeduplicateClaims(IEnumerable<ClaimEntry> claims) =>
        claims.DistinctBy(c => (c.Type, c.Value)).ToList().AsReadOnly();
}

/// <summary>
/// Pure-C# collection extensions. No external dependencies.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Converts any enumerable to an <see cref="IReadOnlyList{T}"/> without double-enumerating
    /// sequences that are already materialized.
    /// </summary>
    public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source) =>
        source is IReadOnlyList<T> already ? already : source.ToList().AsReadOnly();
}
