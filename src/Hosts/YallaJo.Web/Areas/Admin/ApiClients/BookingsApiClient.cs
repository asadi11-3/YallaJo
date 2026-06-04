using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.Bookings;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class BookingsApiClient
{
    private readonly IApiClient _api;

    public BookingsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<AdminBookingsPageResponse>> GetAdminBookingsAsync(
        string? status,
        string? fromDate,
        string? toDate,
        Guid? userId,
        Guid? providerId,
        Guid? tourId,
        string? cursor,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };

        if (!string.IsNullOrWhiteSpace(status))
        {
            query["status"] = status;
        }

        if (!string.IsNullOrWhiteSpace(fromDate))
        {
            query["fromDate"] = fromDate;
        }

        if (!string.IsNullOrWhiteSpace(toDate))
        {
            query["toDate"] = toDate;
        }

        if (userId is { } uid && uid != Guid.Empty)
        {
            query["userId"] = uid.ToString("D", CultureInfo.InvariantCulture);
        }

        if (providerId is { } pid && pid != Guid.Empty)
        {
            query["providerId"] = pid.ToString("D", CultureInfo.InvariantCulture);
        }

        if (tourId is { } tid && tid != Guid.Empty)
        {
            query["tourId"] = tid.ToString("D", CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            query["cursor"] = cursor;
        }

        var url = QueryHelpers.AddQueryString("/api/v1/booking/admin/all", query);
        return _api.GetAsync<AdminBookingsPageResponse>(url, ct);
    }

    public Task<ApiResult> ForceRefundAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var path = $"/api/v1/admin/bookings/{id.ToString("D", CultureInfo.InvariantCulture)}/force-refund";
        return _api.PostAsync(path, new { reason }, ct);
    }
}
