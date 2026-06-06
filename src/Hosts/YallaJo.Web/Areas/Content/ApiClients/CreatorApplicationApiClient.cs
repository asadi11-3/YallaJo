using YallaJo.Web.Areas.Content.Models.Blogs;
using YallaJo.Web.Areas.Content.Models.CreatorApplication;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Content.ApiClients;

public sealed class CreatorApplicationApiClient
{
    private const string Base = "/api/v1/blogs/creators";

    private readonly IApiClient _api;

    public CreatorApplicationApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<CreatorNicheResponse>>> GetNichesAsync(CancellationToken ct = default)
        => _api.GetAsync<List<CreatorNicheResponse>>($"{Base}/niches", ct);

    public Task<ApiResult<CreatorApplicationResponse>> GetMineAsync(CancellationToken ct = default)
        => _api.GetAsync<CreatorApplicationResponse>($"{Base}/applications/mine", ct);

    public Task<ApiResult<CreatorApplicationResponse>> CreateAsync(
        CreateCreatorApplicationApiRequest request,
        CancellationToken ct = default)
        => _api.PostAsync<CreatorApplicationResponse>($"{Base}/applications", request, ct);

    public Task<ApiResult> UpdateAsync(
        Guid applicationId,
        UpdateCreatorApplicationApiRequest request,
        CancellationToken ct = default)
        => _api.PutAsync($"{Base}/applications/{applicationId:D}", request, ct);

    public Task<ApiResult> SubmitAsync(Guid applicationId, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/applications/{applicationId:D}/submit", null, ct);

    public Task<ApiResult> RedeemInvitationAsync(
        RedeemCreatorInvitationApiRequest request,
        CancellationToken ct = default)
        => _api.PostAsync($"{Base}/invitations/redeem", request, ct);
}
