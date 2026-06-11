using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Bookings;
using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Bookings;
using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Bookings;
using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Bookings;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.Facades;

public sealed class BookingsFacade
{
    private readonly BookingsApiClient _api;
    private readonly IApiAssetUrlResolver _assetResolver;

    public BookingsFacade(BookingsApiClient api, IApiAssetUrlResolver assetResolver)
    {
        _api = api;
        _assetResolver = assetResolver;
    }

    public static readonly string[] Tabs = ["Upcoming", "Canceled", "Completed"];

    private static string[] StatusesFor(string tab) => tab switch
    {
        "Canceled" => ["Cancelled"],
        "Completed" => ["Completed"],
        _ => ["AwaitingPayment", "PendingConfirmation", "Confirmed"],
    };

    private static string NormalizeTab(string? tab) =>
        Tabs.FirstOrDefault(t => string.Equals(t, tab, StringComparison.OrdinalIgnoreCase)) ?? "Upcoming";

    public async Task<ApiResult<BookingsVm>> GetBookingsAsync(string? tab, string? fromDate = null, string? toDate = null, CancellationToken ct = default)
    {
        var activeTab = NormalizeTab(tab);
        var result = await _api.GetMyBookingsAsync(StatusesFor(activeTab), fromDate, toDate, ct);

        if (result.IsUnauthorized)
            return ApiResult<BookingsVm>.ForceSignOut();
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<BookingsVm>.Fail(result.StatusCode, result.Error ?? "Could not load bookings.");

        var cards = await Task.WhenAll(result.Data.Items.Select(item => BuildCardAsync(item, ct)));

        return ApiResult<BookingsVm>.Ok(new BookingsVm
        {
            ActiveTab = activeTab,
            Bookings = cards,
            Tabs = Tabs,
            FromDate = fromDate,
            ToDate = toDate,
        });
    }

    private async Task<BookingCardVm> BuildCardAsync(MyBookingItemResponse item, CancellationToken ct)
    {
        var (tourName, imageUrl) = await HydrateTourAsync(item.TourId, ct);
        return BookingsMapper.ToCardVm(item, tourName, imageUrl);
    }

    private async Task<(string Name, string? ImageUrl)> HydrateTourAsync(Guid tourId, CancellationToken ct)
    {
        var name = "Tour booking";
        string? imageUrl = null;
        try
        {
            var tourTask = _api.GetTourAsync(tourId, ct);
            var attachTask = _api.GetAttachmentsAsync("Tour", tourId, ct);
            await Task.WhenAll(tourTask, attachTask);

            var tourResult = await tourTask;
            var attachResult = await attachTask;

            if (tourResult is { IsSuccess: true, Data: { } tour } && !string.IsNullOrWhiteSpace(tour.Name))
                name = tour.Name;

            if (attachResult is { IsSuccess: true, Data: { Count: > 0 } images })
            {
                var primary = images.OrderBy(a => a.SortOrder).First();
                imageUrl = _assetResolver.Resolve(primary.ThumbnailUrl ?? primary.Url);
            }
        }
        catch
        {
            // tolerate per-item hydration failure; fall back to defaults
        }

        return (name, imageUrl);
    }

    public async Task<ApiResult<BookingDetailVm>> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.GetBookingAsync(id, ct);

        if (result.IsUnauthorized)
            return ApiResult<BookingDetailVm>.ForceSignOut();
        if (result.IsNotFound)
            return ApiResult<BookingDetailVm>.Fail(404, "Booking not found.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<BookingDetailVm>.Fail(result.StatusCode, result.Error ?? "Could not load booking.");

        var d = result.Data;
        var (tourName, imageUrl) = await HydrateTourAsync(d.TourId, ct);

        return ApiResult<BookingDetailVm>.Ok(BookingsMapper.ToDetailVm(d, tourName, imageUrl));
    }

    public async Task<ApiResult> CancelAsync(Guid id, string? reason, CancellationToken ct = default)
    {
        var result = await _api.CancelAsync(id, new CancelBookingRequest(reason), ct);

        if (result.IsUnauthorized)
            return ApiResult.ForceSignOut();
        if (result.IsNotFound)
            return ApiResult.Fail(404, "Booking not found.");
        if (result.IsConflict)
            return ApiResult.Fail(409, result.Error ?? "This booking can no longer be cancelled.");
        if (result.IsValidationError)
            return ApiResult.Invalid(result.ValidationErrors!);
        if (!result.IsSuccess)
            return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not cancel booking.");

        return ApiResult.Ok();
    }

    /// <summary>
    /// FE-1A: the booking owner opens a dispute on a Completed booking. Maps backend
    /// failures to friendly, recoverable messages:
    ///   403 → not the owner; 422 → outside the 48h window / wrong state; 409 → concurrency.
    /// </summary>
    public async Task<ApiResult> OpenDisputeAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var result = await _api.OpenDisputeAsync(id, new OpenBookingDisputeRequest(reason ?? string.Empty), ct);

        if (result.IsUnauthorized)
            return ApiResult.ForceSignOut();
        if (result.IsForbidden)
            return ApiResult.Fail(403, "You can only open a dispute on your own booking.");
        if (result.IsNotFound)
            return ApiResult.Fail(404, "Booking not found.");
        if (result.IsConflict)
            return ApiResult.Fail(409, "This booking was just updated. Please reload and try again.");

        // Field-level validation (FluentValidation surfaced a ValidationErrors dict).
        if (result.IsValidationError && result.ValidationErrors is { Count: > 0 })
            return ApiResult.Invalid(result.ValidationErrors);

        // Domain guard (wrong state / outside the 48h window) arrives as 422 without a
        // field dictionary — surface a friendly, actionable message.
        if (result.StatusCode is 422 or 400)
            return ApiResult.Fail(422, result.Error
                ?? "A dispute can only be opened within 48 hours of a completed tour.");

        if (!result.IsSuccess)
            return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not open the dispute.");

        return ApiResult.Ok();
    }
}
