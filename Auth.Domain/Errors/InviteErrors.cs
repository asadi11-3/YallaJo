using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Domain.Errors;

public static class InviteErrors
{
    public static readonly Error NotFound = new(
        "NotFound.Invite",
        "This invite is invalid or has expired.");
}
