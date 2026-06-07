using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Authenticated runtime smoke for the provider booking details page (FE-2B):
/// boots the real web host, stubs auth + IApiClient, and verifies the permission-gated
/// "Mark as completed" action only renders for a Confirmed booking when the user holds
/// Permission.TourBooking.Complete — and that the confirmation copy is present.
/// </summary>
public sealed class ProviderBookingCompletionSmokeTests
{
    private const string ConfirmedBookingJson = """
        {
          "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          "reference": "BK-12345678",
          "status": "Confirmed",
          "userId": "11111111-1111-1111-1111-111111111111",
          "tourId": "33333333-3333-3333-3333-333333333333",
          "providerId": "44444444-4444-4444-4444-444444444444",
          "availabilitySlotId": "55555555-5555-5555-5555-555555555555",
          "participantCount": 2,
          "pricing": { "subtotal": 100, "discountAmount": 0, "totalAmount": 100, "currency": "JOD", "lineItems": [] },
          "isInstantBooking": true,
          "createdAt": "2026-01-01T00:00:00Z"
        }
        """;

    [Fact]
    public async Task ConfirmedBooking_WithCompletePermission_ShowsMarkAsCompleted_AndConfirmationCopy()
    {
        using var factory = new ProviderWebFactory();
        var client = factory.CreateClientFor(["Permission.TourBooking.Complete"], ConfirmedBookingJson);

        var response = await client.GetAsync("/provider/bookings/manage/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("Mark as completed", "the Complete action must render for a Confirmed booking with the permission");
        html.Should().Contain("48-hour dispute window", "the confirmation copy must be present");
        html.Should().Contain("/provider/bookings/manage/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/complete");
        html.Should().NotContain("asp-action", "tag helpers must be processed, not emitted raw");
    }

    [Fact]
    public async Task ConfirmedBooking_WithoutCompletePermission_HidesMarkAsCompleted()
    {
        using var factory = new ProviderWebFactory();
        var client = factory.CreateClientFor(["Permission.SomethingElse.Read"], ConfirmedBookingJson);

        var response = await client.GetAsync("/provider/bookings/manage/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().NotContain("Mark as completed",
            "the Complete action must be hidden when TourBooking.Complete is absent");
    }

    // ── Test host ─────────────────────────────────────────────────────────────

    internal sealed class ProviderWebFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Api:BaseUrl", "http://127.0.0.1:1");

            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<TestAuthSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

                services.PostConfigureAll<AuthenticationOptions>(o =>
                {
                    o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    o.DefaultScheme = TestAuthHandler.SchemeName;
                });

                services.RemoveAll<IApiClient>();
                services.AddScoped<IApiClient, StubApiClient>();
            });
        }

        public HttpClient CreateClientFor(string[] permissions, string? bookingJson)
        {
            var factory = WithWebHostBuilder(b =>
                b.ConfigureServices(s =>
                {
                    s.Configure<TestAuthState>(state => state.Permissions = permissions);
                    s.Configure<TestApiState>(state => state.BookingJson = bookingJson);
                }));

            return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }
    }

    private sealed class TestAuthState { public string[] Permissions { get; set; } = []; }
    private sealed class TestApiState { public string? BookingJson { get; set; } }
    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions { }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "ProviderBookingSmokeScheme";
        private readonly TestAuthState _state;

        public TestAuthHandler(
            IOptionsMonitor<TestAuthSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOptions<TestAuthState> state)
            : base(options, logger, encoder) => _state = state.Value;

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var jwt = new JwtSecurityToken(claims: _state.Permissions.Select(p => new Claim("Permission", p)));
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            var claims = new[]
            {
                new Claim("sub", "99999999-9999-9999-9999-999999999999"),
                new Claim(ClaimTypes.Name, "Smoke Provider"),
                new Claim("access_token", accessToken),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class StubApiClient : IApiClient
    {
        private readonly TestApiState _state;
        public StubApiClient(IOptions<TestApiState> state) => _state = state.Value;

        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            // Booking detail read.
            if (path.StartsWith("/api/v1/booking/", StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(_state.BookingJson))
            {
                return Deserialize<T>(_state.BookingJson);
            }

            // Tour name hydration.
            if (path.StartsWith("/api/v1/tours/", StringComparison.Ordinal))
            {
                return Deserialize<T>(
                    """{"id":"33333333-3333-3333-3333-333333333333","name":"Smoke Tour"}""");
            }

            return Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));
        }

        private static Task<ApiResult<T>> Deserialize<T>(string json)
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Task.FromResult(data is null
                ? ApiResult<T>.Fail(200, "Could not parse response body.")
                : ApiResult<T>.Ok(data, 200));
        }

        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<ApiFile>.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
        public Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PutAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PutFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PostFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
    }
}
