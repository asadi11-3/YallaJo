using System;
using System.Collections.Generic;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetProviderDocuments;

public sealed record GetProviderDocumentsQuery(Guid ProviderId) : IQuery<IReadOnlyList<ProviderDocumentDto>>;
