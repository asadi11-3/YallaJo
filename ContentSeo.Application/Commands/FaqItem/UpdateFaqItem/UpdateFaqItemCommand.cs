// <copyright file="UpdateFaqItemCommand.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.FaqItem.UpdateFaqItem;

using YallaJo.SharedKernel.Application.Abstractions.Messaging;

public sealed record UpdateFaqItemCommand(Guid Id, string Question, string Answer) : ICommand;
