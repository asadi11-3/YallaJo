using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Application.Commands.Sitemap.UpdateSitemapEntry;

public sealed record UpdateSitemapEntryCommand(
    Guid Id,
    decimal? Priority,
    string? ChangeFrequency) : ICommand;
