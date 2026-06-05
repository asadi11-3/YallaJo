// <copyright file="SeoFaqResponse.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace YallaJo.Web.Areas.Admin.Models.SeoFaq;

public enum SeoEntityType : byte
{
    Place = 0,
    Tour = 1,
    Business = 2,
    Blog = 3,
    TourGuide = 4,
    Creator = 5,
}

public sealed class PaginatedFaqResponse
{
    public IReadOnlyList<FaqItemResponse> Items { get; set; } = [];

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }
}

public sealed class FaqItemResponse
{
    public Guid Id { get; set; }

    public SeoEntityType EntityType { get; set; }

    public Guid EntityId { get; set; }

    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }
}

public sealed class CreateFaqItemResponse
{
    public Guid Id { get; set; }
}
