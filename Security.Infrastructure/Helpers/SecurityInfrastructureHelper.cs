using Microsoft.AspNetCore.Identity;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Infrastructure.Helpers;

/// <summary>
/// Infrastructure-layer helpers that may reference ASP.NET Identity types.
///
/// Layer contract:
///   ✔ May reference Microsoft.AspNetCore.Identity (IdentityResult, IdentityError, …).
///   ✗ Must NOT be referenced from Security.Application or Security.Domain.
///   ✗ Must NOT be public — internal keeps it inside Security.Infrastructure.
/// </summary>
internal static class SecurityInfrastructureHelper
{
    /// <summary>
    /// Maps ASP.NET Identity errors from an <see cref="IdentityResult"/> to a
    /// domain <see cref="Error"/> list. Returns an empty list when the operation succeeded.
    ///
    /// Usage (after a UserManager / RoleManager call):
    /// <code>
    /// var result = await _userManager.CreateAsync(user, password);
    /// if (!result.Succeeded)
    ///     return Result&lt;Guid&gt;.Invalid(SecurityInfrastructureHelper.MapIdentityErrors(result).ToArray());
    /// </code>
    /// </summary>
    public static IReadOnlyList<Error> MapIdentityErrors(IdentityResult result) =>
        result.Succeeded
            ? []
            : result.Errors
                .Select(e => Error.Failure(e.Code, e.Description))
                .ToList()
                .AsReadOnly();
}
