using System.Globalization;
using YallaJo.Web.Areas.Admin.Models.NotificationTemplates;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class NotificationTemplatesApiClient
{
    private const string Base = "/api/v1/admin/notification-templates";

    private readonly IApiClient _api;

    public NotificationTemplatesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<IReadOnlyList<NotificationTemplateResponse>>> GetAllAsync(CancellationToken ct)
        => _api.GetAsync<IReadOnlyList<NotificationTemplateResponse>>(Base, ct);

    public Task<ApiResult> CreateAsync(CreateTemplateRequest req, CancellationToken ct)
        => _api.PostAsync(
            Base,
            new
            {
                type = req.Type,
                channel = req.Channel,
                languageCode = req.LanguageCode,
                title = req.Title,
                body = req.Body,
                htmlBody = req.HtmlBody,
            },
            ct);

    public Task<ApiResult> UpdateAsync(Guid id, UpdateTemplateRequest req, CancellationToken ct)
        => _api.PutAsync(
            $"{Base}/{id.ToString("D", CultureInfo.InvariantCulture)}",
            new
            {
                title = req.Title,
                body = req.Body,
                htmlBody = req.HtmlBody,
                rowVersion = req.RowVersion,
            },
            ct);

    public Task<ApiResult> DeleteAsync(Guid id, string? rowVersion, CancellationToken ct)
        => _api.DeleteAsync(
            $"{Base}/{id.ToString("D", CultureInfo.InvariantCulture)}",
            new { rowVersion },
            ct);
}
