using System;
using Microsoft.AspNetCore.Http;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Commands.UpdateProviderDocument;

public sealed record UpdateProviderDocumentCommand(
    Guid DocumentId,
    IFormFile? File,
    DateTime? ExpiresAt
) : ICommand;
