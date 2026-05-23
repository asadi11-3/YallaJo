using System;
using Booking.Domain.Enums;
using Microsoft.AspNetCore.Http;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.UploadProviderDocument;

public sealed record UploadProviderDocumentCommand(
    DocumentType DocumentType,
    IFormFile File,
    DateTime? ExpiresAt) : ICommand<Guid>;
