using Booking.Application.Commands.Common;
using Booking.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.UploadProviderDocument;

public sealed record UploadProviderDocumentCommand(
    DocumentType Type,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize,
    DateOnly? ExpiresAt) : ICommand<ProviderDocumentDto>;
