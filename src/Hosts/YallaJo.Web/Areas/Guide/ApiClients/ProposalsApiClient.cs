using YallaJo.Web.Areas.Guide.Models.Proposals;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

/// <summary>
/// Talks to the ContentTours tour-proposal endpoints under
/// <c>/api/v1/tours/proposals</c>.
/// </summary>
public sealed class ProposalsApiClient
{
    private const string ProposalsBase = "/api/v1/tours/proposals";

    private readonly IApiClient _api;

    public ProposalsApiClient(IApiClient api) => _api = api;

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
