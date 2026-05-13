using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Domain.Errors;

public static class ProfileErrors
{
    public static readonly Error NotFound =
        new("NotFound.Profile", "Profile not found.");
}
