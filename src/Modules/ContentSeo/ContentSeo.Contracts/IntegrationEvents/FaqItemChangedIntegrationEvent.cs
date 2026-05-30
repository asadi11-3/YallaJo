// <copyright file="FaqItemChangedIntegrationEvent.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Contracts.IntegrationEvents;

using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

public sealed record FaqItemChangedIntegrationEvent(
    Guid FaqItemId,
    SeoEntityType EntityType,
    Guid EntityId,
    string ChangeType,
    DateTime ChangedAt) : IntegrationEventBase;
