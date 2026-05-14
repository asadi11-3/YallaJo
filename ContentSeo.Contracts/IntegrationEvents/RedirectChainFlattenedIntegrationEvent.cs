// <copyright file="RedirectChainFlattenedIntegrationEvent.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Contracts.IntegrationEvents;

using YallaJo.SharedKernel.Domain.Event;

public sealed record RedirectChainFlattenedIntegrationEvent(
    Guid RedirectId,
    string OldUrl,
    string OldTarget,
    string NewTarget,
    DateTime FlattenedAt) : IntegrationEventBase;
