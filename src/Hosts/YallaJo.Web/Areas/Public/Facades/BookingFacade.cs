using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Helpers;
using YallaJo.Web.Areas.Public.Models.Booking;
using YallaJo.Web.Areas.Public.Models.Tours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.Facades;

public sealed class BookingFacade
{
    private readonly BookingApiClient _api;
    private readonly IApiAssetUrlResolver _assetResolver;

    public BookingFacade(BookingApiClient api, IApiAssetUrlResolver assetResolver)
    {
        _api = api;
        _assetResolver = assetResolver;
    }

    public async Task<ApiResult<TourBookingVm>> GetBookingPageAsync(Guid tourId, CancellationToken ct = default)
    {
        var tourResult = await _api.GetTourAsync(tourId, ct);
        if (!tourResult.IsSuccess || tourResult.Data is null)
            return ApiResult<TourBookingVm>.Fail(tourResult.StatusCode, tourResult.Error ?? "Tour not found.");

        var tour = tourResult.Data;

        var availabilityTask = _api.GetAvailabilityAsync(tourId, ct);
        var imageTask = ResolveCoverAsync(tourId, ct);
        await Task.WhenAll(availabilityTask, imageTask);

        var slots = new List<BookingSlotVm>();
        var availability = await availabilityTask;
        if (availability is { IsSuccess: true, Data: { Items: { } groups } })
        {
            foreach (var group in groups)
            {
                foreach (var slot in group.Slots)
                {
                    slots.Add(new BookingSlotVm
                    {
                        Id = slot.Id,
                        Date = group.Date,
                        StartTime = slot.StartTime,
                        EndTime = slot.EndTime,
                        AvailableCount = slot.AvailableCount
                    });
                }
            }
        }

        var vm = new TourBookingVm
        {
            TourId = tour.Id,
            TourName = tour.Name,
            TourSlug = tour.Slug,
            ImageUrl = await imageTask,
            ShortDescription = tour.ShortDescription,
            Difficulty = tour.Difficulty,
            DurationMinutes = tour.DurationMinutes,
            MaxGroupSize = tour.MaxGroupSize,
            BasePrice = tour.BasePrice,
            SalePrice = tour.SalePrice,
            Currency = tour.Currency,
            AverageRating = (decimal)tour.AverageRating,
            ReviewCount = tour.ReviewCount,
            IsInstantBooking = tour.IsInstantBooking,
            CancellationPolicyHours = tour.CancellationPolicyHours,
            Slots = slots
        };

        return ApiResult<TourBookingVm>.Ok(vm);
    }

    public Task<ApiResult<CreateTourBookingResponse>> CreateAsync(TourBookingFormVm form, CancellationToken ct = default)
    {
        var request = new CreateTourBookingRequest(
            form.TourId,
            null,
            form.AvailabilitySlotId,
            new ParticipantBreakdownRequest(form.Adults, form.Children, form.Infants, form.Seniors),
            form.IsPrivate,
            string.IsNullOrWhiteSpace(form.PromoCode) ? null : form.PromoCode.Trim(),
            0,
            string.IsNullOrWhiteSpace(form.SpecialRequests) ? null : form.SpecialRequests.Trim());

        return _api.CreateBookingAsync(request, ct);
    }

    public async Task<ApiResult<BookingConfirmVm>> GetConfirmAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.GetBookingAsync(id, ct);
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<BookingConfirmVm>.Fail(result.StatusCode, result.Error ?? "Booking not found.");

        var d = result.Data;
        var pricing = d.Pricing;

        var tourName = "Your tour";
        string? imageUrl = null;
        try
        {
            var tour = await _api.GetTourAsync(d.TourId, ct);
            if (tour is { IsSuccess: true, Data: { } t })
                tourName = t.Name;
            imageUrl = await ResolveCoverAsync(d.TourId, ct);
        }
        catch
        {
            // best-effort hydration
        }

        var vm = new BookingConfirmVm
        {
            Id = d.Id,
            Reference = d.Reference,
            Status = d.Status,
            TourName = tourName,
            ImageUrl = imageUrl,
            ParticipantCount = d.ParticipantCount,
            IsInstantBooking = d.IsInstantBooking,
            SpecialRequests = d.SpecialRequests,
            CreatedAt = d.CreatedAt,
            PaymentExpiresAt = d.PaymentExpiresAt,
            Subtotal = pricing?.Subtotal ?? 0m,
            DiscountAmount = pricing?.DiscountAmount ?? 0m,
            LoyaltyAmount = pricing?.LoyaltyAmount ?? 0m,
            TotalAmount = pricing?.TotalAmount ?? 0m,
            Currency = pricing?.Currency ?? string.Empty,
            LineItems = pricing?.LineItems?
                .Select(li => new BookingConfirmLineVm { TierType = li.TierType, Count = li.Count, UnitPrice = li.UnitPrice })
                .ToList() ?? [],
            IsCancelled = d.Cancellation is not null,
            CancelledAt = d.Cancellation?.CancelledAt,
            RefundAmount = d.Cancellation?.RefundAmount
        };

        return ApiResult<BookingConfirmVm>.Ok(vm);
    }

    // The tour cover exposes no public image field and the attachment endpoint is
    // not anonymous-accessible (the Book GET page is anonymous), so use a
    // deterministic theme placeholder (temporary public image API gap — see
    // PublicImagePlaceholder).
    private static Task<string?> ResolveCoverAsync(Guid tourId, CancellationToken ct)
        => Task.FromResult<string?>(PublicImagePlaceholder.ResolveTourImage(tourId));
}
