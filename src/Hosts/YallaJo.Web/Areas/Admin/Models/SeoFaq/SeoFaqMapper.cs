// <copyright file="SeoFaqMapper.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace YallaJo.Web.Areas.Admin.Models.SeoFaq;

public static class SeoFaqMapper
{
    public static SeoFaqVm ToVm(PaginatedFaqResponse page, FaqFilterRequest filter)
    {
        return new SeoFaqVm
        {
            Page = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
            EntityTypeFilter = filter.EntityType,
            EntityIdFilter = filter.EntityId,
            ActiveOnlyFilter = filter.ActiveOnly,
            Items = page.Items.Select(i => new FaqItemRowVm
            {
                Id = i.Id,
                EntityType = i.EntityType,
                EntityId = i.EntityId,
                Question = i.Question,
                Answer = i.Answer,
                SortOrder = i.SortOrder,
                IsActive = i.IsActive,
            }).ToList(),
        };
    }
}
