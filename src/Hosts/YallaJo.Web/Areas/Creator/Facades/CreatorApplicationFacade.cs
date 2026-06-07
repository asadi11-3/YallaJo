using YallaJo.Web.Features.Blogs.ApiClients;
using YallaJo.Web.Areas.Creator.ApiClients;
using YallaJo.Web.Areas.Creator.Models.Application;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Creator.Facades;

public sealed class CreatorApplicationFacade
{
    private readonly CreatorApiClient _creator;
    private readonly BlogsApiClient _blogs;

    public CreatorApplicationFacade(CreatorApiClient creator, BlogsApiClient blogs)
    {
        _creator = creator;
        _blogs = blogs;
    }

    /// <summary>Builds the application page VM: current application (if any) + niche options.</summary>
    public async Task<ApiResult<CreatorApplicationFormVm>> GetApplicationFormAsync(CancellationToken ct = default)
    {
        var applicationTask = _creator.GetMyApplicationAsync(ct);
        var nichesTask = _blogs.ListCreatorNichesAsync(ct);

        await Task.WhenAll(applicationTask, nichesTask).ConfigureAwait(false);

        var applicationResult = await applicationTask.ConfigureAwait(false);
        var nichesResult = await nichesTask.ConfigureAwait(false);

        if (applicationResult.RequireSignOut || nichesResult.RequireSignOut)
            return ApiResult<CreatorApplicationFormVm>.ForceSignOut();

        if (applicationResult.IsForbidden)
            return ApiResult<CreatorApplicationFormVm>.Fail(
                403, "You don't have permission to view creator applications.");

        if (!TryReadMine(applicationResult, out var application))
            return ApiResult<CreatorApplicationFormVm>.Fail(
                applicationResult.StatusCode,
                applicationResult.Error ?? "Could not load your creator application.");

        var nicheOptions = CreatorApplicationMapper.ToNicheOptions(
            nichesResult is { IsSuccess: true, Data: { } n } ? n : null);

        var vm = CreatorApplicationMapper.ToFormVm(application, nicheOptions);
        return ApiResult<CreatorApplicationFormVm>.Ok(vm);
    }

    /// <summary>Re-populates niche options on a redisplay (e.g. after a validation failure).</summary>
    public async Task PopulateNicheOptionsAsync(CreatorApplicationFormVm form, CancellationToken ct = default)
    {
        var nichesResult = await _blogs.ListCreatorNichesAsync(ct).ConfigureAwait(false);
        form.NicheOptions = CreatorApplicationMapper.ToNicheOptions(
            nichesResult is { IsSuccess: true, Data: { } n } ? n : null);
    }

    /// <summary>POST create (Draft). Returns the new application id on success.</summary>
    public async Task<ApiResult<Guid>> CreateAsync(CreatorApplicationFormVm form, CancellationToken ct = default)
    {
        var body = CreatorApplicationMapper.ToCreateBody(form);
        var result = await _creator.CreateApplicationAsync(body, ct).ConfigureAwait(false);

        if (result.RequireSignOut) return ApiResult<Guid>.ForceSignOut();
        if (result is { IsSuccess: true, Data: { } data })
            return ApiResult<Guid>.Ok(data.ApplicationId, result.StatusCode);
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult<Guid>.ValidationFail(result.StatusCode, result.ValidationErrors);
        return ApiResult<Guid>.Fail(result.StatusCode, result.Error ?? FriendlyError(result.StatusCode, "create"));
    }

    /// <summary>PUT update an existing Draft/MoreInfoNeeded application.</summary>
    public async Task<ApiResult> UpdateAsync(Guid applicationId, CreatorApplicationFormVm form, CancellationToken ct = default)
    {
        var body = CreatorApplicationMapper.ToUpdateBody(form);
        return Normalize(await _creator.UpdateApplicationAsync(applicationId, body, ct).ConfigureAwait(false), "update");
    }

    /// <summary>POST submit a Draft for review.</summary>
    public async Task<ApiResult> SubmitAsync(Guid applicationId, CancellationToken ct = default)
        => Normalize(await _creator.SubmitApplicationAsync(applicationId, ct).ConfigureAwait(false), "submit");

    /// <summary>POST redeem an invitation token.</summary>
    public async Task<ApiResult> RedeemAsync(string token, CancellationToken ct = default)
        => Normalize(
            await _creator.RedeemInvitationAsync(new RedeemInvitationRequestBody(token.Trim()), ct).ConfigureAwait(false),
            "redeem");

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static ApiResult Normalize(ApiResult result, string verb)
    {
        if (result.RequireSignOut) return ApiResult.ForceSignOut();
        if (result.IsSuccess) return ApiResult.Ok(result.StatusCode);
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.ValidationFail(result.StatusCode, result.ValidationErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? FriendlyError(result.StatusCode, verb));
    }

    private static string FriendlyError(int statusCode, string verb) => statusCode switch
    {
        403 => "You don't have permission to perform this action.",
        404 => "We couldn't find that creator application or invitation.",
        409 => "You already have an active creator application.",
        422 => "We couldn't process that request. Please review the details and try again.",
        _   => $"Could not {verb} your creator application. Please try again.",
    };

    /// <summary>
    /// Treats a 200-with-empty-body (the Web ApiClient's signal for an absent "/mine"
    /// resource) and an explicit 404 as a valid "no application" reading.
    /// </summary>
    private static bool TryReadMine<T>(ApiResult<T> result, out T? value) where T : class
    {
        if (result is { IsSuccess: true, Data: { } data })
        {
            value = data;
            return true;
        }

        if (result.StatusCode == 200 || result.IsNotFound)
        {
            value = null;
            return true;
        }

        value = null;
        return false;
    }
}
