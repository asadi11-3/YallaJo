using YallaJo.Web.Areas.Auth.Features.AcceptInvite.Requests;
using YallaJo.Web.Areas.Auth.Features.AcceptInvite.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.AcceptInvite;

public sealed class AcceptInviteFacade
{
    private readonly AcceptInviteApiClient _api;
    public AcceptInviteFacade(AcceptInviteApiClient api) => _api = api;

    public async Task<AcceptInviteResult> HandleAsync(AcceptInviteVm vm, CancellationToken ct = default)
    {
        var result = await _api.AcceptAsync(new AcceptInviteRequest
        {
            Email           = vm.Email.Trim(),
            Token           = vm.Token.Trim(),
            Password        = vm.Password,
            ConfirmPassword = vm.ConfirmPassword,
        }, ct);

        if (result.IsSuccess)
            return AcceptInviteResult.Ok();

        if (result.IsValidationError)
        {
            // Distinguish Invite.Expired returned as 400 (we can't introspect
            // error code reliably from ProblemDetails here, so treat any 400
            // whose error text mentions "expired" as the expired branch).
            if (result.Error is { } msg && msg.Contains("expired", StringComparison.OrdinalIgnoreCase))
                return AcceptInviteResult.Expired(msg);

            return result.ValidationErrors is not null
                ? AcceptInviteResult.Invalid(result.ValidationErrors)
                : AcceptInviteResult.Fail(result.Error ?? "Invalid invite.");
        }

        if (result.IsNotFound)
            return AcceptInviteResult.Expired(result.Error ?? "This invite is invalid or has expired.");

        if (result.IsConflict)
            return AcceptInviteResult.AlreadyCompleted(
                result.Error ?? "This invite has already been accepted. Please sign in instead.");

        if (result.IsTooManyRequests)
            return AcceptInviteResult.Fail(
                result.Error ?? "Too many invalid attempts. Please request a new invite.");

        return AcceptInviteResult.Fail(result.Error ?? "Could not accept invite.");
    }
}
