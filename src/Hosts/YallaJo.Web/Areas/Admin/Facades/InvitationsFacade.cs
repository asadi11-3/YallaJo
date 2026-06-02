using YallaJo.Web.Areas.Admin.Models.Invitations;
using YallaJo.Web.Areas.Admin.Models.Invitations;
using YallaJo.Web.Areas.Admin.Models.Invitations;
using YallaJo.Web.Infrastructure.Api.Contracts;

using YallaJo.Web.Areas.Admin.ApiClients;
namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class InvitationsFacade
{
    private readonly InvitationsApiClient _api;
    public InvitationsFacade(InvitationsApiClient api) => _api = api;

    public async Task<ApiResult<IReadOnlyList<InvitableRoleOptionVm>>> GetInvitableRolesAsync(CancellationToken ct = default)
    {
        var result = await _api.GetInvitableRolesAsync(ct);

        if (result.IsSuccess && result.Data is not null)
        {
            var options = (IReadOnlyList<InvitableRoleOptionVm>)result.Data
                .Select(r => new InvitableRoleOptionVm
                {
                    RoleId = r.RoleId,
                    Name = r.Name,
                    Description = r.Description,
                    IsPrivileged = r.IsPrivileged,
                })
                .ToList();

            return ApiResult<IReadOnlyList<InvitableRoleOptionVm>>.CreateSuccess(options, result.StatusCode);
        }

        if (result.IsUnauthorized)
            return ApiResult<IReadOnlyList<InvitableRoleOptionVm>>.ForceSignOut();

        return ApiResult<IReadOnlyList<InvitableRoleOptionVm>>.CreateFailure(
            result.StatusCode,
            result.Error ?? "Could not load invitable roles.");
    }

    public async Task<ApiResult<InviteUserResponse>> InviteAsync(InviteUserVm vm, CancellationToken ct = default)
    {
        var request = new InviteUserRequest(
            Email:       vm.Email.Trim(),
            FirstName:   vm.FirstName.Trim(),
            LastName:    vm.LastName.Trim(),
            DisplayName: string.IsNullOrWhiteSpace(vm.DisplayName) ? null : vm.DisplayName.Trim(),
            AvatarUrl:   string.IsNullOrWhiteSpace(vm.AvatarUrl) ? null : vm.AvatarUrl.Trim(),
            RoleIds:     vm.SelectedRoleIds.Distinct().ToList());

        var result = await _api.InviteAsync(request, ct);

        if (result.IsSuccess && result.Data is not null)
            return ApiResult<InviteUserResponse>.CreateSuccess(result.Data, result.StatusCode);

        if (result.IsUnauthorized)    return ApiResult<InviteUserResponse>.ForceSignOut();
        if (result.IsConflict)        return ApiResult<InviteUserResponse>.CreateFailure(409, "An account with this email already exists.");
        if (result.IsValidationError) return ApiResult<InviteUserResponse>.CreateValidationFailure(result.StatusCode, result.ValidationErrors!);
        return ApiResult<InviteUserResponse>.CreateFailure(result.StatusCode, result.Error ?? "Could not send invite.");
    }

    public async Task<ApiResult> ResendAsync(ResendInviteVm vm, CancellationToken ct = default)
    {
        var result = await _api.ResendAsync(new ResendInviteRequest(vm.Email.Trim()), ct);

        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsConflict) return ApiResult.Fail("This account has already completed onboarding.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? "Could not resend invite.");
    }
}
