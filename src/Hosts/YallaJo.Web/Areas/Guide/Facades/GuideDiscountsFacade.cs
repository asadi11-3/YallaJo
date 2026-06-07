using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.Discounts;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

public sealed class GuideDiscountsFacade
{
    private readonly DiscountsApiClient _api;
    private readonly ILogger<GuideDiscountsFacade> _logger;

    public GuideDiscountsFacade(DiscountsApiClient api, ILogger<GuideDiscountsFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<DiscountsVm>> GetAsync(CancellationToken ct = default)
    {
        var result = await _api.GetMyDiscountsAsync(ct);
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

        return ApiResult<DiscountsVm>.Ok(new DiscountsVm { Discounts = discounts });
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
