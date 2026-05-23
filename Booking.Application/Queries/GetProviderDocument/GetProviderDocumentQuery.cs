using System;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetProviderDocument;

public sealed record GetProviderDocumentQuery(Guid DocumentId) : IQuery<ProviderDocumentDto>;
