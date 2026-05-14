// <copyright file="CreateFaqItemCommand.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.FaqItem.CreateFaqItem;

using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

public sealed record CreateFaqItemCommand(
    SeoEntityType EntityType,
    Guid EntityId,
    string Question,
    string Answer,
    int SortOrder,
    string? SourceLanguageCode = null)
    : ICommand<CreateFaqItemResult>;

public sealed record CreateFaqItemResult(Guid Id);
