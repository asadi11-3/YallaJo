// <copyright file="DeleteSeoMetadataCommand.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.SeoMetadata.DeleteSeoMetadata;

using YallaJo.SharedKernel.Application.Abstractions.Messaging;

/// <summary>
/// Soft-deletes a SEO metadata record by its surrogate id.
/// Idempotent at the handler boundary: a missing or already-deleted record returns NotFound.
/// </summary>
public sealed record DeleteSeoMetadataCommand(Guid Id) : ICommand;
