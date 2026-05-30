// <copyright file="UpsertSeoMetadataCommandValidator.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.SeoMetadata.UpsertSeoMetadata;

using System.Text.RegularExpressions;
using FluentValidation;

public sealed class UpsertSeoMetadataCommandValidator : AbstractValidator<UpsertSeoMetadataCommand>
{
    private static readonly string[] AllowedChangeFrequencies =
        new[] { "always", "hourly", "daily", "weekly", "monthly", "yearly", "never" };

    private static readonly Regex CanonicalUrlRegex = new(
        "^(/[a-z0-9\\-/]+|https?://[^\\s]+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public UpsertSeoMetadataCommandValidator()
    {
        RuleFor(x => x.EntityType).IsInEnum();
        RuleFor(x => x.EntityId).NotEmpty();

        RuleFor(x => x.MetaTitle).MaximumLength(60)
            .When(x => !string.IsNullOrEmpty(x.MetaTitle))
            .WithErrorCode("SeoMetadata.MetaTitleTooLong");

        RuleFor(x => x.MetaDescription).MaximumLength(160)
            .When(x => !string.IsNullOrEmpty(x.MetaDescription))
            .WithErrorCode("SeoMetadata.MetaDescriptionTooLong");

        RuleFor(x => x.SitemapPriority)
            .InclusiveBetween(0.0m, 1.0m)
            .WithErrorCode("SeoMetadata.PriorityOutOfRange");

        RuleFor(x => x.SitemapChangeFrequency)
            .Must(f => f is null || AllowedChangeFrequencies.Contains(f.ToLowerInvariant()))
            .WithErrorCode("SeoMetadata.InvalidChangeFrequency")
            .WithMessage("SitemapChangeFrequency must be one of: always, hourly, daily, weekly, monthly, yearly, never.");

        RuleFor(x => x.CanonicalUrl)
            .Must(u => u is null || CanonicalUrlRegex.IsMatch(u))
            .WithErrorCode("SeoMetadata.CanonicalUrlInvalid")
            .WithMessage("CanonicalUrl must be a relative path (/...) or absolute URL.");
    }
}
