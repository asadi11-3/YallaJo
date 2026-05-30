// <copyright file="DeleteFaqItemCommand.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.FaqItem.DeleteFaqItem;

using YallaJo.SharedKernel.Application.Abstractions.Messaging;

public sealed record DeleteFaqItemCommand(Guid Id) : ICommand;
