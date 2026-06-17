using Accounts.Application.Commands.Provider.Shared;
using Accounts.Application.Interfaces;
using Accounts.Domain.Entities;
using Accounts.Domain.Repositories;
using ContentCore.Contracts.Storage;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Maintenance.BackfillProviderDocuments;

/// <summary>
/// Patch 2B backfill handler. See <see cref="BackfillProviderDocumentsCommand"/> for scope.
/// <para>
/// PII discipline: this handler MUST NOT log <c>FileUrl</c>, <c>StorageKey</c>,
/// physical paths, or <c>FileName</c> at Information level or higher. The
/// extension (e.g. ".pdf") is safe to log; the document id (a GUIDv7) is safe.
/// </para>
/// </summary>
public sealed class BackfillProviderDocumentsCommandHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IProviderDocumentBackfillStore backfillStore,
    IFileAssetRegistrar fileAssetRegistrar,
    IFileStorageService fileStorageService,
    ILogger<BackfillProviderDocumentsCommandHandler> logger)
    : ICommandHandler<BackfillProviderDocumentsCommand, BackfillProviderDocumentsReport>
{
    // Internal limits — fixed by patch scope (b45).
    private const int BatchSize = 100;
    private const int MaxRowsHardCap = 5000;
    private const int MaxRowsDefault = 500;

    public async Task<Result<BackfillProviderDocumentsReport>> Handle(
        BackfillProviderDocumentsCommand request,
        CancellationToken cancellationToken)
    {
        // Pre-flight: refuse to run unless both Patch 2A migrations are applied
        // on BOTH module schemas. Idempotent migrations check via the
        // application-layer port (no Infrastructure dependency).
        var migrations = await backfillStore.CheckMigrationsAsync(cancellationToken);
        if (!migrations.BothApplied)
        {
            var detail = (migrations.AccountsApplied, migrations.ContentCoreApplied) switch
            {
                (false, false) => "Patch 2A migrations 'AddProviderDocumentFiles' (accounts) and 'AddFileAssets' (content_core) are not applied.",
                (false, true) => "Patch 2A Accounts migration 'AddProviderDocumentFiles' is not applied.",
                (true, false) => "Patch 2A ContentCore migration 'AddFileAssets' is not applied.",
                _ => "Migrations check failed.",
            };
            return Result<BackfillProviderDocumentsReport>.Failure(
                Error.Validation("backfill", detail),
                Outcome.Invalid);
        }

        var runId = Guid.CreateVersion7();
        var maxRows = Math.Clamp(
            request.MaxRows > 0 ? request.MaxRows : MaxRowsDefault,
            1,
            MaxRowsHardCap);
        var dryRun = request.DryRun;
        var baseUrl = backfillStore.GetFileStorageBaseUrl();

        logger.LogInformation(
            "Backfill run {RunId} started: DryRun={DryRun} MaxRows={MaxRows}",
            runId, dryRun, maxRows);

        var skipped = new Dictionary<string, int>(StringComparer.Ordinal);
        int total = 0;
        int inserted = 0;
        int reused = 0;
        int errors = 0;

        var cursor = request.AfterId ?? Guid.Empty;
        Guid? nextAfterId = null;
        bool sawShortPage = false;

        while (total < maxRows)
        {
            var take = Math.Min(BatchSize, maxRows - total);
            var batch = await providerApplicationRepository
                .GetDocumentsForBackfillAsync(cursor, take, cancellationToken);

            if (batch.Count == 0)
            {
                sawShortPage = true;
                break;
            }

            foreach (var doc in batch)
            {
                total++;
                cursor = doc.Id;
                nextAfterId = doc.Id;

                try
                {
                    var rowOutcome = await ProcessRowAsync(doc, baseUrl, dryRun, runId, cancellationToken);
                    switch (rowOutcome.Kind)
                    {
                        case RowOutcomeKind.Inserted:
                            inserted++;
                            break;
                        case RowOutcomeKind.Reused:
                            reused++;
                            break;
                        case RowOutcomeKind.Skipped:
                            Increment(skipped, rowOutcome.SkipReason!);
                            break;
                        case RowOutcomeKind.Error:
                            errors++;
                            break;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Never log ex.Message (may contain a physical path). Only the type name.
                    logger.LogError(
                        "Backfill run {RunId}: document {DocumentId} raised {ExceptionType}",
                        runId, doc.Id, ex.GetType().Name);
                    errors++;
                }
            }

            // Page returned fewer than requested -> end of stream.
            if (batch.Count < take)
            {
                sawShortPage = true;
                break;
            }
        }

        // HasMore is true only when we exited because MaxRows was reached AND
        // the final batch was full (no short page seen). It is a hint to the
        // caller to resume from NextAfterId.
        bool hasMore = total >= maxRows && !sawShortPage;

        logger.LogInformation(
            "Backfill run {RunId} completed: Total={Total} Inserted={Inserted} Reused={Reused} Errors={Errors} DryRun={DryRun}",
            runId, total, inserted, reused, errors, dryRun);

        return Result<BackfillProviderDocumentsReport>.Success(
            new BackfillProviderDocumentsReport(
                RunId: runId,
                DryRun: dryRun,
                Total: total,
                Inserted: inserted,
                Reused: reused,
                Errors: errors,
                SkippedByReason: skipped,
                NextAfterId: nextAfterId,
                HasMore: hasMore));
    }

    private async Task<RowOutcome> ProcessRowAsync(
        ProviderDocument doc,
        string baseUrl,
        bool dryRun,
        Guid runId,
        CancellationToken ct)
    {
        // Pre-check #1: already linked?
        if (await backfillStore.IsAlreadyLinkedAsync(doc.Id, ct))
            return RowOutcome.Skipped("AlreadyLinked");

        // Pre-check #2: parse FileUrl -> StorageKey/extension/contentType.
        var parse = ProviderDocumentFileUrlParser.Parse(doc.FileUrl, baseUrl);
        if (!parse.IsSuccess)
        {
            logger.LogWarning(
                "Backfill run {RunId}: document {DocumentId} skipped: {SkipReason}",
                runId, doc.Id, parse.SkipReason);
            return RowOutcome.Skipped(parse.SkipReason!);
        }

        // Pre-check #3: physical file exists? Reuse OpenReadAsync per (b45) §5;
        // immediately dispose the stream.
        var existence = await fileStorageService.OpenReadAsync(doc.FileUrl, ct);
        if (existence.IsFailure || existence.Value is null)
        {
            logger.LogWarning(
                "Backfill run {RunId}: document {DocumentId} skipped: FileNotFound (ext={Extension})",
                runId, doc.Id, parse.Extension);
            return RowOutcome.Skipped("FileNotFound");
        }
        existence.Value.Content.Dispose();

        // Get-or-add FileAsset via cross-module port.
        var safeName = SafeFileNameSanitizer.Sanitize(doc.FileName, $"document-{doc.Id:N}");
        var seed = new FileAssetSeed(
            StorageProvider: "Local",
            StorageKey: parse.StorageKey!,
            OriginalFileName: doc.FileName ?? string.Empty,
            SafeFileName: safeName,
            ContentType: parse.ContentType!,
            Extension: parse.Extension!,
            SizeBytes: doc.FileSizeBytes,
            UploadedByUserId: doc.Application.UserId);

        var registrarResult = await fileAssetRegistrar.GetOrAddByStorageKeyAsync(seed, dryRun, ct);
        if (registrarResult.IsFailure || registrarResult.Value is null)
        {
            logger.LogWarning(
                "Backfill run {RunId}: document {DocumentId} registrar failed (ext={Extension})",
                runId, doc.Id, parse.Extension);
            return RowOutcome.Error();
        }

        var fileAssetRecord = registrarResult.Value;
        bool wasReused = fileAssetRecord.WasReused;

        // Dry-run: report would-be link creation; do not touch DbContext.
        if (dryRun)
            return wasReused ? RowOutcome.Reused() : RowOutcome.Inserted();

        // Create link row via the store. The store catches the
        // UX_ProviderDocumentFiles_ProviderDocumentId race and reports AlreadyLinked.
        var linkResult = await backfillStore.InsertLinkAsync(
            doc.Id, fileAssetRecord.Id, doc.DocumentType, ct);

        if (linkResult == LinkInsertResult.AlreadyLinked)
        {
            logger.LogWarning(
                "Backfill run {RunId}: document {DocumentId} skipped: AlreadyLinked (race)",
                runId, doc.Id);
            return RowOutcome.Skipped("AlreadyLinked");
        }

        // Per-row Information log only on actual insert. UploaderSource documents
        // that the UploadedByUserId is the owning ProviderApplication.UserId — NOT
        // necessarily the original uploader (b45 / m0396 answer 7).
        logger.LogInformation(
            "Backfill run {RunId}: linked document {DocumentId} -> fileAsset {FileAssetId} UploaderSource=ApplicationOwner WasReused={WasReused}",
            runId, doc.Id, fileAssetRecord.Id, wasReused);

        return wasReused ? RowOutcome.Reused() : RowOutcome.Inserted();
    }

    private static void Increment(Dictionary<string, int> map, string key)
    {
        map[key] = map.TryGetValue(key, out var current) ? current + 1 : 1;
    }

    private readonly struct RowOutcome
    {
        public RowOutcomeKind Kind { get; }
        public string? SkipReason { get; }
        private RowOutcome(RowOutcomeKind kind, string? skipReason)
        {
            Kind = kind;
            SkipReason = skipReason;
        }
        public static RowOutcome Inserted() => new(RowOutcomeKind.Inserted, null);
        public static RowOutcome Reused() => new(RowOutcomeKind.Reused, null);
        public static RowOutcome Skipped(string reason) => new(RowOutcomeKind.Skipped, reason);
        public static RowOutcome Error() => new(RowOutcomeKind.Error, null);
    }

    private enum RowOutcomeKind { Inserted, Reused, Skipped, Error }
}
