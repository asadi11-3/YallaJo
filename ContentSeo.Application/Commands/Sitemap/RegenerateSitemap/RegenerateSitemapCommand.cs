// <copyright file="RegenerateSitemapCommand.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.Sitemap.RegenerateSitemap;

using YallaJo.SharedKernel.Application.Abstractions.Messaging;

public sealed record RegenerateSitemapCommand : ICommand<RegenerateSitemapResult>;
