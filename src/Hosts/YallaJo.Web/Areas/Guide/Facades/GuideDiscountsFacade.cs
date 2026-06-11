using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.Discounts;
using YallaJo.Web.Areas.Guide.Services;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

public sealed class GuideDiscountsFacade
{
    private const int TourOptionsPageSize = 50;

    private readonly DiscountsApiClient _api;
    private readonly GuideIdAccessor _guideId;
    private readonly ILogger<GuideDiscountsFacade> _logger;

    public GuideDiscountsFacade(DiscountsApiClient api, GuideIdAccessor guideId, ILogger<GuideDiscountsFacade> logger)
    {
        _api = api;
        _guideId = guideId;
        _logger = logger;
    }

    public async Task<ApiResult<DiscountsVm>> GetAsync(CancellationToken ct = default)
    {
        // UI-PERF-API1: fetch discounts and the guide's tours (F10 picker options) in parallel.
        var discountsTask = _api.GetMyDiscountsAsync(ct);
        var tourOptionsTask = FetchTourOptionsAsync(ct);
        await Task.WhenAll(discountsTask, tourOptionsTask);

        var result = await discountsTask;
        var tourOptions = await tourOptionsTask;
        if (result.RequireSignOut)
        {
            return ApiResult<DiscountsVm>.ForceSignOut();
        }

        if (!result.IsSuccess)
        {
            return ApiResult<DiscountsVm>.Fail(result.StatusCode, result.Error);
        }

        var discounts = (result.Data ?? [])
            .OrderByDescending(d => d.IsActive)
            .ThenByDescending(d => d.ValidFrom)
            .Select(d => new DiscountRowVm(
                d.Id,
                d.Name,
                d.Description,
                d.DiscountType,
                d.DiscountValue,
                d.Currency,
                d.ValidFrom,
                d.ValidUntil,
                d.MaxUsageCount,
                d.CurrentUsageCount,
                d.IsActive))
            .ToList();

        return ApiResult<DiscountsVm>.Ok(new DiscountsVm { Discounts = discounts, TourOptions = tourOptions });
    }

    private async Task<IReadOnlyList<TourOptionVm>> FetchTourOptionsAsync(CancellationToken ct)
    {
        // UI-ERR3: picker options are an enhancement — degrade to an empty list
        // (the select still renders with just the placeholder) on any failure.
        try
        {
            if (await _guideId.GetGuideIdAsync(ct) is not { } guideId)
            {
                return [];
            }

            var result = await _api.GetMyToursAsync(guideId, page: 1, pageSize: TourOptionsPageSize, ct);
            if (result is not { IsSuccess: true, Data: not null })
            {
                return [];
            }

            return result.Data.Items
                .Select(t => new TourOptionVm(t.TourId, string.IsNullOrWhiteSpace(t.Title) ? t.TourId.ToString() : t.Title))
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to load tour options for the discounts form.");
            return [];
        }
    }

    public async Task<ApiResult> CreateAsync(CreateDiscountFormVm form, CancellationToken ct = default)
    {
        var description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        var currency = string.IsNullOrWhiteSpace(form.Currency) ? "JOD" : form.Currency.Trim().ToUpperInvariant();
        var request = new CreateGuideDiscountRequest(
            form.TourId,
            form.Name.Trim(),
            description,
            form.DiscountType,
            form.DiscountValue,
            currency,
            form.ValidFrom,
            form.ValidUntil,
            form.MaxUsageCount);

        try
        {
            return await _api.CreateAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create guide discount.");
            return ApiResult.Fail(500, "Unable to create the discount. Please try again.");
        }
    }

    public async Task<ApiResult> UpdateAsync(Guid id, EditDiscountFormVm form, CancellationToken ct = default)
    {
        var request = new UpdateGuideDiscountRequest(
            form.Name.Trim(),
            string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
            form.DiscountValue,
            form.ValidFrom,
            form.ValidUntil,
            form.MaxUsageCount);

        try
        {
            return await _api.UpdateAsync(id, request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update guide discount {Id}.", id);
            return ApiResult.Fail(500, "Unable to update the discount. Please try again.");
        }
    }

    public async Task<ApiResult> DeactivateAsync(Guid id, CancellationToken ct = default) =>
        await _api.DeactivateAsync(id, ct);
}
