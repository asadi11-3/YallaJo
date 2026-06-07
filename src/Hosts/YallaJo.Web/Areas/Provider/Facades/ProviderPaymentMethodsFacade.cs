using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.PaymentMethods;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public sealed class ProviderPaymentMethodsFacade
{
    private readonly PaymentMethodsApiClient _api;
    private readonly ILogger<ProviderPaymentMethodsFacade> _logger;

    public ProviderPaymentMethodsFacade(PaymentMethodsApiClient api, ILogger<ProviderPaymentMethodsFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<PaymentMethodsVm>> GetAsync(CancellationToken ct = default)
    {
        var result = await _api.GetMethodsAsync(ct);
        if (result.RequireSignOut)
            return ApiResult<PaymentMethodsVm>.ForceSignOut();
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<PaymentMethodsVm>.Fail(result.StatusCode, result.Error);

        var rows = result.Data
            .OrderByDescending(m => m.IsDefault)
            .ThenBy(m => m.DisplayName)
            .Select(ToRow)
            .ToList();

        return ApiResult<PaymentMethodsVm>.Ok(new PaymentMethodsVm { Methods = rows });
    }

    public async Task<ApiResult> CreateAsync(CreatePaymentMethodFormVm form, CancellationToken ct = default)
    {
        var request = new ProviderPaymentMethodRequest(
            form.PaymentMethodType,
            form.DisplayName.Trim(),
            form.AccountIdentifier.Trim(),
            NullIfBlank(form.BankName),
            form.IsDefault);

        try
        {
            return Normalize(await _api.CreateAsync(request, ct), "Could not add the payment method.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create provider payment method");
            return ApiResult.Fail("Could not add the payment method.");
        }
    }

    public async Task<ApiResult> UpdateAsync(Guid id, CreatePaymentMethodFormVm form, CancellationToken ct = default)
    {
        var request = new ProviderPaymentMethodRequest(
            form.PaymentMethodType,
            form.DisplayName.Trim(),
            form.AccountIdentifier.Trim(),
            NullIfBlank(form.BankName),
            form.IsDefault);

        try
        {
            return Normalize(await _api.UpdateAsync(id, request, ct), "Could not update the payment method.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update provider payment method {Id}", id);
            return ApiResult.Fail("Could not update the payment method.");
        }
    }

    public async Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return Normalize(await _api.DeleteAsync(id, ct), "Could not delete the payment method.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete provider payment method {Id}", id);
            return ApiResult.Fail("Could not delete the payment method.");
        }
    }

    private static PaymentMethodRowVm ToRow(ProviderPaymentMethodResponse r) => new(
        r.Id,
        r.PaymentMethodType,
        r.DisplayName,
        r.AccountIdentifier,
        r.BankName,
        r.IsDefault,
        r.IsVerified);

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess)
            return ApiResult.Ok();
        if (result.IsUnauthorized)
            return ApiResult.ForceSignOut();
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.Invalid(result.ValidationErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static ApiResult Normalize<T>(ApiResult<T> result, string fallback)
    {
        if (result.IsSuccess)
            return ApiResult.Ok();
        if (result.IsUnauthorized)
            return ApiResult.ForceSignOut();
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.Invalid(result.ValidationErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
