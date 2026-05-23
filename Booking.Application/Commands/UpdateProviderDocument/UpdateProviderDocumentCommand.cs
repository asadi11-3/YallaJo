using Booking.Application.Commands.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.UpdateProviderDocument;

public sealed record UpdateProviderDocumentCommand(
    Guid Id,
    Stream? FileStream,
    string? FileName,
    string? ContentType,
    long? FileSize,
    DateOnly? ExpiresAt,
    byte[] RowVersion) : ICommand<ProviderDocumentDto>;
