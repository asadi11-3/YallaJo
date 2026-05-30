// <copyright file="SeoMetadataChangedIntegrationEvent.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Contracts.IntegrationEvents;

using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

public sealed record SeoMetadataChangedIntegrationEvent(
    Guid SeoMetadataId,
    SeoEntityType EntityType,
    Guid EntityId,
    DateTime ChangedAt) : IntegrationEventBase;
