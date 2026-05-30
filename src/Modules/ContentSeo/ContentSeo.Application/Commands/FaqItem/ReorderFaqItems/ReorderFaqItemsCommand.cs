// <copyright file="ReorderFaqItemsCommand.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.FaqItem.ReorderFaqItems;

using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

public sealed record ReorderFaqItemsCommand(
    SeoEntityType EntityType,
    Guid EntityId,
    IReadOnlyList<ReorderFaqItemDto> Items)
    : ICommand;

public sealed record ReorderFaqItemDto(Guid Id, int SortOrder);
