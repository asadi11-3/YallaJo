using System.Text;
using Accounts.Application.Interfaces;
using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using ContentCore.Contracts.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Accounts.Infrastructure.Persistence.Seeding;

/// <summary>
/// DEV-SEED-B1 — Development / QA-only seeder for the Accounts module.
/// <para>
/// Seeds (1) a <see cref="Profile"/> for every <c>seed.*@yallajo.dev</c> dev account and
/// (2) one APPROVED <see cref="ProviderApplication"/> (with downloadable provider documents)
/// for the dev provider user, so provider/document QA flows have realistic data.
/// </para>
/// <para>
/// Guarded by <see cref="IHostEnvironment.IsDevelopment"/> (never runs in Production) and
/// idempotent per-row. Provider documents are materialized through the SAME secure write path
/// a real upload uses (<see cref="IFileStorageService"/> → <see cref="IFileAssetRegistrar"/> →
/// <see cref="IProviderDocumentFileWriter"/>), landing under the 404-shielded
/// <c>provider-application-documents</c> folder. Existing seeders are untouched.
/// </para>
/// </summary>
public sealed class DevAccountsSeeder(
    AccountsDbContext dbContext,
    IProviderApplicationRepository providerApplicationRepository,
    IFileStorageService fileStorageService,
    IFileAssetRegistrar fileAssetRegistrar,
    IProviderDocumentFileWriter providerDocumentFileWriter,
    IHostEnvironment hostEnvironment,
    ILogger<DevAccountsSeeder> logger) : IModuleDbInitializer
{
    public int Order => 161;

    private const string StorageFolder = "provider-application-documents";

    private static readonly Guid AdminUserId = DevSeedIds.AdminUserId;

    /// <summary>Minimal valid single-page PDF used as each seeded document's physical content.</summary>
    private static readonly byte[] PlaceholderPdfBytes = Encoding.ASCII.GetBytes(
        "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n" +
        "2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
        "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 200]>>endobj\n" +
        "trailer<</Root 1 0 R>>\n%%EOF\n");

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        await SeedProfilesAsync(cancellationToken);
        await SeedProviderApplicationAsync(cancellationToken);
    }

    private async Task SeedProfilesAsync(CancellationToken cancellationToken)
    {
        var existingProfileUserIds = (await dbContext.Profiles
                .IgnoreQueryFilters()
                .Select(p => p.UserId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var newProfiles = new List<Profile>();
        foreach (var account in DevSeedProfiles.All)
        {
            if (existingProfileUserIds.Contains(account.UserId))
            {
                continue;
            }

            var profile = Profile.Create(account.UserId, account.FirstName, account.LastName);
            profile.SetDisplayName($"{account.FirstName} {account.LastName}");
            profile.SetAvatarUrl($"https://cdn.yallajo.local/avatars/{account.UserId:N}.png");
            newProfiles.Add(profile);
        }

        if (newProfiles.Count == 0)
        {
            return;
        }

        dbContext.Profiles.AddRange(newProfiles);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("DEV-SEED-B1: seeded {Count} development profile(s).", newProfiles.Count);
    }

    private async Task SeedProviderApplicationAsync(CancellationToken cancellationToken)
    {
        // Per-row idempotency: only seed if the dev provider has no application yet.
        var alreadyExists = await dbContext.ProviderApplications
            .IgnoreQueryFilters()
            .AnyAsync(a => a.UserId == DevSeedIds.ProviderUserId, cancellationToken);
        if (alreadyExists)
        {
            return;
        }

        var registerResult = ProviderApplication.Register(
            DevSeedIds.ProviderUserId,
            ProviderType.IndependentGuide,
            "Dev Seed Guide Services",
            "seed.provider@yallajo.dev",
            "+962790000010",
            "Amman, Jordan",
            "DEV-SEED-B1: approved independent guide provider for manual QA.");
        EnsureSuccess(registerResult, "Register");
        var application = registerResult.Value!;
        SetProperty(application, nameof(ProviderApplication.Id), DevSeedIds.ProviderApplicationId);

        var expiresAt = DateTime.UtcNow.AddDays(365);
        foreach (var docType in application.GetMissingDocumentTypes())
        {
            EnsureSuccess(application.AddDocument(docType, expiresAt), $"AddDocument({docType})");
        }

        EnsureSuccess(application.Submit(), "Submit");
        EnsureSuccess(application.Approve(AdminUserId), "Approve");

        dbContext.ProviderApplications.Add(application);

        // Persist first so each ProviderDocument.Id is durable before file materialization.
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var document in application.Documents)
        {
            await MaterializeFileAssetLinkAsync(DevSeedIds.ProviderUserId, document, cancellationToken);
        }

        logger.LogInformation(
            "DEV-SEED-B1: seeded approved provider application {ApplicationId} with {DocCount} document(s).",
            application.Id, application.Documents.Count);
    }

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
            return;
        }

        var fileName = $"{document.DocumentType.ToString().ToLowerInvariant()}-{document.Id:N}.pdf";

        using var contentStream = new MemoryStream(PlaceholderPdfBytes, writable: false);
        var uploadResponse = await fileStorageService.UploadAsync(
            contentStream, fileName, "application/pdf", StorageFolder, cancellationToken);

        if (uploadResponse.IsFailure || uploadResponse.Value is null)
        {
            throw new InvalidOperationException(
                $"DEV-SEED-B1: ProviderDocument {document.Id} ({document.DocumentType}) failed to store its placeholder file: " +
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
            await TryCleanupAsync(uploaded.Url, cancellationToken);
            throw new InvalidOperationException(
                $"DEV-SEED-B1: ProviderDocument {document.Id} ({document.DocumentType}) failed to register its FileAsset: " +
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
                $"DEV-SEED-B1: ProviderDocument {document.Id} ({document.DocumentType}) failed to link its FileAsset.", ex);
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
                "DEV-SEED-B1 cleanup failed for orphaned provider-document file {FileUrl}; manual removal may be required.",
                fileUrl);
        }
    }

    private static void EnsureSuccess(Result result, string operation)
    {
        if (result.IsSuccess)
        {
            return;
        }

        throw new InvalidOperationException(
            $"DEV-SEED-B1: provider application failed at {operation}: {FormatFailure(result.Errors, result.Messages)}");
    }

    private static void EnsureSuccess<T>(Result<T> result, string operation)
    {
        if (result.IsSuccess)
        {
            return;
        }

        throw new InvalidOperationException(
            $"DEV-SEED-B1: provider application failed at {operation}: {FormatFailure(result.Errors, result.Messages)}");
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

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (property is null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().FullName}.");
        }

        property.SetValue(target, value);
    }
}
