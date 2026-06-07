using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Provider.Models.TourAvailability;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

/// <summary>
/// Typed access to the Booking availability-slot API for provider slot management.
/// All endpoint URLs live here; calls go through <see cref="IApiClient"/>.
/// </summary>
public sealed class ProviderTourAvailabilityApiClient
{
    private const string Base = "/api/v1/booking/availability";

    private readonly IApiClient _api;

    public ProviderTourAvailabilityApiClient(IApiClient api) => _api = api;

    // GET /api/v1/booking/availability/{tourId}/manage — owner-scoped list incl. inactive + RowVersion
    public Task<ApiResult<List<ManageAvailabilitySlotResponse>>> GetManageListAsync(
        Guid tourId, CancellationToken ct = default)
        => _api.GetAsync<List<ManageAvailabilitySlotResponse>>($"{Base}/{tourId}/manage", ct);

    // POST /api/v1/booking/availability/slots
    public Task<ApiResult<CreateAvailabilitySlotResponse>> CreateAsync(
        CreateAvailabilitySlotApiRequest request, CancellationToken ct = default)
        => _api.PostAsync<CreateAvailabilitySlotResponse>($"{Base}/slots", request, ct);

    // POST /api/v1/booking/availability/slots/bulk — recurring slot generation
    public Task<ApiResult> CreateBulkAsync(
        CreateBulkAvailabilitySlotsApiRequest request, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/slots/bulk", request, ct);

    // PUT /api/v1/booking/availability/slots/{slotId}
    public Task<ApiResult> UpdateAsync(
        Guid slotId, UpdateAvailabilitySlotApiRequest request, CancellationToken ct = default)
        => _api.PutAsync($"{Base}/slots/{slotId}", request, ct);

    // DELETE /api/v1/booking/availability/slots/{slotId}?rowVersion={rowVersion}
    public Task<ApiResult> DeleteAsync(Guid slotId, string rowVersion, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(
            $"{Base}/slots/{slotId}",
            "rowVersion",
            rowVersion ?? string.Empty);
        return _api.DeleteAsync(url, ct);
    }
}
