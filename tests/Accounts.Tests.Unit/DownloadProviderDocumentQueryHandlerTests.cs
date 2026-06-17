using Accounts.Application.Queries.DownloadProviderDocument;
using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using ContentCore.Contracts.Storage;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Tests.Unit;

/// <summary>
/// Patch 2C handler-level tests covering: FileAsset path is preferred when a
/// ProviderDocumentFile link exists; graceful fall-through to the legacy
/// FileUrl path when no link / locator NotFound / OpenReadByStorageKeyAsync
/// fails; both paths fail -> NotFound (anti-enumeration); preserved Patch 1A
/// authorization (cross-provider 404, anonymous 401, admin allowed); and the
/// "no PII/StorageKey/FileUrl at Information level or above" log contract.
/// </summary>
public sealed class DownloadProviderDocumentQueryHandlerTests
{
    // ── (a) FileAsset path is preferred when a link exists ────────────────────
    [Fact]
    public async Task FileAsset_path_is_preferred_when_link_exists()
    {
        var ownerUserId = Guid.CreateVersion7();
        var doc = BuildDoc(ownerUserId, fileUrl: "/uploads/provider-application-documents/legacy.pdf");
        var (handler, repo, locator, storage, currentUser, _) = BuildHandler(ownerUserId, isAdmin: false);

        StubOwnerLookup(repo, ownerUserId, doc);

        var fileAssetId = Guid.CreateVersion7();
        repo.GetFileAssetIdByDocumentIdAsync(doc.Id, Arg.Any<CancellationToken>())
            .Returns(fileAssetId);

        var assetView = new FileAssetView(
            Id: fileAssetId,
            StorageProvider: "Local",
            StorageKey: "provider-application-documents/abc.pdf",
            ContentType: "application/pdf",
            Extension: ".pdf",
            OriginalFileName: "Original License.pdf",
            SafeFileName: "OriginalLicense.pdf",
            SizeBytes: 4242);
        locator.GetByIdAsync(fileAssetId, Arg.Any<CancellationToken>())
               .Returns(Result<FileAssetView>.Success(assetView));

        var assetStream = new MemoryStream(new byte[] { 1, 2, 3 });
        storage.OpenReadByStorageKeyAsync(assetView.StorageKey, Arg.Any<CancellationToken>())
               .Returns(Result<FileDownload>.Success(new FileDownload(assetStream, "application/pdf", 4242)));

        var result = await handler.Handle(
            new DownloadProviderDocumentQuery(doc.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().BeSameAs(assetStream);
        result.Value.ContentType.Should().Be("application/pdf");
        result.Value.FileSize.Should().Be(4242);

        // Legacy path must NOT have been touched.
        await storage.DidNotReceiveWithAnyArgs().OpenReadAsync(default!, default);
    }

    // ── (b) No link row -> falls back to legacy FileUrl path ──────────────────
    [Fact]
    public async Task Falls_back_to_legacy_path_when_no_link_exists()
    {
        var ownerUserId = Guid.CreateVersion7();
        var doc = BuildDoc(ownerUserId, fileUrl: "/uploads/provider-application-documents/legacy.pdf");
        var (handler, repo, locator, storage, _, _) = BuildHandler(ownerUserId, isAdmin: false);

        StubOwnerLookup(repo, ownerUserId, doc);
        repo.GetFileAssetIdByDocumentIdAsync(doc.Id, Arg.Any<CancellationToken>()).ReturnsNull();

        var legacyStream = new MemoryStream(new byte[] { 9, 9 });
        storage.OpenReadAsync(doc.FileUrl, Arg.Any<CancellationToken>())
               .Returns(Result<FileDownload>.Success(new FileDownload(legacyStream, "application/pdf", 2)));

        var result = await handler.Handle(
            new DownloadProviderDocumentQuery(doc.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().BeSameAs(legacyStream);

        await locator.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await storage.DidNotReceiveWithAnyArgs().OpenReadByStorageKeyAsync(default!, default);
    }

    // ── (c) Locator NotFound -> falls back to legacy FileUrl path ─────────────
    [Fact]
    public async Task Falls_back_to_legacy_path_when_locator_returns_not_found()
    {
        var ownerUserId = Guid.CreateVersion7();
        var doc = BuildDoc(ownerUserId, fileUrl: "/uploads/provider-application-documents/legacy.pdf");
        var (handler, repo, locator, storage, _, _) = BuildHandler(ownerUserId, isAdmin: false);

        StubOwnerLookup(repo, ownerUserId, doc);

        var fileAssetId = Guid.CreateVersion7();
        repo.GetFileAssetIdByDocumentIdAsync(doc.Id, Arg.Any<CancellationToken>()).Returns(fileAssetId);

        locator.GetByIdAsync(fileAssetId, Arg.Any<CancellationToken>())
               .Returns(Result<FileAssetView>.Failure(Error.NotFound("FileAsset"), Outcome.NotFound));

        var legacyStream = new MemoryStream(new byte[] { 7 });
        storage.OpenReadAsync(doc.FileUrl, Arg.Any<CancellationToken>())
               .Returns(Result<FileDownload>.Success(new FileDownload(legacyStream, "application/pdf", 1)));

        var result = await handler.Handle(
            new DownloadProviderDocumentQuery(doc.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().BeSameAs(legacyStream);
        await storage.DidNotReceiveWithAnyArgs().OpenReadByStorageKeyAsync(default!, default);
    }

    // ── (d) OpenReadByStorageKeyAsync fails -> falls back to legacy path ─────
    [Fact]
    public async Task Falls_back_to_legacy_path_when_storage_by_key_fails()
    {
        var ownerUserId = Guid.CreateVersion7();
        var doc = BuildDoc(ownerUserId, fileUrl: "/uploads/provider-application-documents/legacy.pdf");
        var (handler, repo, locator, storage, _, _) = BuildHandler(ownerUserId, isAdmin: false);

        StubOwnerLookup(repo, ownerUserId, doc);

        var fileAssetId = Guid.CreateVersion7();
        repo.GetFileAssetIdByDocumentIdAsync(doc.Id, Arg.Any<CancellationToken>()).Returns(fileAssetId);

        var assetView = new FileAssetView(
            fileAssetId, "Local", "provider-application-documents/missing.pdf",
            "application/pdf", ".pdf", "x.pdf", "x.pdf", 10);
        locator.GetByIdAsync(fileAssetId, Arg.Any<CancellationToken>())
               .Returns(Result<FileAssetView>.Success(assetView));

        storage.OpenReadByStorageKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Result<FileDownload>.Failure(Error.NotFound("File"), Outcome.NotFound));

        var legacyStream = new MemoryStream(new byte[] { 4 });
        storage.OpenReadAsync(doc.FileUrl, Arg.Any<CancellationToken>())
               .Returns(Result<FileDownload>.Success(new FileDownload(legacyStream, "application/pdf", 1)));

        var result = await handler.Handle(
            new DownloadProviderDocumentQuery(doc.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().BeSameAs(legacyStream);
    }

    // ── (e) Both paths fail -> NotFound ───────────────────────────────────────
    [Fact]
    public async Task Returns_NotFound_when_both_paths_fail()
    {
        var ownerUserId = Guid.CreateVersion7();
        var doc = BuildDoc(ownerUserId, fileUrl: "/uploads/provider-application-documents/missing.pdf");
        var (handler, repo, locator, storage, _, _) = BuildHandler(ownerUserId, isAdmin: false);

        StubOwnerLookup(repo, ownerUserId, doc);
        repo.GetFileAssetIdByDocumentIdAsync(doc.Id, Arg.Any<CancellationToken>()).ReturnsNull();

        storage.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Result<FileDownload>.Failure(Error.NotFound("File"), Outcome.NotFound));

        var result = await handler.Handle(
            new DownloadProviderDocumentQuery(doc.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    // ── (f) Cross-provider request -> 404 even if a link would resolve ───────
    [Fact]
    public async Task Cross_provider_request_returns_NotFound_and_never_touches_storage()
    {
        var callerUserId = Guid.CreateVersion7();
        // Caller has NO application/documents at all (cross-provider scenario).
        var (handler, repo, locator, storage, _, _) = BuildHandler(callerUserId, isAdmin: false);

        // Owner lookup returns null -> document==null -> NotFound short-circuit.
        repo.GetWithDocumentsByUserIdAsync(callerUserId, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await handler.Handle(
            new DownloadProviderDocumentQuery(Guid.CreateVersion7()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);

        await locator.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await storage.DidNotReceiveWithAnyArgs().OpenReadAsync(default!, default);
        await storage.DidNotReceiveWithAnyArgs().OpenReadByStorageKeyAsync(default!, default);
        await repo.DidNotReceiveWithAnyArgs().GetFileAssetIdByDocumentIdAsync(default, default);
    }

    // ── (g) Anonymous caller -> 401 without touching any I/O ─────────────────
    [Fact]
    public async Task Anonymous_caller_returns_Unauthorized_without_touching_io()
    {
        var (handler, repo, locator, storage, currentUser, _) = BuildHandler(Guid.Empty, isAdmin: false);
        currentUser.IsAuthenticated.Returns(false);
        currentUser.UserId.Returns((Guid?)null);

        var result = await handler.Handle(
            new DownloadProviderDocumentQuery(Guid.CreateVersion7()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);

        await repo.DidNotReceiveWithAnyArgs().GetWithDocumentsByUserIdAsync(default, default);
        await repo.DidNotReceiveWithAnyArgs().GetFileAssetIdByDocumentIdAsync(default, default);
        await locator.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await storage.DidNotReceiveWithAnyArgs().OpenReadAsync(default!, default);
        await storage.DidNotReceiveWithAnyArgs().OpenReadByStorageKeyAsync(default!, default);
    }

    // ── (h) Admin-tier caller -> FileAsset path works for foreign documents ─
    [Fact]
    public async Task Admin_tier_caller_uses_FileAsset_path_for_foreign_document()
    {
        var ownerUserId = Guid.CreateVersion7();
        var adminUserId = Guid.CreateVersion7();
        var doc = BuildDoc(ownerUserId, fileUrl: "/uploads/provider-application-documents/legacy.pdf");
        var (handler, repo, locator, storage, currentUser, _) = BuildHandler(adminUserId, isAdmin: true);

        // Admin's own application lookup returns null (admin is not the owner).
        repo.GetWithDocumentsByUserIdAsync(adminUserId, Arg.Any<CancellationToken>()).ReturnsNull();
        // Admin-tier fallback: lookup-by-document-id resolves to the owner's app.
        var ownerApp = (ProviderApplication)typeof(ProviderDocument)
            .GetProperty(nameof(ProviderDocument.Application))!.GetValue(doc)!;
        repo.GetWithDocumentsByDocumentIdAsync(doc.Id, Arg.Any<CancellationToken>())
            .Returns(ownerApp);

        var fileAssetId = Guid.CreateVersion7();
        repo.GetFileAssetIdByDocumentIdAsync(doc.Id, Arg.Any<CancellationToken>()).Returns(fileAssetId);

        var assetView = new FileAssetView(
            fileAssetId, "Local", "provider-application-documents/admin.pdf",
            "application/pdf", ".pdf", "AdminViewable.pdf", "AdminViewable.pdf", 16);
        locator.GetByIdAsync(fileAssetId, Arg.Any<CancellationToken>())
               .Returns(Result<FileAssetView>.Success(assetView));

        var assetStream = new MemoryStream(new byte[] { 0xCC });
        storage.OpenReadByStorageKeyAsync(assetView.StorageKey, Arg.Any<CancellationToken>())
               .Returns(Result<FileDownload>.Success(new FileDownload(assetStream, "application/pdf", 16)));

        var result = await handler.Handle(
            new DownloadProviderDocumentQuery(doc.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().BeSameAs(assetStream);
        result.Value.FileSize.Should().Be(16);
    }

    // ── (i) PII-no-leak contract via captured ILogger ─────────────────────────
    [Fact]
    public async Task No_PII_appears_in_Information_or_higher_log_messages()
    {
        var secretFileName = "TOP_SECRET_LICENSE.pdf";
        var secretFileUrl = "/uploads/provider-application-documents/SECRET-ABC.pdf";
        var secretStorageKey = "provider-application-documents/SECRET-ABC.pdf";

        var ownerUserId = Guid.CreateVersion7();
        var doc = BuildDoc(ownerUserId, fileUrl: secretFileUrl, fileName: secretFileName);
        var captured = new List<CapturedLog>();
        var (handler, repo, locator, storage, _, _) = BuildHandler(ownerUserId, isAdmin: false, sharedLogs: captured);

        StubOwnerLookup(repo, ownerUserId, doc);

        var fileAssetId = Guid.CreateVersion7();
        repo.GetFileAssetIdByDocumentIdAsync(doc.Id, Arg.Any<CancellationToken>()).Returns(fileAssetId);

        var assetView = new FileAssetView(
            fileAssetId, "Local", secretStorageKey, "application/pdf",
            ".pdf", secretFileName, "safe-name.pdf", 99);
        locator.GetByIdAsync(fileAssetId, Arg.Any<CancellationToken>())
               .Returns(Result<FileAssetView>.Success(assetView));
        storage.OpenReadByStorageKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Result<FileDownload>.Success(new FileDownload(new MemoryStream(), "application/pdf", 99)));

        var result = await handler.Handle(
            new DownloadProviderDocumentQuery(doc.Id), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        var leakable = new[]
        {
            secretFileName,
            secretFileUrl,
            secretStorageKey,
            "/uploads/provider-application-documents/"
        };

        foreach (var rec in captured.Where(c => c.Level >= LogLevel.Information))
        {
            foreach (var needle in leakable)
            {
                rec.Rendered.Should().NotContain(needle,
                    $"PII leak at level {rec.Level}: '{rec.Rendered}'");
            }
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static (DownloadProviderDocumentQueryHandler handler,
                    IProviderApplicationRepository repo,
                    IFileAssetLocator locator,
                    IFileStorageService storage,
                    ICurrentUser currentUser,
                    List<CapturedLog> logs)
        BuildHandler(Guid callerUserId, bool isAdmin, List<CapturedLog>? sharedLogs = null)
    {
        var repo = Substitute.For<IProviderApplicationRepository>();
        var locator = Substitute.For<IFileAssetLocator>();
        var storage = Substitute.For<IFileStorageService>();
        var currentUser = Substitute.For<ICurrentUser>();

        currentUser.IsAuthenticated.Returns(callerUserId != Guid.Empty);
        currentUser.UserId.Returns(callerUserId == Guid.Empty ? (Guid?)null : callerUserId);
        currentUser.Roles.Returns(isAdmin ? new[] { AppRoles.Admin } : Array.Empty<string>());

        var captured = sharedLogs ?? new List<CapturedLog>();
        var logger = new ListLogger<DownloadProviderDocumentQueryHandler>(captured);

        var handler = new DownloadProviderDocumentQueryHandler(repo, storage, locator, currentUser, logger);
        return (handler, repo, locator, storage, currentUser, captured);
    }

    private static void StubOwnerLookup(
        IProviderApplicationRepository repo, Guid ownerUserId, ProviderDocument doc)
    {
        var application = (ProviderApplication)typeof(ProviderDocument)
            .GetProperty(nameof(ProviderDocument.Application))!.GetValue(doc)!;
        repo.GetWithDocumentsByUserIdAsync(ownerUserId, Arg.Any<CancellationToken>())
            .Returns(application);
    }

    private static ProviderDocument BuildDoc(
        Guid ownerUserId,
        string fileUrl,
        string fileName = "license.pdf")
    {
        var application = ProviderApplication.Register(
            userId:              ownerUserId,
            type:                ProviderType.TourOperator,
            businessName:        "Acme Tours",
            contactEmail:        "contact@acme.com",
            contactPhone:        "+1234567890",
            address:             "123 Main St, Cairo",
            description:         "Premium tour operator in Egypt",
            typeSpecificDataJson: null).Value!;

        var addResult = application.AddDocument(
            documentType: DocumentType.BusinessLicense,
            fileUrl:       fileUrl,
            fileName:      fileName,
            fileSizeBytes: 1024);
        var doc = addResult.Value!;

        // Wire the back-navigation EF would normally populate.
        var navProperty = typeof(ProviderDocument).GetProperty(nameof(ProviderDocument.Application));
        navProperty!.SetValue(doc, application);
        return doc;
    }

    private sealed record CapturedLog(LogLevel Level, string Rendered);

    private sealed class ListLogger<T>(List<CapturedLog> sink) : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            sink.Add(new CapturedLog(logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
