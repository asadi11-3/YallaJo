using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Accounts.Application.Interfaces;
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
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace YallaJo.Accounts.IntegrationTests;

/// <summary>
/// Patch 2D write-path tests: verify that uploading or replacing a provider document
/// also materializes a FileAsset (via <see cref="IFileAssetRegistrar"/>) and a
/// ProviderDocumentFile link (via <see cref="IProviderDocumentFileWriter"/>), while the
/// legacy ProviderDocument.FileUrl write and the public response shape stay unchanged.
/// A failure in the FileAsset/link materialization must NOT fail the upload/replace
/// (best-effort; the Patch 2C download fallback still serves via FileUrl).
/// </summary>
public sealed class ProviderDocumentFileAssetWriteTests
{
    private static readonly Guid OwnerUserId = Guid.Parse("0a000000-0000-0000-0000-0000000000d1");
    private static readonly Guid KnownFileAssetId = Guid.Parse("fa000000-0000-0000-0000-0000000000f1");

    private static byte[] ValidPdf() =>
        [.. "%PDF-1.4\n%\u00E2\u00E3\u00CF\u00D3\n1 0 obj\n<<>>\nendobj\n"u8.ToArray()];

    [Fact]
    public async Task Upload_materializes_FileAsset_and_link_and_returns_created()
    {
        var ownerApp = BuildOwnerApplication(OwnerUserId);

        await using var factory = new WriteFactory
        {
            CallerUserId = OwnerUserId,
            RequiredAction = AppAction.Create,
            OwnerApplication = ownerApp,
        };
        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/api/v1/provider/documents/upload",
            BuildUploadContent(ValidPdf(), "doc.pdf", "application/pdf"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await factory.Registrar.Received(1).GetOrAddByStorageKeyAsync(
            Arg.Any<FileAssetSeed>(), false, Arg.Any<CancellationToken>());

        // The new document's Id is generated inside the aggregate, so we match any non-empty Guid.
        await factory.Writer.Received(1).UpsertLinkAsync(
            Arg.Is<Guid>(id => id != Guid.Empty),
            KnownFileAssetId,
            DocumentType.BusinessLicense,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_still_succeeds_when_FileAsset_materialization_fails()
    {
        var ownerApp = BuildOwnerApplication(OwnerUserId);

        await using var factory = new WriteFactory
        {
            CallerUserId = OwnerUserId,
            RequiredAction = AppAction.Create,
            OwnerApplication = ownerApp,
            WriterThrows = true,
        };
        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/api/v1/provider/documents/upload",
            BuildUploadContent(ValidPdf(), "doc.pdf", "application/pdf"));

        // Best-effort materialization: even though the link writer throws, the upload
        // succeeds (legacy FileUrl is still written and serves via the Patch 2C fallback).
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await factory.Writer.Received(1).UpsertLinkAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DocumentType>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replace_updates_existing_link_in_place()
    {
        var ownerApp = BuildOwnerApplication(OwnerUserId);
        var addResult = ownerApp.AddDocument(
            DocumentType.BusinessLicense,
            "/uploads/provider-application-documents/old.pdf",
            "old.pdf",
            1024);
        addResult.IsSuccess.Should().BeTrue();
        var existingDoc = addResult.Value!;
        var documentId = existingDoc.Id;

        // Wire the back-navigation so the handler can resolve the original DocumentType.
        var navProperty = typeof(ProviderDocument).GetProperty(nameof(ProviderDocument.Application));
        navProperty!.SetValue(existingDoc, ownerApp);

        await using var factory = new WriteFactory
        {
            CallerUserId = OwnerUserId,
            RequiredAction = AppAction.Update,
            OwnerApplication = ownerApp,
        };
        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/v1/provider/documents/{documentId}/replace-upload",
            BuildReplaceContent(ValidPdf(), "doc.pdf", "application/pdf"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Same DocumentId is repointed to the new FileAsset (update in place, not a 2nd row).
        await factory.Writer.Received(1).UpsertLinkAsync(
            documentId,
            KnownFileAssetId,
            DocumentType.BusinessLicense,
            Arg.Any<CancellationToken>());
    }

    private static MultipartFormDataContent BuildUploadContent(byte[] bytes, string fileName, string contentType)
    {
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent
        {
            { fileContent, "file", fileName },
            { new StringContent(nameof(DocumentType.BusinessLicense)), "documentType" },
        };
    }

    private static MultipartFormDataContent BuildReplaceContent(byte[] bytes, string fileName, string contentType)
    {
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent
        {
            { fileContent, "file", fileName },
        };
    }

    private static ProviderApplication BuildOwnerApplication(Guid ownerUserId)
    {
        var register = ProviderApplication.Register(
            ownerUserId,
            ProviderType.TourOperator,
            "Acme Tours",
            "owner@example.com",
            "+97400000000",
            "Doha",
            "Tours");
        register.IsSuccess.Should().BeTrue();
        return register.Value!;
    }

    private sealed class WriteFactory : WebApplicationFactory<Program>
    {
        private const string FakeJwtKey =
            "yallajo-test-only-fake-jwt-signing-key-do-not-use-in-production-0123456789";
        private const string FakeExternalAuthSigningKey =
            "yallajo-test-only-fake-externalauth-signing-key-do-not-use-in-production-0123456789";

        public Guid CallerUserId { get; init; }

        public string RequiredAction { get; init; } = AppAction.Create;

        public ProviderApplication? OwnerApplication { get; init; }

        public bool WriterThrows { get; init; }

        public IFileAssetRegistrar Registrar { get; } = Substitute.For<IFileAssetRegistrar>();

        public IProviderDocumentFileWriter Writer { get; } = Substitute.For<IProviderDocumentFileWriter>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Seeding:Enabled", "false");
            builder.UseSetting("Jwt:Key", FakeJwtKey);
            builder.UseSetting("ExternalAuth:SigningKey", FakeExternalAuthSigningKey);

            Registrar
                .GetOrAddByStorageKeyAsync(Arg.Any<FileAssetSeed>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromResult(
                    Result<FileAssetRecord>.Success(new FileAssetRecord(KnownFileAssetId, WasReused: false))));

            if (WriterThrows)
            {
                Writer
                    .UpsertLinkAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DocumentType>(), Arg.Any<CancellationToken>())
                    .Returns<Task>(_ => throw new InvalidOperationException("simulated link-write failure"));
            }
            else
            {
                Writer
                    .UpsertLinkAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DocumentType>(), Arg.Any<CancellationToken>())
                    .Returns(Task.CompletedTask);
            }

            builder.ConfigureTestServices(services =>
            {
                WriteAuthState.CallerUserId = CallerUserId;
                WriteAuthState.RequiredAction = RequiredAction;

                services.AddAuthentication(WriteTestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, WriteTestAuthHandler>(
                        WriteTestAuthHandler.SchemeName, _ => { });
                services.PostConfigureAll<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = WriteTestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = WriteTestAuthHandler.SchemeName;
                    options.DefaultScheme = WriteTestAuthHandler.SchemeName;
                    options.DefaultForbidScheme = WriteTestAuthHandler.SchemeName;
                });

                services.RemoveAll<IProviderApplicationRepository>();
                var repo = Substitute.For<IProviderApplicationRepository>();
                repo.GetWithDocumentsByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                    .Returns(_ => Task.FromResult(OwnerApplication));
                services.AddScoped(_ => repo);

                services.RemoveAll<IFileStorageService>();
                services.AddSingleton<IFileStorageService>(new StubFileStorage());

                services.RemoveAll<IAccountsUnitOfWork>();
                var uow = Substitute.For<IAccountsUnitOfWork>();
                uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
                services.AddScoped(_ => uow);

                services.RemoveAll<IFileAssetRegistrar>();
                services.AddSingleton(Registrar);

                services.RemoveAll<IProviderDocumentFileWriter>();
                services.AddSingleton(Writer);
            });
        }
    }

    private static class WriteAuthState
    {
        public static Guid CallerUserId;
        public static string RequiredAction = AppAction.Create;
    }

    private sealed class WriteTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "AccountsProviderWriteTestScheme";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, WriteAuthState.CallerUserId.ToString()),
                new Claim("sub", WriteAuthState.CallerUserId.ToString()),
                new Claim(
                    "Permission",
                    PermissionPolicyNames.Build(AccountsFeatures.ProviderApplication, WriteAuthState.RequiredAction)),
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
            Stream stream, string fileName, string folder, string contentType, CancellationToken ct = default)
        {
            var length = stream.CanSeek ? stream.Length : 0;
            return Task.FromResult(Result<FileUploadResult>.Success(
                new FileUploadResult($"/uploads/{folder}/x.bin", $"{folder}/x.bin", length)));
        }

        public Task<bool> DeleteAsync(string fileUrl, CancellationToken ct = default) => Task.FromResult(true);

        public Task<string> GetAccessUrlAsync(string fileUrl, TimeSpan? expiresIn = null, CancellationToken ct = default)
            => Task.FromResult(fileUrl);

        public Task<Result<FileDownload>> OpenReadAsync(string fileUrl, CancellationToken ct = default)
            => Task.FromResult(Result<FileDownload>.Success(
                new FileDownload(new MemoryStream([0x25, 0x50, 0x44, 0x46], writable: false), "application/pdf", 4)));

        public Task<Result<FileDownload>> OpenReadByStorageKeyAsync(string storageKey, CancellationToken ct = default)
            => Task.FromResult(Result<FileDownload>.Failure(Error.NotFound("File"), Outcome.NotFound));
    }
}
