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
                        AvailableCount = slot.AvailableCount,
                        TourGuideId = slot.TourGuideId
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

    public async Task<ApiResult<CreateTourBookingResponse>> CreateAsync(
        TourBookingFormVm form, CancellationToken ct = default)
    {
        // Resolve the guide server-side from the chosen slot (never trust the posted
        // GuideId alone). Booking requires a non-empty GuideId; if the slot can't be
        // resolved or carries no guide, surface a friendly message instead of a raw 400.
        var guideId = await ResolveSlotGuideAsync(form.TourId, form.AvailabilitySlotId, form.GuideId, ct);
        if (guideId is null || guideId == Guid.Empty)
        {
            return ApiResult<CreateTourBookingResponse>.Fail(
                400, "This tour has no guide available for the selected time. Please pick another slot.");
        }

        var request = new CreateTourBookingRequest(
            form.TourId,
            guideId,
            form.AvailabilitySlotId,
            new ParticipantBreakdownRequest(form.Adults, form.Children, form.Infants, form.Seniors),
            form.IsPrivate,
            string.IsNullOrWhiteSpace(form.PromoCode) ? null : form.PromoCode.Trim(),
            0,
            string.IsNullOrWhiteSpace(form.SpecialRequests) ? null : form.SpecialRequests.Trim());

        var result = await _api.CreateBookingAsync(request, ct);
        if (result.IsSuccess)
            return result;

        // Map common booking failures to friendly, actionable messages.
        var friendly = result.StatusCode switch
        {
            401 => null, // let the controller's GuardSignOut handle re-auth
            404 => "This tour or time slot is no longer available. Please choose another.",
            409 => result.Error ?? "That time slot was just taken or changed. Please pick another and try again.",
            400 or 422 => result.Error ?? "Some booking details are invalid. Please review and try again.",
            _ => result.Error ?? "We could not create your booking. Please try again.",
        };

        return result.StatusCode == 401
            ? ApiResult<CreateTourBookingResponse>.ForceSignOut()
            : ApiResult<CreateTourBookingResponse>.Fail(result.StatusCode, friendly);
    }

    /// <summary>
    /// Re-reads the tour's availability and returns the guide assigned to the chosen
    /// slot. Falls back to the posted guide only if it matches a real slot guide.
    /// </summary>
    private async Task<Guid?> ResolveSlotGuideAsync(
        Guid tourId, Guid slotId, Guid postedGuideId, CancellationToken ct)
    {
        try
        {
            var availability = await _api.GetAvailabilityAsync(tourId, ct);
            if (availability is { IsSuccess: true, Data.Items: { } groups })
            {
                foreach (var group in groups)
                {
                    foreach (var slot in group.Slots)
                    {
                        if (slot.Id == slotId)
                            return slot.TourGuideId == Guid.Empty ? null : slot.TourGuideId;
                    }
                }
            }
        }
        catch
        {
            // tolerate availability fetch failure — fall through to posted value
        }

        return postedGuideId == Guid.Empty ? null : postedGuideId;
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
