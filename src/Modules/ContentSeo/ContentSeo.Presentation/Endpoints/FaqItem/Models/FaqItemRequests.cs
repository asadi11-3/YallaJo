// <copyright file="FaqItemRequests.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Presentation.Endpoints.FaqItem.Models;

using ContentSeo.Domain.Enums;

public sealed record CreateFaqItemRequest(
    SeoEntityType EntityType,
    Guid EntityId,
    string Question,
    string Answer,
    int SortOrder = 0,
    string? SourceLanguageCode = null);

public sealed record UpdateFaqItemRequest(string Question, string Answer);

public sealed record ReorderFaqItemsRequest(
    SeoEntityType EntityType,
    Guid EntityId,
    IReadOnlyList<ReorderFaqItem> Items);

public sealed record ReorderFaqItem(Guid Id, int SortOrder);
