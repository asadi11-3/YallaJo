using YallaJo.Web.Areas.Content.ApiClients;
using YallaJo.Web.Areas.Content.Models.Blogs;
using YallaJo.Web.Areas.Content.Models.CreatorApplication;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Content.Facades;

public sealed class CreatorApplicationFacade
{
    private readonly CreatorApplicationApiClient _api;

    public CreatorApplicationFacade(CreatorApplicationApiClient api) => _api = api;

    public async Task<ApiResult<CreatorApplyVm>> GetApplyAsync(
        CreatorApplicationFormVm? form = null,
        CancellationToken ct = default)
    {
        var niches = await LoadNichesAsync(ct);
        if (niches.RequireSignOut) return ApiResult<CreatorApplyVm>.ForceSignOut();

        form ??= new CreatorApplicationFormVm();
        var vm = new CreatorApplyVm
        {
            Form = form,
            Niches = CreatorApplicationMapper.ToNicheChoices(niches.Data ?? [], form.NicheIds),
        };
        return ApiResult<CreatorApplyVm>.Ok(vm);
    }

    /// <summary>Creates the application then immediately submits it for review.</summary>
    public async Task<ApiResult> ApplyAsync(CreatorApplicationFormVm form, CancellationToken ct = default)
    {
        var create = await _api.CreateAsync(CreatorApplicationMapper.ToCreateRequest(form), ct);
        if (!create.IsSuccess) return Normalize(create, "Could not create your creator application.");

        var id = create.Data!.Id;
        var submit = await _api.SubmitAsync(id, ct);
        return Normalize(submit, "Your application was saved but could not be submitted for review.");
    }

    public async Task<ApiResult<CreatorApplicationStatusVm>> GetApplicationAsync(CancellationToken ct = default)
    {
        var result = await _api.GetMineAsync(ct);

        if (result.IsSuccess && result.Data is not null)
        {
            var niches = await LoadNichesAsync(ct);
            if (niches.RequireSignOut) return ApiResult<CreatorApplicationStatusVm>.ForceSignOut();

            var vm = CreatorApplicationMapper.ToStatusVm(result.Data, niches.Data ?? []);
            return ApiResult<CreatorApplicationStatusVm>.Ok(vm);
        }

        if (result.IsUnauthorized) return ApiResult<CreatorApplicationStatusVm>.ForceSignOut();

        // No application yet: surface an empty status so the page can offer the apply call to action.
        if (result.IsNotFound)
        {
            return ApiResult<CreatorApplicationStatusVm>.Ok(new CreatorApplicationStatusVm { HasApplication = false });
        }

        return ApiResult<CreatorApplicationStatusVm>.Fail(result.StatusCode, result.Error);
    }

    public Task<ApiResult> UpdateApplicationAsync(CreatorApplicationFormVm form, CancellationToken ct = default)
    {
        var request = CreatorApplicationMapper.ToUpdateRequest(form);
        return RunAsync(() => _api.UpdateAsync(form.Id, request, ct), "Could not update your creator application.");
    }

    public Task<ApiResult> SubmitApplicationAsync(Guid applicationId, CancellationToken ct = default)
        => RunAsync(() => _api.SubmitAsync(applicationId, ct), "Could not submit your application for review.");

    public Task<ApiResult> RedeemAsync(RedeemCreatorInvitationVm vm, CancellationToken ct = default)
    {
        var request = new RedeemCreatorInvitationApiRequest(vm.Token.Trim());
        return RunAsync(() => _api.RedeemInvitationAsync(request, ct), "Could not redeem that invitation token.");
    }

    private async Task<ApiResult<List<CreatorNicheResponse>>> LoadNichesAsync(CancellationToken ct)
    {
        var result = await _api.GetNichesAsync(ct);
        if (result.IsSuccess) return ApiResult<List<CreatorNicheResponse>>.Ok(result.Data ?? []);
        if (result.IsUnauthorized) return ApiResult<List<CreatorNicheResponse>>.ForceSignOut();
        // Niches are non-fatal for rendering; fall back to an empty list.
        return ApiResult<List<CreatorNicheResponse>>.Ok([]);
    }

    private static async Task<ApiResult> RunAsync(Func<Task<ApiResult>> call, string fallback)
    {
        var result = await call();
        return Normalize(result, fallback);
    }

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static ApiResult Normalize<T>(ApiResult<T> result, string fallback)
    {
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
