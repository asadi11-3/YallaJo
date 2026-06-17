using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;

using Accounts.Contracts.Authorization;
using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
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
/// Patch 1C — verifies magic-byte / file-signature validation on the provider
/// document upload endpoint (POST /api/v1/provider/documents/upload).
///
/// Sensitive provider application documents (national IDs, business licenses)
/// must not be trusted by extension or declared Content-Type alone. The
/// ProviderDocumentContentInspector inspects the leading bytes and rejects any
/// upload whose real signature does not agree with BOTH the declared
/// Content-Type AND the file extension. Supported signatures: PDF (%PDF),
/// JPEG (FF D8 FF), PNG (89 50 4E 47 0D 0A 1A 0A).
///
/// Rejections must surface a SAFE validation message (HTTP 400) that never
/// exposes physical paths, storage keys, or internal exception detail. A valid
/// upload must still succeed end-to-end (201). The real host runs the full auth
/// + endpoint pipeline; only the repository, storage, and unit-of-work are
/// stubbed so no database or disk is needed.
/// </summary>
public sealed class ProviderDocumentUploadValidationTests
{
    private static readonly Guid OwnerUserId = Guid.Parse("0a000000-0000-0000-0000-0000000000a1");

    // ── Fixtures (real signatures) ───────────────────────────────────────────

    // "%PDF-1.4\n…"
    private static byte[] ValidPdf() =>
        [.. "%PDF-1.4\n%\u00E2\u00E3\u00CF\u00D3\n1 0 obj\n<<>>\nendobj\n"u8.ToArray()];

    // FF D8 FF E0 … FF D9 (minimal JFIF-ish JPEG envelope).
    private static byte[] ValidJpeg() =>
        [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0xFF, 0xD9];

    // 89 50 4E 47 0D 0A 1A 0A + minimal IHDR chunk header.
    private static byte[] ValidPng() =>
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
    ];

    // Not a PDF at all — plain ASCII payload.
    private static byte[] NotAPdf() => [.. "NOTPDF this is not a real document"u8.ToArray()];

    // ── Accepted uploads ───────────────────────────────────────────────────────

    [Fact]
    public async Task Valid_pdf_is_accepted()
    {
        var response = await UploadAsync(ValidPdf(), "doc.pdf", "application/pdf");
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Valid_jpeg_is_accepted()
    {
        var response = await UploadAsync(ValidJpeg(), "photo.jpg", "image/jpeg");
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Valid_png_is_accepted()
    {
        var response = await UploadAsync(ValidPng(), "scan.png", "image/png");
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── Rejected uploads ─────────────────────────────────────────────────────

    [Fact]
    public async Task Fake_pdf_bytes_are_rejected()
    {
        // .pdf extension + application/pdf content-type, but the bytes are not a PDF.
        var response = await UploadAsync(NotAPdf(), "fake.pdf", "application/pdf");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Extension_content_mismatch_is_rejected()
    {
        // Real PNG bytes, but declared as a .pdf upload.
        var response = await UploadAsync(ValidPng(), "claim.pdf", "application/pdf");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ContentType_signature_mismatch_is_rejected()
    {
        // Real PDF bytes, but declared as image/png with a .png extension.
        var response = await UploadAsync(ValidPdf(), "doc.png", "image/png");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Rejection_body_does_not_leak_server_or_storage_paths()
    {
        var response = await UploadAsync(NotAPdf(), "fake.pdf", "application/pdf");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();

        // The safe inspector message is surfaced…
        body.Should().Contain("does not match its type");
        // …and no physical path / storage key / internal detail leaks.
        body.Should().NotContain("wwwroot");
        body.Should().NotContain("C:\\");
        body.Should().NotContain("provider-application-documents");
        body.Should().NotContain(".bin");
        body.Should().NotContainEquivalentOf("Exception");
        body.Should().NotContainEquivalentOf("StackTrace");
    }

    // ── Size cap is unchanged (rejected by validator, not by the inspector) ─────

    [Fact]
    public async Task Oversized_upload_is_still_rejected()
    {
        // 11 MB of valid PDF — exceeds the 10 MB FluentValidation cap.
        var bytes = new byte[11 * 1024 * 1024];
        var pdf = ValidPdf();
        Array.Copy(pdf, bytes, pdf.Length);

        var response = await UploadAsync(bytes, "big.pdf", "application/pdf");

        response.StatusCode.Should().NotBe(HttpStatusCode.Created);
        // It must be a client error, and must not be the inspector's mismatch message
        // (the size cap is enforced earlier, by validation).
        ((int)response.StatusCode).Should().BeGreaterThanOrEqualTo(400).And.BeLessThan(500);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("wwwroot");
        body.Should().NotContain("C:\\");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static async Task<HttpResponseMessage> UploadAsync(byte[] bytes, string fileName, string contentType)
    {
        var ownerApp = BuildOwnerApplication(OwnerUserId);

        await using var factory = new UploadFactory
        {
            CallerUserId = OwnerUserId,
            OwnerApplication = ownerApp,
        };

        using var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(nameof(DocumentType.BusinessLicense)), "documentType");

        return await client.PostAsync("/api/v1/provider/documents/upload", content);
    }

    private static ProviderApplication BuildOwnerApplication(Guid ownerUserId)
    {
        var register = ProviderApplication.Register(
            ownerUserId,
            ProviderType.TourOperator,
            businessName: "Acme Tours",
            contactEmail: "owner@example.com",
            contactPhone: "+97400000000",
            address: "Doha",
            description: "Tours");

        register.IsSuccess.Should().BeTrue();
        return register.Value!;
    }

    /// <summary>
    /// Boots the real API host with a stubbed repository / storage / unit-of-work
    /// and a test auth scheme that grants the upload permission
    /// (ProviderApplication, Create) to the configured caller.
    /// </summary>
    private sealed class UploadFactory : WebApplicationFactory<Program>
    {
        private const string FakeJwtKey =
            "yallajo-test-only-fake-jwt-signing-key-do-not-use-in-production-0123456789";
        private const string FakeExternalAuthSigningKey =
            "yallajo-test-only-fake-externalauth-signing-key-do-not-use-in-production-0123456789";

        public Guid CallerUserId { get; init; }

        /// <summary>Application returned by GetWithDocumentsByUserIdAsync(CallerUserId).</summary>
        public ProviderApplication? OwnerApplication { get; init; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Seeding:Enabled", "false");
            builder.UseSetting("Jwt:Key", FakeJwtKey);
            builder.UseSetting("ExternalAuth:SigningKey", FakeExternalAuthSigningKey);

            builder.ConfigureTestServices(services =>
            {
                UploadAuthState.CallerUserId = CallerUserId;

                services.AddAuthentication(UploadTestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, UploadTestAuthHandler>(
                        UploadTestAuthHandler.SchemeName, _ => { });

                services.PostConfigureAll<AuthenticationOptions>(o =>
                {
                    o.DefaultAuthenticateScheme = UploadTestAuthHandler.SchemeName;
                    o.DefaultChallengeScheme = UploadTestAuthHandler.SchemeName;
                    o.DefaultScheme = UploadTestAuthHandler.SchemeName;
                    o.DefaultForbidScheme = UploadTestAuthHandler.SchemeName;
                });

                // Stub the repository so the caller "owns" an application.
                services.RemoveAll<IProviderApplicationRepository>();
                services.AddScoped<IProviderApplicationRepository>(_ =>
                {
                    var repo = Substitute.For<IProviderApplicationRepository>();
                    repo.GetWithDocumentsByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                        .Returns(_ => Task.FromResult(OwnerApplication));
                    return repo;
                });

                // Stub storage so an ACCEPTED upload does not touch the disk.
                services.RemoveAll<IFileStorageService>();
                services.AddSingleton<IFileStorageService>(_ => new StubFileStorage());

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

    private static class UploadAuthState
    {
        public static Guid CallerUserId;
    }

    private sealed class UploadTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "AccountsProviderUploadTestScheme";

        public UploadTestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, UploadAuthState.CallerUserId.ToString()),
                new("sub", UploadAuthState.CallerUserId.ToString()),
                // Upload requires the Create permission on provider application data.
                new("Permission",
                    PermissionPolicyNames.Build(AccountsFeatures.ProviderApplication, AppAction.Create)),
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class StubFileStorage : IFileStorageService
    {
        public Task<Result<FileUploadResult>> UploadAsync(
            Stream stream, string fileName, string contentType, string folder, CancellationToken ct = default)
            => Task.FromResult(Result<FileUploadResult>.Success(
                new FileUploadResult($"/uploads/{folder}/x.bin", $"{folder}/x.bin", stream.Length)));

        public Task<bool> DeleteAsync(string fileUrl, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<string> GetAccessUrlAsync(string fileUrl, TimeSpan? expiry = null, CancellationToken ct = default)
            => Task.FromResult(fileUrl);

        public Task<Result<FileDownload>> OpenReadAsync(string fileUrl, CancellationToken ct = default)
            => Task.FromResult(Result<FileDownload>.Success(
                new FileDownload(new MemoryStream([0x25, 0x50, 0x44, 0x46], writable: false), "application/pdf", 4)));
    }
}
