using Accounts.Application.Queries.GetAdminProviderApplicationById;
using Accounts.Application.Queries.GetMyApplicationStatus;
using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using ContentCore.Contracts.Storage;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Tests.Unit;

/// <summary>
/// Patch 2E read-model tests for the two provider-document list/index handlers
/// (admin application details + my-application status). They verify that each
/// handler prefers FileAsset V2 metadata when a ProviderDocumentFile link exists,
/// falls back to legacy ProviderDocument fields when no link exists, never leaks
/// the internal StorageKey, preserves authorization, and batches the cross-module
/// reads (no N+1).
/// </summary>
public sealed class ProviderDocumentListReadModelTests
{
    private const string LegacyFileName = "legacy.pdf";
    private const long LegacyFileSizeBytes = 1000;
    private const string LegacyFileUrl = "/uploads/provider-application-documents/legacy.pdf";

    private const string FileAssetOriginalName = "RealOriginal.pdf";
    private const long FileAssetSizeBytes = 4242;
    private const string FileAssetStorageKey = "provider-application-documents/secret.pdf";

    // ── Admin details: FileAsset metadata is preferred when a link exists ──────
    [Fact]
    public async Task Admin_details_prefer_FileAsset_metadata_when_link_exists()
    {
        var ownerUserId = Guid.CreateVersion7();
        var (app, doc) = BuildApplicationWithDocument(ownerUserId);
        var repo = Substitute.For<IProviderApplicationRepository>();
        var locator = Substitute.For<IFileAssetLocator>();

        repo.GetWithDocumentsAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);
        var fileAssetId = StubLinkedFileAsset(repo, locator, doc.Id);

        var handler = new GetAdminProviderApplicationByIdQueryHandler(
            repo, locator, NullLogger<GetAdminProviderApplicationByIdQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetAdminProviderApplicationByIdQuery(app.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var projected = result.Value!.Documents.Single();
        projected.FileName.Should().Be(FileAssetOriginalName);
        projected.FileSizeBytes.Should().Be(FileAssetSizeBytes);
        projected.FileUrl.Should().Be(LegacyFileUrl);

        // Batched once -> no N+1.
        await repo.Received(1).GetFileAssetIdsByDocumentIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
        await locator.Received(1).GetByIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
        _ = fileAssetId;
    }

    // ── Admin details: legacy fallback when no link exists ────────────────────
    [Fact]
    public async Task Admin_details_fall_back_to_legacy_metadata_when_no_link_exists()
    {
        var ownerUserId = Guid.CreateVersion7();
        var (app, _) = BuildApplicationWithDocument(ownerUserId);
        var repo = Substitute.For<IProviderApplicationRepository>();
        var locator = Substitute.For<IFileAssetLocator>();

        repo.GetWithDocumentsAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);
        StubNoLink(repo, locator);

        var handler = new GetAdminProviderApplicationByIdQueryHandler(
            repo, locator, NullLogger<GetAdminProviderApplicationByIdQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetAdminProviderApplicationByIdQuery(app.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var projected = result.Value!.Documents.Single();
        projected.FileName.Should().Be(LegacyFileName);
        projected.FileSizeBytes.Should().Be(LegacyFileSizeBytes);
        projected.FileUrl.Should().Be(LegacyFileUrl);
    }

    // ── Status: FileAsset metadata is preferred when a link exists ────────────
    [Fact]
    public async Task Status_prefers_FileAsset_metadata_when_link_exists()
    {
        var ownerUserId = Guid.CreateVersion7();
        var (app, doc) = BuildApplicationWithDocument(ownerUserId);
        var repo = Substitute.For<IProviderApplicationRepository>();
        var locator = Substitute.For<IFileAssetLocator>();
        var currentUser = BuildCurrentUser(ownerUserId);

        repo.GetWithDocumentsByUserIdAsync(ownerUserId, Arg.Any<CancellationToken>()).Returns(app);
        StubLinkedFileAsset(repo, locator, doc.Id);

        var handler = new GetMyApplicationStatusQueryHandler(
            repo, locator, currentUser, NullLogger<GetMyApplicationStatusQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetMyApplicationStatusQuery(ownerUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var projected = result.Value!.Documents.Single();
        projected.FileName.Should().Be(FileAssetOriginalName);
        projected.FileUrl.Should().Be(LegacyFileUrl);

        await repo.Received(1).GetFileAssetIdsByDocumentIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
        await locator.Received(1).GetByIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
    }

    // ── Status: legacy fallback when no link exists ───────────────────────────
    [Fact]
    public async Task Status_falls_back_to_legacy_metadata_when_no_link_exists()
    {
        var ownerUserId = Guid.CreateVersion7();
        var (app, _) = BuildApplicationWithDocument(ownerUserId);
        var repo = Substitute.For<IProviderApplicationRepository>();
        var locator = Substitute.For<IFileAssetLocator>();
        var currentUser = BuildCurrentUser(ownerUserId);

        repo.GetWithDocumentsByUserIdAsync(ownerUserId, Arg.Any<CancellationToken>()).Returns(app);
        StubNoLink(repo, locator);

        var handler = new GetMyApplicationStatusQueryHandler(
            repo, locator, currentUser, NullLogger<GetMyApplicationStatusQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetMyApplicationStatusQuery(ownerUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Documents.Single().FileName.Should().Be(LegacyFileName);
    }

    // ── No StorageKey/physical path leak in projected DTOs ────────────────────
    [Fact]
    public async Task Admin_details_never_expose_StorageKey()
    {
        var ownerUserId = Guid.CreateVersion7();
        var (app, doc) = BuildApplicationWithDocument(ownerUserId);
        var repo = Substitute.For<IProviderApplicationRepository>();
        var locator = Substitute.For<IFileAssetLocator>();

        repo.GetWithDocumentsAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);
        StubLinkedFileAsset(repo, locator, doc.Id);

        var handler = new GetAdminProviderApplicationByIdQueryHandler(
            repo, locator, NullLogger<GetAdminProviderApplicationByIdQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetAdminProviderApplicationByIdQuery(app.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var d = result.Value!.Documents.Single();
        d.FileUrl.Should().NotContain(FileAssetStorageKey);
        d.FileName.Should().NotBe(FileAssetStorageKey);
        // FileUrl remains the public /uploads web URL, never the storage key.
        d.FileUrl.Should().Be(LegacyFileUrl);
    }

    // ── Authorization preserved: anonymous -> Unauthorized, no I/O ────────────
    [Fact]
    public async Task Status_anonymous_returns_Unauthorized_without_touching_io()
    {
        var repo = Substitute.For<IProviderApplicationRepository>();
        var locator = Substitute.For<IFileAssetLocator>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(false);
        currentUser.UserId.Returns((Guid?)null);

        var handler = new GetMyApplicationStatusQueryHandler(
            repo, locator, currentUser, NullLogger<GetMyApplicationStatusQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetMyApplicationStatusQuery(Guid.CreateVersion7()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);

        await repo.DidNotReceiveWithAnyArgs().GetWithDocumentsByUserIdAsync(default, default);
        await repo.DidNotReceiveWithAnyArgs().GetFileAssetIdsByDocumentIdsAsync(default!, default);
        await locator.DidNotReceiveWithAnyArgs().GetByIdsAsync(default!, default);
    }

    // ── Authorization preserved: foreign user -> Forbidden, no I/O ────────────
    [Fact]
    public async Task Status_foreign_user_returns_Forbidden_without_touching_io()
    {
        var callerUserId = Guid.CreateVersion7();
        var requestedUserId = Guid.CreateVersion7();
        var repo = Substitute.For<IProviderApplicationRepository>();
        var locator = Substitute.For<IFileAssetLocator>();
        var currentUser = BuildCurrentUser(callerUserId);

        var handler = new GetMyApplicationStatusQueryHandler(
            repo, locator, currentUser, NullLogger<GetMyApplicationStatusQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetMyApplicationStatusQuery(requestedUserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);

        await repo.DidNotReceiveWithAnyArgs().GetWithDocumentsByUserIdAsync(default, default);
        await repo.DidNotReceiveWithAnyArgs().GetFileAssetIdsByDocumentIdsAsync(default!, default);
        await locator.DidNotReceiveWithAnyArgs().GetByIdsAsync(default!, default);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static Guid StubLinkedFileAsset(
        IProviderApplicationRepository repo, IFileAssetLocator locator, Guid documentId)
    {
        var fileAssetId = Guid.CreateVersion7();

        repo.GetFileAssetIdsByDocumentIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<Guid, Guid>)new Dictionary<Guid, Guid>
            {
                [documentId] = fileAssetId,
            });

        var view = new FileAssetView(
            Id: fileAssetId,
            StorageProvider: "Local",
            StorageKey: FileAssetStorageKey,
            ContentType: "application/pdf",
            Extension: ".pdf",
            OriginalFileName: FileAssetOriginalName,
            SafeFileName: FileAssetOriginalName,
            SizeBytes: FileAssetSizeBytes);

        locator.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<Guid, FileAssetView>)new Dictionary<Guid, FileAssetView>
            {
                [fileAssetId] = view,
            });

        return fileAssetId;
    }

    private static void StubNoLink(
        IProviderApplicationRepository repo, IFileAssetLocator locator)
    {
        repo.GetFileAssetIdsByDocumentIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<Guid, Guid>)new Dictionary<Guid, Guid>());

        locator.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyDictionary<Guid, FileAssetView>)new Dictionary<Guid, FileAssetView>());
    }

    private static ICurrentUser BuildCurrentUser(Guid userId)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        return currentUser;
    }

    private static (ProviderApplication application, ProviderDocument document)
        BuildApplicationWithDocument(Guid ownerUserId)
    {
        var application = ProviderApplication.Register(
            userId:               ownerUserId,
            type:                 ProviderType.TourOperator,
            businessName:         "Acme Tours",
            contactEmail:         "owner@example.com",
            contactPhone:         "+97400000000",
            address:              "Doha",
            description:          "Tours",
            typeSpecificDataJson: null).Value!;

        var addResult = application.AddDocument(
            documentType: DocumentType.BusinessLicense,
            fileUrl:       LegacyFileUrl,
            fileName:      LegacyFileName,
            fileSizeBytes: LegacyFileSizeBytes);
        var doc = addResult.Value!;

        var navProperty = typeof(ProviderDocument).GetProperty(nameof(ProviderDocument.Application));
        navProperty!.SetValue(doc, application);

        return (application, doc);
    }
}
