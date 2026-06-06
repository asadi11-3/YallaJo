using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Public.Models.Translations;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

public sealed class TranslationsApiClient(IApiClient api)
{
    private const string Base = "/api/v1/content-core/translations";
    private const string ApprovedStatus = "HumanReviewed";

    public Task<ApiResult<TranslationsResponse>> GetAsync(
        string entityType,
        Guid entityId,
        string languageCode,
        CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(
            $"{Base}/{Uri.EscapeDataString(entityType)}/{entityId}",
            new Dictionary<string, string?>
            {
                ["languageCode"] = languageCode,
                ["status"] = ApprovedStatus,
            });

        return api.GetAsync<TranslationsResponse>(url, ct);
    }
}
