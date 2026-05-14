// <copyright file="UpdateSeoMetadataCommandValidator.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.SeoMetadata.UpdateSeoMetadata;

using System.Text.RegularExpressions;
using FluentValidation;

public sealed class UpdateSeoMetadataCommandValidator : AbstractValidator<UpdateSeoMetadataCommand>
{
    private static readonly string[] AllowedChangeFrequencies =
        new[] { "always", "hourly", "daily", "weekly", "monthly", "yearly", "never" };

    private static readonly Regex CanonicalUrlRegex = new(
        "^(/[a-z0-9\\-/]+|https?://[^\\s]+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public UpdateSeoMetadataCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

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
            .WithErrorCode("SeoMetadata.InvalidChangeFrequency");

        RuleFor(x => x.CanonicalUrl)
            .Must(u => u is null || CanonicalUrlRegex.IsMatch(u))
            .WithErrorCode("SeoMetadata.CanonicalUrlInvalid");
    }
}
