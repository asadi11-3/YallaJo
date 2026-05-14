// <copyright file="DeleteRedirectCommand.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.Redirect.DeleteRedirect;

using YallaJo.SharedKernel.Application.Abstractions.Messaging;

public sealed record DeleteRedirectCommand(Guid Id) : ICommand;
