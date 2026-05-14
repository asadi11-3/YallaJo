// <copyright file="CreateRedirectCommand.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.Redirect.CreateRedirect;

using YallaJo.SharedKernel.Application.Abstractions.Messaging;

public sealed record CreateRedirectCommand(string OldUrl, string NewUrl, int StatusCode)
    : ICommand<CreateRedirectResult>;

public sealed record CreateRedirectResult(Guid Id, string FinalTarget, int HopsCollapsed);
