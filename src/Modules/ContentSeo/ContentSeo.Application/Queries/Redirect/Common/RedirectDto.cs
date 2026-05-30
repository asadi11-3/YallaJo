// <copyright file="RedirectDto.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.Redirect.Common;

using DomainRedirect = ContentSeo.Domain.Entities.Redirect;

public sealed record RedirectDto(
    Guid Id,
    string OldUrl,
    string NewUrl,
    int StatusCode,
    bool IsActive,
    long HitCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static RedirectDto From(DomainRedirect entity) => new(
        entity.Id,
        entity.OldUrl,
        entity.NewUrl,
        entity.StatusCode,
        entity.IsActive,
        entity.HitCount,
        entity.CreatedAt,
        entity.UpdatedAt);
}
