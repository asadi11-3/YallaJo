using YallaJo.Web.Areas.Accounts.Models.UpdatePhone;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

public sealed class UpdatePhoneApiClient
{
    private readonly IApiClient _api;
    public UpdatePhoneApiClient(IApiClient api) => _api = api;

    public Task<ApiResult> UpdatePrimaryPhoneAsync(
        UpdatePrimaryPhoneRequest request, CancellationToken ct = default)
        => _api.PutAsync("/api/v1/security/account/phone", request, ct);
}
