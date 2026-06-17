using System.Text;
using Accounts.Application.Interfaces;
using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using ContentCore.Contracts.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Accounts.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds Accounts.ProviderApplication aggregates for the four test "provider" users
/// (guide-pending, guide-approved, business, agency). Runs AFTER AccountsDbInitializer so
/// the corresponding Profiles already exist. Idempotent: re-running adds nothing.
/// <para>
/// Patch 2G: because V2 is FileAsset-only, every seeded <see cref="ProviderDocument"/> is
/// materialized through the SAME write path a real multipart upload uses — a placeholder file
/// is stored via <see cref="IFileStorageService"/>, registered as a <c>FileAsset</c> via
/// <see cref="IFileAssetRegistrar"/>, and linked via <see cref="IProviderDocumentFileWriter"/>.
/// This guarantees seeded provider documents are downloadable and never leave dangling,
/// link-less ProviderDocument rows in the database.
/// </para>
/// </summary>
public sealed class AccountsProviderApplicationSeeder(
    AccountsDbContext dbContext,
    IProviderApplicationRepository providerApplicationRepository,
    IFileStorageService fileStorageService,
    IFileAssetRegistrar fileAssetRegistrar,
    IProviderDocumentFileWriter providerDocumentFileWriter,
    ILogger<AccountsProviderApplicationSeeder> logger) : IModuleDbInitializer
{
    public int Order => 51;

    private const string StorageFolder = "provider-application-documents";

    /// <summary>Minimal valid single-page PDF used as the seeded document's physical content.</summary>
    private static readonly byte[] PlaceholderPdfBytes = Encoding.ASCII.GetBytes(
        "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n" +
        "2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
        "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 200]>>endobj\n" +
        "trailer<</Root 1 0 R>>\n%%EOF\n");

    private static readonly Guid AdminUserId = new("b0000000-0000-0000-0000-000000000001");

    /// <summary>End-state target for each seeded provider application.</summary>
    private enum SeedEndState
    {
        Pending,
        Approved
    }

    private sealed record ProviderSeedSpec(
        Guid UserId,
        ProviderType Type,
        string BusinessName,
        string ContactEmail,
        string ContactPhone,
        string Address,
        string Description,
        SeedEndState EndState
    );

    private static readonly IReadOnlyList<ProviderSeedSpec> Targets =
    [
        new(
            new Guid("b0000000-0000-0000-0000-000000000004"),
            ProviderType.IndependentGuide,
            "Pending Guide Co.",
            "guide-pending@yallajo.test",
            "+962790000004",
            "Amman, Jordan",
            "Test seed: independent guide, awaiting admin review.",
            SeedEndState.Pending
        ),
        new(
            new Guid("b0000000-0000-0000-0000-000000000005"),
            ProviderType.IndependentGuide,
            "Approved Guide Co.",
            "guide-approved@yallajo.test",
            "+962790000005",
            "Amman, Jordan",
            "Test seed: independent guide, approved provider.",
            SeedEndState.Approved
        ),
        new(
            new Guid("b0000000-0000-0000-0000-000000000006"),
            ProviderType.BusinessOwner,
            "Test Business LLC",
            "business@yallajo.test",
            "+962790000006",
            "Amman, Jordan",
            "Test seed: business owner, approved provider.",
            SeedEndState.Approved
        ),
        new(
            new Guid("b0000000-0000-0000-0000-000000000007"),
            ProviderType.Agency,
            "Test Travel Agency",
            "agency@yallajo.test",
            "+962790000007",
            "Amman, Jordan",
            "Test seed: travel agency, approved provider.",
            SeedEndState.Approved
        )
    ];

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Idempotent: skip seeds that already have an application row.
        var existingAppUserIds = await dbContext.ProviderApplications
            .Select(a => a.UserId)
            .ToListAsync(cancellationToken);
        var existing = new HashSet<Guid>(existingAppUserIds);

        var pending = Targets.Where(t => !existing.Contains(t.UserId)).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        var seededDocuments = new List<(Guid UserId, ProviderDocument Document)>();
        foreach (var spec in pending)
        {
            var application = BuildApplication(spec);
            dbContext.ProviderApplications.Add(application);
            foreach (var document in application.Documents)
            {
                seededDocuments.Add((spec.UserId, document));
            }
        }

        // Persist applications + documents first so each ProviderDocument.Id is durable
        // before we materialize its FileAsset + ProviderDocumentFile link.
        await dbContext.SaveChangesAsync(cancellationToken);

        // Patch 2G: V2 is FileAsset-only — give every seeded document a real, downloadable
        // file by mirroring the production upload write path.
        foreach (var (userId, document) in seededDocuments)
        {
            await MaterializeFileAssetLinkAsync(userId, document, cancellationToken);
        }
    }

    /// <summary>
    /// Stores a placeholder file, registers it as a FileAsset, and links it to the seeded
    /// ProviderDocument — exactly as <c>UploadProviderDocumentCommandHandler</c> does for a
    /// real multipart upload. Idempotent: when a <c>ProviderDocumentFile</c> link already
    /// exists for the document, the whole flow (upload + FileAsset registration + link upsert)
    /// is skipped, so repeated Development seeding never re-uploads placeholder files or
    /// repoints existing links.
    /// </summary>
    private async Task MaterializeFileAssetLinkAsync(
        Guid userId,
        ProviderDocument document,
        CancellationToken cancellationToken)
    {
        // Idempotency guard: skip documents that already have a FileAsset link.
        var existingFileAssetId = await providerApplicationRepository
            .GetFileAssetIdByDocumentIdAsync(document.Id, cancellationToken);
        if (existingFileAssetId is not null)
        {
            logger.LogInformation(
                "Seed provider document {DocumentId} already has FileAsset link; skipping materialization.",
                document.Id);
            return;
        }

        var fileName = $"{document.DocumentType.ToString().ToLowerInvariant()}-{document.Id:N}.pdf";

        using var contentStream = new MemoryStream(PlaceholderPdfBytes, writable: false);
        var uploadResponse = await fileStorageService.UploadAsync(
            contentStream, fileName, "application/pdf", StorageFolder, cancellationToken);

        if (uploadResponse.IsFailure || uploadResponse.Value is null)
        {
            throw new InvalidOperationException(
                $"Seeding ProviderDocument {document.Id} ({document.DocumentType}) failed to store its placeholder file: " +
                FormatFailure(uploadResponse.Errors, uploadResponse.Messages));
        }

        var uploaded = uploadResponse.Value;

        var seed = new FileAssetSeed(
            StorageProvider: "Local",
            StorageKey: uploaded.StorageKey,
            OriginalFileName: fileName,
            SafeFileName: fileName,
            ContentType: "application/pdf",
            Extension: ".pdf",
            SizeBytes: uploaded.FileSize > 0 ? uploaded.FileSize : PlaceholderPdfBytes.Length,
            UploadedByUserId: userId);

        var registrarResult = await fileAssetRegistrar.GetOrAddByStorageKeyAsync(seed, dryRun: false, cancellationToken);
        if (registrarResult.IsFailure || registrarResult.Value is null)
        {
            // Clean up the orphaned blob — never leave a stored file with no FileAsset row.
            await TryCleanupAsync(uploaded.Url, cancellationToken);
            throw new InvalidOperationException(
                $"Seeding ProviderDocument {document.Id} ({document.DocumentType}) failed to register its FileAsset: " +
                FormatFailure(registrarResult.Errors, registrarResult.Messages));
        }

        try
        {
            await providerDocumentFileWriter.UpsertLinkAsync(
                document.Id, registrarResult.Value.Id, document.DocumentType, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await TryCleanupAsync(uploaded.Url, cancellationToken);
            throw new InvalidOperationException(
                $"Seeding ProviderDocument {document.Id} ({document.DocumentType}) failed to link its FileAsset.", ex);
        }
    }

    private async Task TryCleanupAsync(string fileUrl, CancellationToken ct)
    {
        try
        {
            await fileStorageService.DeleteAsync(fileUrl, ct);
        }
        catch (Exception cleanupEx)
        {
            logger.LogError(cleanupEx,
                "Seeding cleanup failed for orphaned provider-document file {FileUrl}; manual removal may be required.",
                fileUrl);
        }
    }

    private static ProviderApplication BuildApplication(ProviderSeedSpec spec)
    {
        var registerResult = ProviderApplication.Register(
            spec.UserId,
            spec.Type,
            spec.BusinessName,
            spec.ContactEmail,
            spec.ContactPhone,
            spec.Address,
            spec.Description
        );
        EnsureSuccess(registerResult, "Register", spec);
        var application = registerResult.Value!;

        AddRequiredDocuments(application, spec);

        var submitResult = application.Submit();
        EnsureSuccess(submitResult, "Submit", spec);

        if (spec.EndState == SeedEndState.Approved)
        {
            var approveResult = application.Approve(AdminUserId);
            EnsureSuccess(approveResult, "Approve", spec);
        }

        return application;
    }

    private static void AddRequiredDocuments(ProviderApplication application, ProviderSeedSpec spec)
    {
        var missingDocs = application.GetMissingDocumentTypes();
        var expiresAt = DateTime.UtcNow.AddDays(365);

        // Patch 2G: ProviderDocument carries only business metadata (DocumentType / ExpiresAt).
        // The physical file (StorageKey/OriginalFileName/SizeBytes) lives in the FileAsset V2 model.
        // Each seeded document's FileAsset + ProviderDocumentFile link is materialized in a second
        // pass (MaterializeFileAssetLinkAsync) after these aggregates are persisted, so seeded
        // documents are fully downloadable just like a real upload.
        foreach (var docType in missingDocs)
        {
            var addResult = application.AddDocument(docType, expiresAt);
            EnsureSuccess(addResult, $"AddDocument({docType})", spec);
        }
    }

    private static void EnsureSuccess(Result result, string operation, ProviderSeedSpec spec)
    {
        if (result.IsSuccess)
        {
            return;
        }

        var detail = FormatFailure(result.Errors, result.Messages);
        throw new InvalidOperationException(
            $"Seeding ProviderApplication for {spec.ContactEmail} failed at {operation}: {detail}");
    }

    private static void EnsureSuccess<T>(Result<T> result, string operation, ProviderSeedSpec spec)
    {
        if (result.IsSuccess)
        {
            return;
        }

        var detail = FormatFailure(result.Errors, result.Messages);
        throw new InvalidOperationException(
            $"Seeding ProviderApplication for {spec.ContactEmail} failed at {operation}: {detail}");
    }

    private static string FormatFailure(IReadOnlyList<Error> errors, IReadOnlyList<string> messages)
    {
        if (errors.Count > 0)
        {
            return string.Join("; ", errors.Select(e => $"{e.Code}: {e.Message}"));
        }

        if (messages.Count > 0)
        {
            return string.Join("; ", messages);
        }

        return "Unknown failure.";
    }
}
