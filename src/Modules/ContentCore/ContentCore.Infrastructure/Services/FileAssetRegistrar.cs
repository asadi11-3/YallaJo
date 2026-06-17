using ContentCore.Contracts.Storage;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Infrastructure.Services;

/// <summary>
/// Idempotent FileAsset registrar (Patch 2B). Implements the
/// <see cref="IFileAssetRegistrar"/> contract consumed by cross-module callers
/// (e.g. Accounts backfill) so callers never touch ContentCoreDbContext directly.
/// <para>
/// Idempotency strategy:
/// 1. SELECT existing row by <see cref="FileAssetSeed.StorageKey"/> (AsNoTracking).
///    If found -> return existing Id with <see cref="FileAssetRecord.WasReused"/> = true.
/// 2. Otherwise build the entity and Add it to the change tracker.
/// 3. If <paramref name="dryRun"/> is true, do NOT call SaveChangesAsync; detach
///    the just-Added entity and return Id = Guid.Empty + WasReused = false.
/// 4. Else SaveChangesAsync. On <see cref="DbUpdateException"/> caused by a SQL
///    Server unique-violation (2627/2601) on <c>UX_FileAssets_StorageKey</c>,
///    detach and re-query: the racing winner's row is returned with WasReused = true.
/// </para>
/// </summary>
internal sealed class FileAssetRegistrar(
    ContentCoreDbContext dbContext,
    IContentCoreUnitOfWork unitOfWork)
    : IFileAssetRegistrar
{
    public async Task<Result<FileAssetRecord>> GetOrAddByStorageKeyAsync(
        FileAssetSeed seed,
        bool dryRun,
        CancellationToken ct = default)
    {
        if (seed is null)
            return Result<FileAssetRecord>.Failure(
                Error.Validation("file-asset", "Seed is required."), Outcome.Invalid);

        if (string.IsNullOrWhiteSpace(seed.StorageKey))
            return Result<FileAssetRecord>.Failure(
                Error.Validation("file-asset", "StorageKey is required."), Outcome.Invalid);

        // 1. Existence probe — by unique StorageKey.
        var existingId = await dbContext.FileAssets
            .AsNoTracking()
            .Where(f => f.StorageKey == seed.StorageKey)
            .Select(f => f.Id)
            .FirstOrDefaultAsync(ct);

        if (existingId != Guid.Empty)
            return Result<FileAssetRecord>.Success(new FileAssetRecord(existingId, WasReused: true));

        // 2. Build entity. FileAsset.Create validates required fields.
        FileAsset asset;
        try
        {
            asset = FileAsset.Create(
                storageProvider: seed.StorageProvider,
                storageKey: seed.StorageKey,
                originalFileName: seed.OriginalFileName,
                safeFileName: seed.SafeFileName,
                contentType: seed.ContentType,
                extension: seed.Extension,
                sizeBytes: seed.SizeBytes,
                uploadedByUserId: seed.UploadedByUserId);
        }
        catch (ArgumentException ex)
        {
            return Result<FileAssetRecord>.Failure(
                Error.Validation("file-asset", ex.Message), Outcome.Invalid);
        }

        dbContext.FileAssets.Add(asset);

        // 3. Dry-run: do NOT persist. Detach and return placeholder.
        if (dryRun)
        {
            dbContext.Entry(asset).State = EntityState.Detached;
            return Result<FileAssetRecord>.Success(new FileAssetRecord(Guid.Empty, WasReused: false));
        }

        // 4. Persist. Handle unique-violation race on UX_FileAssets_StorageKey.
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return Result<FileAssetRecord>.Success(new FileAssetRecord(asset.Id, WasReused: false));
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            dbContext.Entry(asset).State = EntityState.Detached;

            var winnerId = await dbContext.FileAssets
                .AsNoTracking()
                .Where(f => f.StorageKey == seed.StorageKey)
                .Select(f => f.Id)
                .FirstOrDefaultAsync(ct);

            if (winnerId == Guid.Empty)
            {
                // Race lost but cannot locate winner — surface as conflict for caller.
                return Result<FileAssetRecord>.Failure(
                    Error.Conflict("FileAsset", "Unique-violation but no row found for StorageKey."),
                    Outcome.Conflict);
            }

            return Result<FileAssetRecord>.Success(new FileAssetRecord(winnerId, WasReused: true));
        }
    }

    /// <summary>
    /// True when the wrapped SqlException is a unique-key/primary-key violation
    /// (2627 = unique constraint, 2601 = unique index).
    /// </summary>
    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sql)
        {
            foreach (SqlError error in sql.Errors)
            {
                if (error.Number == 2627 || error.Number == 2601)
                    return true;
            }
        }
        return false;
    }
}
