using ContentCore.Contracts.Storage;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Infrastructure.Services;

/// <summary>
/// Read-side FileAsset projection (Patch 2C). Implements the
/// <see cref="IFileAssetLocator"/> contract consumed by cross-module callers
/// (e.g. the Accounts provider-document download handler) so callers never
/// touch ContentCoreDbContext directly.
/// <para>
/// Single AsNoTracking projection query Where(Id == fileAssetId)
/// Select(new FileAssetView(...)) FirstOrDefaultAsync. The FileAsset
/// global query filter (!IsDeleted) auto-applies, so soft-deleted rows
/// behave as not-found.
/// </para>
/// </summary>
internal sealed class FileAssetLocator(ContentCoreDbContext dbContext) : IFileAssetLocator
{
    public async Task<Result<FileAssetView>> GetByIdAsync(
        Guid fileAssetId,
        CancellationToken ct = default)
    {
        if (fileAssetId == Guid.Empty)
        {
            return Result<FileAssetView>.Failure(
                Error.Validation("file-asset", "FileAssetId is required."), Outcome.Invalid);
        }

        var view = await dbContext.FileAssets
            .AsNoTracking()
            .Where(f => f.Id == fileAssetId)
            .Select(f => new FileAssetView(
                f.Id,
                f.StorageProvider,
                f.StorageKey,
                f.ContentType,
                f.Extension,
                f.OriginalFileName,
                f.SafeFileName,
                f.SizeBytes))
            .FirstOrDefaultAsync(ct);

        if (view is null)
        {
            return Result<FileAssetView>.Failure(
                Error.NotFound("FileAsset"), Outcome.NotFound);
        }

        return Result<FileAssetView>.Success(view);
    }
}
