using Microsoft.AspNetCore.Identity;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Infrastructure.Services;

internal static class SecurityInfrastructureHelper
{
    public static IReadOnlyList<Error> MapIdentityErrors(IdentityResult result) =>
        result.Succeeded
            ? []
            : result.Errors
                .Select(e => Error.Failure(e.Code, e.Description))
                .ToList()
                .AsReadOnly();
}
