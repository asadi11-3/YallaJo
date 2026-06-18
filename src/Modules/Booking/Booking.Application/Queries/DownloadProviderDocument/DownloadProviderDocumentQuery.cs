using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.DownloadProviderDocument;

/// <summary>
/// Requests an authorized, server-mediated download of a Booking provider document.
/// The handler enforces owner-or-admin access and resolves the file bytes internally;
/// the physical URL / storage key is never exposed to the caller.
/// </summary>
public sealed record DownloadProviderDocumentQuery(Guid Id)
    : IQuery<DownloadProviderDocumentResult>;
