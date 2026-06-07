// <copyright file="SeoFaqRequest.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace YallaJo.Web.Areas.Admin.Models.SeoFaq;

public sealed class FaqFilterRequest
{
    public SeoEntityType? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    public bool? ActiveOnly { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;
}

public sealed record CreateFaqItemApiRequest(
    SeoEntityType EntityType,
    Guid EntityId,
    string Question,
    string Answer,
    int SortOrder);

public sealed record UpdateFaqItemApiRequest(string Question, string Answer);

// §8.10 — body for PUT /api/v1/seo/faq/reorder (batch reorder within one entity's FAQ list).
public sealed record ReorderFaqItemsApiRequest(
    SeoEntityType EntityType,
    Guid EntityId,
    IReadOnlyList<ReorderFaqItemApi> Items);

public sealed record ReorderFaqItemApi(Guid Id, int SortOrder);
