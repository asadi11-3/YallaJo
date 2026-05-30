using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Application.Commands.Sitemap.DeleteSitemapEntry;

public sealed record DeleteSitemapEntryCommand(Guid Id) : ICommand;
