using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;

using Accounts.Contracts.Authorization;
using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;

using ContentCore.Contracts.Storage;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Security.Contracts.Authorization;

using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace YallaJo.Accounts.IntegrationTests;

/// <summary>
/// Patch 1A — verifies the authorized provider-document download endpoint
/// (GET /api/v1/provider/documents/{documentId}/download).
///
/// These tests prove the security guarantee that prompted the patch: a private
/// provider application document (national IDs, business licenses — PII) can be
/// streamed ONLY to its owner or an admin-tier user, and never to a cross-provider
/// caller or an anonymous request. The bytes are served by an ownership-checked
/// MediatR query handler, NOT by the public static-file middleware.
///
/// The host is real (so the full auth + endpoint pipeline runs); only the
/// <see cref="IProviderApplicationRepository"/> and <see cref="IFileStorageService"/>
/// are stubbed so the test does not need a database or real files on disk.
/// </summary>
public sealed class ProviderDocumentDownloadEndpointTests
{
    private static readonly byte[] KnownBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37]; // "%PDF-1.7"
    private const string KnownContentType = "application/pdf";
    private const string KnownFileName = "business-license.pdf";

    private static readonly Guid OwnerUserId = Guid.Parse("0a000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherUserId = Guid.Parse("0b000000-0000-0000-0000-000000000002");
    private static readonly Guid AdminUserId = Guid.Parse("0c000000-0000-0000-0000-000000000003");

    [Fact]
    public async Task Owner_can_download_their_own_document_and_receives_the_bytes()
    {
        // Build the owner's application with one document; capture its id.
        var (ownerApp, documentId) = BuildApplicationWithDocument(OwnerUserId);

        await using var factory = new DownloadFactory
        {
            CallerUserId = OwnerUserId,
            IsAdmin = false,
            // Owner path: repo finds the application by the caller's user id.
            ByUserId = ownerApp,
            ByDocumentId = null,
        };

        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/provider/documents/{documentId}/download");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(KnownContentType);
        var body = await response.Content.ReadAsByteArrayAsync();
        body.Should().Equal(KnownBytes);
    }

    [Fact]
    public async Task Cross_provider_caller_gets_404_and_no_bytes()
    {
        var (ownerApp, documentId) = BuildApplicationWithDocument(OwnerUserId);

        await using var factory = new DownloadFactory
        {
            CallerUserId = OtherUserId,
            IsAdmin = false,
            // The other provider has no application containing this document.
            ByUserId = null,
            // Even though the document exists, a non-admin must never reach the
            // by-document lookup — and the handler returns NotFound (not Forbidden)
            // to avoid leaking the document's existence (enumeration defence).
            ByDocumentId = ownerApp,
        };

        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/provider/documents/{documentId}/download");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Admin_can_download_any_providers_document()
    {
        var (ownerApp, documentId) = BuildApplicationWithDocument(OwnerUserId);

        await using var factory = new DownloadFactory
        {
            CallerUserId = AdminUserId,
            IsAdmin = true,
            // Admin is not the owner, so the by-user lookup returns nothing…
            ByUserId = null,
            // …but the admin path resolves the owning application by document id.
            ByDocumentId = ownerApp,
        };

        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/provider/documents/{documentId}/download");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsByteArrayAsync();
        body.Should().Equal(KnownBytes);
    }

    [Fact]
    public async Task Anonymous_request_is_rejected_with_401()
    {
        var (_, documentId) = BuildApplicationWithDocument(OwnerUserId);

        // No test auth scheme registered → the endpoint's RequireAuthorization()
        // challenges an unauthenticated request.
        await using var factory = new DownloadFactory
        {
            Anonymous = true,
            ByUserId = null,
            ByDocumentId = null,
        };

        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/provider/documents/{documentId}/download");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Patch 2C: read-source preference (FileAsset path vs legacy fallback) ──

    [Fact]
    public async Task FileAsset_path_serves_FileAsset_bytes_when_link_exists()
    {
        var (ownerApp, documentId) = BuildApplicationWithDocument(OwnerUserId);

        var fileAssetId = Guid.Parse("0d000000-0000-0000-0000-0000000000a1");
        var assetView = new FileAssetView(
            Id: fileAssetId,
            StorageProvider: "Local",
            StorageKey: "provider-application-documents/asset-abc.pdf",
            ContentType: KnownContentType,
            Extension: ".pdf",
            OriginalFileName: "Renamed License.pdf",
            SafeFileName: "RenamedLicense.pdf",
            SizeBytes: FileAssetBytes.Length);

        await using var factory = new DownloadFactory
        {
            CallerUserId = OwnerUserId,
            IsAdmin = false,
            ByUserId = ownerApp,
            ByDocumentId = null,
            LinkedFileAssetId = fileAssetId,
            FileAssetViewResult = assetView,
            FileAssetBytes = FileAssetBytes,
        };

        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/provider/documents/{documentId}/download");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(KnownContentType);
        var body = await response.Content.ReadAsByteArrayAsync();
        // Must come from the FileAsset path, not the legacy KnownBytes path.
        body.Should().Equal(FileAssetBytes);
        body.Should().NotEqual(KnownBytes);
    }

    [Fact]
    public async Task Legacy_FileUrl_path_serves_bytes_when_no_link_exists()
    {
        var (ownerApp, documentId) = BuildApplicationWithDocument(OwnerUserId);

        // No LinkedFileAssetId / FileAssetViewResult / FileAssetBytes set:
        // - repo.GetFileAssetIdByDocumentIdAsync returns null,
        // - locator.GetByIdAsync would return NotFound,
        // - storage.OpenReadByStorageKeyAsync would return NotFound,
        // -> handler falls back to OpenReadAsync(doc.FileUrl) which yields KnownBytes.
        await using var factory = new DownloadFactory
        {
            CallerUserId = OwnerUserId,
            IsAdmin = false,
            ByUserId = ownerApp,
            ByDocumentId = null,
        };

        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/provider/documents/{documentId}/download");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsByteArrayAsync();
        body.Should().Equal(KnownBytes);
    }

    [Fact]
    public async Task Response_does_not_leak_StorageKey_FileUrl_or_physical_paths()
    {
        var (ownerApp, documentId) = BuildApplicationWithDocument(OwnerUserId);

        var fileAssetId = Guid.Parse("0d000000-0000-0000-0000-0000000000a2");
        // Deliberately distinctive markers so the assertion proves nothing leaked.
        const string SecretStorageKey = "provider-application-documents/SECRET-2c-asset.pdf";
        const string SecretOriginalFileName = "TOP_SECRET_2C_LICENSE.pdf";
        const string SecretFileUrl = "/uploads/provider-application-documents/SECRET-2C-LEGACY.pdf";

        // Mutate the application's document so FileUrl is the SECRET marker.
        // (BuildApplicationWithDocument already created a doc, we use that path
        // implicitly via the legacy stub; for the FileAsset path the markers are
        // injected through the view + storage stub key arguments.)
        var assetView = new FileAssetView(
            Id: fileAssetId,
            StorageProvider: "Local",
            StorageKey: SecretStorageKey,
            ContentType: KnownContentType,
            Extension: ".pdf",
            OriginalFileName: SecretOriginalFileName,
            SafeFileName: "safe-name.pdf",
            SizeBytes: FileAssetBytes.Length);

        await using var factory = new DownloadFactory
        {
            CallerUserId = OwnerUserId,
            IsAdmin = false,
            ByUserId = ownerApp,
            ByDocumentId = null,
            LinkedFileAssetId = fileAssetId,
            FileAssetViewResult = assetView,
            FileAssetBytes = FileAssetBytes,
        };

        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/provider/documents/{documentId}/download");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Storage internals (key, legacy URL, physical paths) must NEVER appear
        // anywhere in the response. OriginalFileName is intentionally NOT in this
        // list — it is the user-supplied download name and is legitimately
        // surfaced (sanitized) via Content-Disposition. That is the same Patch 1A
        // behavior preserved by Patch 2C.
        var leakable = new[]
        {
            SecretStorageKey,
            SecretFileUrl,
            "/uploads/provider-application-documents/",
        };

        foreach (var header in response.Headers.Concat(response.Content.Headers))
        {
            foreach (var value in header.Value)
            {
                foreach (var needle in leakable)
                {
                    value.Should().NotContain(needle,
                        $"response header '{header.Key}' leaked '{needle}'");
                }
            }
        }

        // The body is the FileAsset bytes themselves (binary PDF marker), so it
        // trivially cannot contain ASCII secrets unless one of our markers is a
        // substring of those bytes — but we still verify defensively.
        var bodyText = await response.Content.ReadAsStringAsync();
        foreach (var needle in leakable)
        {
            bodyText.Should().NotContain(needle, $"response body leaked '{needle}'");
        }

        // Positive contract: the user's OriginalFileName (sanitized) IS expected
        // to surface as the Content-Disposition download name — confirming the
        // FileAsset path correctly drove the response (vs the legacy FileUrl
        // fallback, which would surface KnownFileName instead).
        response.Content.Headers.ContentDisposition.Should().NotBeNull();
        var disposition = response.Content.Headers.ContentDisposition!.ToString();
        disposition.Should().Contain(SecretOriginalFileName,
            "Content-Disposition should reflect the FileAsset OriginalFileName when the FileAsset path served the response");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    // Patch 2C: distinct bytes for the FileAsset path so tests can prove which
    // branch served the response.
    private static readonly byte[] FileAssetBytes = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF];

    private static (ProviderApplication App, Guid DocumentId) BuildApplicationWithDocument(Guid ownerUserId)
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
        var app = register.Value!;

        var add = app.AddDocument(
            DocumentType.BusinessLicense,
            fileUrl: "/uploads/provider-application-documents/00000000-0000-0000-0000-0000000000aa.pdf",
            fileName: KnownFileName,
            fileSizeBytes: KnownBytes.Length);

        add.IsSuccess.Should().BeTrue();
        return (app, add.Value!.Id);
    }

    /// <summary>
    /// Boots the real API host with stubbed repository + storage and a configurable
    /// test auth scheme. Each test instantiates a factory describing the caller
    /// identity and what the repository should return for the two lookup paths.
    /// </summary>
    private sealed class DownloadFactory : WebApplicationFactory<Program>
    {
        // Test-only fake secrets so StartupSecretGuards (Patch 0A) passes without
        // relying on developer-machine user-secrets. Both are >32 bytes.
        private const string FakeJwtKey =
            "yallajo-test-only-fake-jwt-signing-key-do-not-use-in-production-0123456789";
        private const string FakeExternalAuthSigningKey =
            "yallajo-test-only-fake-externalauth-signing-key-do-not-use-in-production-0123456789";

        public Guid CallerUserId { get; init; }
        public bool IsAdmin { get; init; }
        public bool Anonymous { get; init; }

        /// <summary>Application returned by GetWithDocumentsByUserIdAsync(CallerUserId).</summary>
        public ProviderApplication? ByUserId { get; init; }

        /// <summary>Application returned by GetWithDocumentsByDocumentIdAsync(anyId).</summary>
        public ProviderApplication? ByDocumentId { get; init; }

        /// <summary>
        /// Patch 2C: optional FileAsset link. When set (and not Guid.Empty), the
        /// stubbed <see cref="IProviderApplicationRepository.GetFileAssetIdByDocumentIdAsync"/>
        /// returns it; when null (default), no link exists and the handler must fall
        /// back to the legacy <c>FileUrl</c> path.
        /// </summary>
        public Guid? LinkedFileAssetId { get; init; }

        /// <summary>
        /// Patch 2C: optional FileAsset view. When set, the stubbed
        /// <see cref="IFileAssetLocator"/> returns it; otherwise it returns NotFound.
        /// </summary>
        public FileAssetView? FileAssetViewResult { get; init; }

        /// <summary>
        /// Patch 2C: bytes the FileAsset path streams via OpenReadByStorageKeyAsync.
        /// </summary>
        public byte[]? FileAssetBytes { get; init; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Seeding:Enabled", "false");
            builder.UseSetting("Jwt:Key", FakeJwtKey);
            builder.UseSetting("ExternalAuth:SigningKey", FakeExternalAuthSigningKey);

            builder.ConfigureTestServices(services =>
            {
                if (!Anonymous)
                {
                    TestAuthState.CallerUserId = CallerUserId;
                    TestAuthState.IsAdmin = IsAdmin;

                    services.AddAuthentication(TestAuthHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            TestAuthHandler.SchemeName, _ => { });

                    services.PostConfigureAll<AuthenticationOptions>(o =>
                    {
                        o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                        o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                        o.DefaultScheme = TestAuthHandler.SchemeName;
                        o.DefaultForbidScheme = TestAuthHandler.SchemeName;
                    });
                }

                // Stub the repository so no database is required.
                services.RemoveAll<IProviderApplicationRepository>();
                services.AddScoped<IProviderApplicationRepository>(_ =>
                {
                    var repo = Substitute.For<IProviderApplicationRepository>();
                    repo.GetWithDocumentsByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                        .Returns(_ => Task.FromResult(ByUserId));
                    repo.GetWithDocumentsByDocumentIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                        .Returns(_ => Task.FromResult(ByDocumentId));
                    // Patch 2C: by default no link exists -> handler uses legacy path.
                    repo.GetFileAssetIdByDocumentIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                        .Returns(_ => Task.FromResult(LinkedFileAssetId));
                    return repo;
                });

                // Patch 2C: stub the FileAsset locator so no ContentCoreDbContext is
                // required. Default: NotFound -> handler falls back to legacy path.
                services.RemoveAll<IFileAssetLocator>();
                services.AddSingleton<IFileAssetLocator>(_ =>
                {
                    var locator = Substitute.For<IFileAssetLocator>();
                    locator.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                           .Returns(_ => FileAssetViewResult is null
                               ? Task.FromResult(Result<FileAssetView>.Failure(
                                     Error.NotFound("FileAsset"), Outcome.NotFound))
                               : Task.FromResult(Result<FileAssetView>.Success(FileAssetViewResult)));
                    return locator;
                });

                // Stub storage so OpenReadAsync streams the known bytes without disk I/O.
                services.RemoveAll<IFileStorageService>();
                services.AddSingleton<IFileStorageService>(_ => new StubFileStorage(FileAssetBytes));
            });
        }
    }

    /// <summary>Ambient state read by the test auth handler (single-test scoped).</summary>
    private static class TestAuthState
    {
        public static Guid CallerUserId;
        public static bool IsAdmin;
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "AccountsProviderDownloadTestScheme";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, TestAuthState.CallerUserId.ToString()),
                new("sub", TestAuthState.CallerUserId.ToString()),
                // Read-tier permission for provider application data (reused for download).
                new("Permission",
                    PermissionPolicyNames.Build(AccountsFeatures.ProviderApplication, AppAction.Read)),
            };

            if (TestAuthState.IsAdmin)
            {
                // CurrentUser.Roles reads "role" ∪ ClaimTypes.Role; AppRoles.Admin → level 60.
                claims.Add(new Claim("role", AppRoles.Admin));
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class StubFileStorage : IFileStorageService
    {
        private readonly byte[]? _fileAssetBytes;

        public StubFileStorage(byte[]? fileAssetBytes = null)
        {
            _fileAssetBytes = fileAssetBytes;
        }

        public Task<Result<FileUploadResult>> UploadAsync(
            Stream stream, string fileName, string contentType, string folder, CancellationToken ct = default)
            => Task.FromResult(Result<FileUploadResult>.Success(
                new FileUploadResult($"/uploads/{folder}/x.bin", $"{folder}/x.bin", 0)));

        public Task<bool> DeleteAsync(string fileUrl, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<string> GetAccessUrlAsync(string fileUrl, TimeSpan? expiry = null, CancellationToken ct = default)
            => Task.FromResult(fileUrl);

        public Task<Result<FileDownload>> OpenReadAsync(string fileUrl, CancellationToken ct = default)
            => Task.FromResult(Result<FileDownload>.Success(
                new FileDownload(new MemoryStream(KnownBytes, writable: false), KnownContentType, KnownBytes.Length)));

        // Patch 2C: when the factory supplies FileAssetBytes, this stub honors the
        // FileAsset-path read by streaming them back; otherwise it returns NotFound
        // so the handler is forced to fall back to OpenReadAsync(fileUrl).
        public Task<Result<FileDownload>> OpenReadByStorageKeyAsync(string storageKey, CancellationToken ct = default)
            => _fileAssetBytes is null
                ? Task.FromResult(Result<FileDownload>.Failure(Error.NotFound("File"), Outcome.NotFound))
                : Task.FromResult(Result<FileDownload>.Success(
                    new FileDownload(
                        new MemoryStream(_fileAssetBytes, writable: false),
                        KnownContentType,
                        _fileAssetBytes.Length)));
    }
}

internal static class ServiceCollectionRemoveAllExtensions
{
    public static void RemoveAll<TService>(this IServiceCollection services)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == typeof(TService))
            {
                services.RemoveAt(i);
            }
        }
    }
}
