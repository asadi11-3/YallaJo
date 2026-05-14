// <copyright file="RedirectCreatedIntegrationEvent.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Contracts.IntegrationEvents;

using YallaJo.SharedKernel.Domain.Event;

public sealed record RedirectCreatedIntegrationEvent(
    Guid RedirectId,
    string OldUrl,
    string NewUrl,
    int StatusCode,
    DateTime CreatedAt) : IntegrationEventBase;
