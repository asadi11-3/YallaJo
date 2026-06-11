using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Guide.Models.Proposals;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

/// <summary>
/// Talks to the ContentTours tour-proposal endpoints under
/// <c>/api/v1/tours/proposals</c>, plus the places lookup feeding the
/// F10 place picker on the create form.
/// </summary>
public sealed class ProposalsApiClient
{
    private const string ProposalsBase = "/api/v1/tours/proposals";
    private const string PlacesBase = "/api/v1/places";
    private const int LookupPageSize = 20; // API clamps the lookup to max 20

    private readonly IApiClient _api;

    public ProposalsApiClient(IApiClient api) => _api = api;

    /// <summary>
    /// GET /api/v1/places/lookup?term=&amp;pageSize=20 — lightweight typeahead lookup.
    /// A blank term returns the first N places alphabetically (used for the no-JS prefill, PE2).
    /// </summary>
    public Task<ApiResult<List<PlaceLookupResponse>>> LookupPlacesAsync(string? term, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["pageSize"] = LookupPageSize.ToString(),
        };
        if (!string.IsNullOrWhiteSpace(term)) query["term"] = term;

        var url = QueryHelpers.AddQueryString($"{PlacesBase}/lookup", query);
        return _api.GetAsync<List<PlaceLookupResponse>>(url, ct);
    }

    /// <summary>
    /// GET /api/v1/tours/proposals — list the current guide's proposals.
    /// </summary>
    public Task<ApiResult<List<TourProposalResponse>>> GetMyProposalsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<TourProposalResponse>>(ProposalsBase, ct);

    /// <summary>
    /// POST /api/v1/tours/proposals — create a proposal draft. Returns the new id.
    /// </summary>
    public Task<ApiResult<Guid>> CreateAsync(CreateTourProposalRequest request, CancellationToken ct = default)
        => _api.PostAsync<Guid>(ProposalsBase, request, ct);

    /// <summary>
    /// POST /api/v1/tours/proposals/{id}/submit — submit a draft for admin review.
    /// </summary>
    public Task<ApiResult> SubmitAsync(Guid id, CancellationToken ct = default)
        => _api.PostAsync($"{ProposalsBase}/{id}/submit", null, ct);
}
