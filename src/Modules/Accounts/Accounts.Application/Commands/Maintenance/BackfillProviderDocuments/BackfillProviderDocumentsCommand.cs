using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Maintenance.BackfillProviderDocuments;

/// <summary>
/// Backfills legacy <see cref="Accounts.Domain.Entities.ProviderDocument"/> rows into
/// the Patch 2A FileAsset + ProviderDocumentFiles tables. Idempotent and re-runnable.
/// Always read-side from <see cref="Accounts.Domain.Entities.ProviderDocument.FileUrl"/>;
/// never mutates the source row. Dry-run defaults ON at the endpoint layer.
/// </summary>
/// <param name="MaxRows">
/// Hard upper bound on the number of documents inspected this invocation. The
/// handler reads in batches of 100 until either <paramref name="MaxRows"/> is
/// reached or the page returns fewer rows than requested.
/// </param>
/// <param name="DryRun">
/// When true (default at the endpoint), nothing is persisted: the handler still
/// inspects each row and reports counts but does NOT call SaveChangesAsync on
/// either the FileAssets table or the ProviderDocumentFiles table.
/// </param>
/// <param name="AfterId">
/// Keyset cursor. Pass <c>null</c> or <see cref="System.Guid.Empty"/> for the
/// first invocation. The handler returns <c>NextAfterId</c> for the caller to
/// resume from on the next call.
/// </param>
public sealed record BackfillProviderDocumentsCommand(
    int MaxRows,
    bool DryRun,
    Guid? AfterId) : ICommand<BackfillProviderDocumentsReport>;
