using YallaJo.Web.Areas.Guide.Models.Applications;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

/// <summary>API client for the guide's tour-run applications (list own + apply to run a tour).</summary>
public sealed class ApplicationsApiClient
{
    private const string GuidesBase = "/api/v1/guides";
    private const string ToursBase = "/api/v1/tours";

    private readonly IApiClient _api;

    public ApplicationsApiClient(IApiClient api) => _api = api;

    /// <summary>GET /api/v1/guides/me/applications?page&amp;pageSize — the guide's own applications.</summary>
    public Task<ApiResult<GuideApplicationsResponse>> GetMyApplicationsAsync(
        int page,
        int pageSize,
        CancellationToken ct = default) =>
        _api.GetAsync<GuideApplicationsResponse>(
            $"{GuidesBase}/me/applications?page={page}&pageSize={pageSize}", ct);

    /// <summary>POST /api/v1/tours/{tourId}/applications — apply to run a tour.</summary>
    public Task<ApiResult> ApplyForTourAsync(
        Guid tourId,
        ApplyForTourRequest request,
        CancellationToken ct = default) =>
        _api.PostAsync($"{ToursBase}/{tourId}/applications", request, ct);
}
