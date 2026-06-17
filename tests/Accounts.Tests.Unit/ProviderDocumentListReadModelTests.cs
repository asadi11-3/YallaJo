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
/// Patch 2G read-model tests for the two provider-document list/index handlers
/// (admin application details + my-application status). FileAsset V2 is now the
/// ONLY source of document display metadata: each handler reads FileName/SizeBytes
/// from the linked FileAsset when a ProviderDocumentFile link exists, and renders
/// EMPTY metadata (no legacy ProviderDocument.FileName/FileSizeBytes fallback) when
/// no link exists. The internal StorageKey is never projected, FileUrl no longer
/// exists on the DTOs, authorization is preserved, and the cross-module reads are
/// batched (no N+1).
/// </summary>
public sealed class ProviderDocumentListReadModelTests
{
    private const string FileAssetOriginalName = "RealOriginal.pdf";
    private const long FileAssetSizeBytes = 4242;
    private const string FileAssetStorageKey = "provider-application-documents/secret.pdf";

    // ── Admin details: FileAsset metadata is used when a link exists ──────────
    [Fact]
    public async Task Admin_details_use_FileAsset_metadata_when_link_exists()
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

        // Batched once -> no N+1.
        await repo.Received(1).GetFileAssetIdsByDocumentIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
        await locator.Received(1).GetByIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
        _ = fileAssetId;
    }

    // ── Admin details: empty metadata when no link exists (no legacy fallback) ─
    [Fact]
    public async Task Admin_details_render_empty_metadata_when_no_link_exists()
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
        projected.FileName.Should().BeEmpty();
        projected.FileSizeBytes.Should().Be(0);
    }

    // ── Status: FileAsset metadata is used when a link exists ─────────────────
    [Fact]
    public async Task Status_uses_FileAsset_metadata_when_link_exists()
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

        await repo.Received(1).GetFileAssetIdsByDocumentIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
        await locator.Received(1).GetByIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
    }

    // ── Status: empty metadata when no link exists (no legacy fallback) ───────
    [Fact]
    public async Task Status_renders_empty_metadata_when_no_link_exists()
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
        result.Value!.Documents.Single().FileName.Should().BeEmpty();
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
        // The display name is the FileAsset's human-readable original name, never the
        // internal storage key / physical path.
        d.FileName.Should().Be(FileAssetOriginalName);
        d.FileName.Should().NotContain(FileAssetStorageKey);
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

        // Patch 2G: AddDocument no longer carries file metadata (FileAsset is authoritative).
        var addResult = application.AddDocument(documentType: DocumentType.BusinessLicense);
        var doc = addResult.Value!;

        var navProperty = typeof(ProviderDocument).GetProperty(nameof(ProviderDocument.Application));
        navProperty!.SetValue(doc, application);

        return (application, doc);
    }
}
