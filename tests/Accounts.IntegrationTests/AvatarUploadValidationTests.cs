using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;

using Accounts.Contracts.Authorization;
using Accounts.Domain.Entities;
using Accounts.Domain.Repositories;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace YallaJo.Accounts.IntegrationTests;

/// <summary>
/// M1 — verifies avatar upload content validation on the profile avatar endpoint
/// (PUT /api/v1/accounts/profile/avatar).
///
/// User avatars must accept only JPEG, PNG and WEBP and must validate extension,
/// declared Content-Type AND the real magic bytes; spoofed/mismatched files are
/// rejected with HTTP 422 and nothing is uploaded. Oversized files (> 5 MB) are
/// also rejected with 422. A valid image upload succeeds (200). The real host
/// runs the full auth + endpoint pipeline; only the repository, storage and
/// unit-of-work are stubbed so no database or disk is needed.
/// </summary>
public sealed class AvatarUploadValidationTests
{
    private static readonly Guid CallerUserId = Guid.Parse("0a000000-0000-0000-0000-0000000000b7");

    // ── Fixtures (real signatures) ───────────────────────────────────────────

    private static byte[] ValidJpeg() =>
        [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0xFF, 0xD9];

    private static byte[] ValidPng() =>
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
    ];

    private static byte[] ValidWebp() =>
    [
        0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00,
        0x57, 0x45, 0x42, 0x50, 0x56, 0x50, 0x38, 0x20,
    ];

    private static byte[] NotAnImage() => [.. "this is just text, not an image"u8.ToArray()];

    // ── Accepted uploads ─────────────────────────────────────────────────────

    [Fact]
    public async Task Valid_jpeg_avatar_is_accepted()
    {
        var response = await UploadAsync(ValidJpeg(), "avatar.jpg", "image/jpeg");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Valid_png_avatar_is_accepted()
    {
        var response = await UploadAsync(ValidPng(), "avatar.png", "image/png");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Valid_webp_avatar_is_accepted()
    {
        var response = await UploadAsync(ValidWebp(), "avatar.webp", "image/webp");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Rejected uploads (422) ───────────────────────────────────────────────

    [Fact]
    public async Task Garbage_bytes_are_rejected_with_422()
    {
        var response = await UploadAsync(NotAnImage(), "avatar.png", "image/png");
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Mismatched_signature_is_rejected_with_422()
    {
        // Real PNG bytes, but declared as JPEG (.jpg).
        var response = await UploadAsync(ValidPng(), "avatar.jpg", "image/jpeg");
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Gif_is_rejected_with_422()
    {
        var gif = "GIF89a________"u8.ToArray();
        var response = await UploadAsync(gif, "avatar.gif", "image/gif");
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Svg_is_rejected_with_422()
    {
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"u8.ToArray();
        var response = await UploadAsync(svg, "avatar.svg", "image/svg+xml");
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Oversized_avatar_is_rejected_with_422()
    {
        // 6 MB of valid PNG — exceeds the 5 MB cap.
        var bytes = new byte[6 * 1024 * 1024];
        var png = ValidPng();
        Array.Copy(png, bytes, png.Length);

        var response = await UploadAsync(bytes, "avatar.png", "image/png");
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Rejected_upload_does_not_touch_storage()
    {
        await using var factory = new AvatarFactory { CallerUserId = CallerUserId };
        using var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(NotAnImage());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "avatar.png");

        var response = await client.PutAsync("/api/v1/accounts/profile/avatar", content);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        factory.Storage.UploadCount.Should().Be(0);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task<HttpResponseMessage> UploadAsync(byte[] bytes, string fileName, string contentType)
    {
        await using var factory = new AvatarFactory { CallerUserId = CallerUserId };
        using var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        return await client.PutAsync("/api/v1/accounts/profile/avatar", content);
    }

    /// <summary>
    /// Boots the real API host with a stubbed profile repository / storage /
    /// unit-of-work and a test auth scheme that grants the avatar-update permission
    /// (Profile, Update) to the configured caller.
    /// </summary>
    private sealed class AvatarFactory : WebApplicationFactory<Program>
    {
        private const string FakeJwtKey =
            "yallajo-test-only-fake-jwt-signing-key-do-not-use-in-production-0123456789";
        private const string FakeExternalAuthSigningKey =
            "yallajo-test-only-fake-externalauth-signing-key-do-not-use-in-production-0123456789";

        public Guid CallerUserId { get; init; }

        public RecordingFileStorage Storage { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Seeding:Enabled", "false");
            builder.UseSetting("Jwt:Key", FakeJwtKey);
            builder.UseSetting("ExternalAuth:SigningKey", FakeExternalAuthSigningKey);

            builder.ConfigureTestServices(services =>
            {
                AvatarAuthState.CallerUserId = CallerUserId;

                services.AddAuthentication(AvatarTestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, AvatarTestAuthHandler>(
                        AvatarTestAuthHandler.SchemeName, _ => { });

                services.PostConfigureAll<AuthenticationOptions>(o =>
                {
                    o.DefaultAuthenticateScheme = AvatarTestAuthHandler.SchemeName;
                    o.DefaultChallengeScheme = AvatarTestAuthHandler.SchemeName;
                    o.DefaultScheme = AvatarTestAuthHandler.SchemeName;
                    o.DefaultForbidScheme = AvatarTestAuthHandler.SchemeName;
                });

                // Stub the repository so the caller owns a profile.
                services.RemoveAll<IProfileRepository>();
                services.AddScoped<IProfileRepository>(_ =>
                {
                    var repo = Substitute.For<IProfileRepository>();
                    repo.FirstOrDefaultAsync(
                            Arg.Any<Expression<Func<Profile, bool>>>(),
                            Arg.Any<Func<IQueryable<Profile>, IQueryable<Profile>>?>(),
                            Arg.Any<Func<IQueryable<Profile>, IOrderedQueryable<Profile>>?>(),
                            Arg.Any<bool>(),
                            Arg.Any<CancellationToken>())
                        .Returns(_ => Task.FromResult<Profile?>(Profile.Create(CallerUserId, "Test", "User")));
                    return repo;
                });

                // Stub storage so an ACCEPTED upload does not touch the disk and a
                // REJECTED upload can be asserted to have never called UploadAsync.
                services.RemoveAll<IFileStorageService>();
                services.AddSingleton<IFileStorageService>(Storage);

                // Stub the unit of work so SaveChanges succeeds without a database.
                services.RemoveAll<IAccountsUnitOfWork>();
                services.AddScoped<IAccountsUnitOfWork>(_ =>
                {
                    var uow = Substitute.For<IAccountsUnitOfWork>();
                    uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
                    return uow;
                });
            });
        }
    }

    private static class AvatarAuthState
    {
        public static Guid CallerUserId;
    }

    private sealed class AvatarTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "AccountsAvatarUploadTestScheme";

        public AvatarTestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, AvatarAuthState.CallerUserId.ToString()),
                new("sub", AvatarAuthState.CallerUserId.ToString()),
                new("Permission",
                    PermissionPolicyNames.Build(AccountsFeatures.Profile, AppAction.Update)),
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class RecordingFileStorage : IFileStorageService
    {
        public int UploadCount { get; private set; }

        public Task<Result<FileUploadResult>> UploadAsync(
            Stream stream, string fileName, string contentType, string folder, CancellationToken ct = default)
        {
            UploadCount++;
            return Task.FromResult(Result<FileUploadResult>.Success(
                new FileUploadResult($"/uploads/{folder}/x.bin", $"{folder}/x.bin", stream.Length)));
        }

        public Task<bool> DeleteAsync(string fileUrl, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<string> GetAccessUrlAsync(string fileUrl, TimeSpan? expiry = null, CancellationToken ct = default)
            => Task.FromResult(fileUrl);

        public Task<Result<FileDownload>> OpenReadAsync(string fileUrl, CancellationToken ct = default)
            => Task.FromResult(Result<FileDownload>.Failure(Error.NotFound("File"), Outcome.NotFound));

        public Task<Result<FileDownload>> OpenReadByStorageKeyAsync(string storageKey, CancellationToken ct = default)
            => Task.FromResult(Result<FileDownload>.Failure(Error.NotFound("File"), Outcome.NotFound));
    }
}
