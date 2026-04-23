using YallaJo.Web.Areas.Auth.Features.ExternalProviders.Requests;
using YallaJo.Web.Areas.Auth.Features.ExternalProviders.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Authentication.ExternalAuth;
using YallaJo.Web.Infrastructure.Authentication.SignIn;

namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders;

public sealed class ExternalProvidersFacade
{
    private readonly ExternalProvidersApiClient _api;
    private readonly IWebSignInService          _signIn;
    private readonly IExternalAuthTicketBuilder _ticketBuilder;

    public ExternalProvidersFacade(
        ExternalProvidersApiClient api,
        IWebSignInService signIn,
        IExternalAuthTicketBuilder ticketBuilder)
    {
        _api           = api;
        _signIn        = signIn;
        _ticketBuilder = ticketBuilder;
    }

    public string BuildTicket(
        string provider,
        string providerUserId,
        string? email,
        bool emailVerifiedByProvider,
        string? firstName = null,
        string? lastName = null) =>
        _ticketBuilder.Build(
            provider, providerUserId, email, emailVerifiedByProvider, firstName, lastName);

    public async Task<ApiResult> LinkAsync(
        string ticket,
        string recaptchaToken,
        CancellationToken ct = default)
    {
        var result = await _api.LinkAsync(
        new LinkExternalProviderRequest
        {
            Ticket = ticket,
            RecaptchaToken = recaptchaToken,
        }, ct);

        if (result.IsSuccess) return ApiResult.Ok();

        if (result.IsUnauthorized)
        {
            await _signIn.SignOutAsync();
            return ApiResult.ForceSignOut();
        }

        if (result.IsConflict)
            return ApiResult.Fail(result.Error ?? "This provider account is already linked.");

        if (result.IsValidationError)
            return ApiResult.Invalid(result.ValidationErrors!);

        return ApiResult.Fail(result.Error ?? "Link failed.");
    }

    public async Task<ExternalLoginOutcome> LoginAsync(
        string ticket,
        string recaptchaToken,
        CancellationToken ct = default)
    {
        var result = await _api.LoginAsync(
        new ExternalLoginRequest
        {
            Ticket = ticket,
            RecaptchaToken = recaptchaToken,
        }, ct);

        if (result.IsSuccess && result.Data is not null)
        {
            var d = result.Data;
            await _signIn.SignInAsync(d.UserId, d.AccessToken, d.RefreshToken, d.RefreshTokenExpiresAt);
            return ExternalLoginOutcome.Ok();
        }

        if (result.IsUnauthorized)
            return ExternalLoginOutcome.NotLinked();

        if (result.IsTooManyRequests)
            return ExternalLoginOutcome.Fail("Too many sign-in attempts. Please try again shortly.");

        return ExternalLoginOutcome.Fail(result.Error ?? "External sign-in failed.");
    }

    public async Task<ApiResult> UnlinkAsync(Guid providerId, CancellationToken ct = default)
    {
        var result = await _api.UnlinkAsync(providerId, ct);

        if (result.IsSuccess || result.IsNotFound) return ApiResult.Ok();

        if (result.IsUnauthorized)
        {
            await _signIn.SignOutAsync();
            return ApiResult.ForceSignOut();
        }

        return ApiResult.Fail(result.Error ?? "Unlink failed.");
    }
}
