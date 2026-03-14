using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Tag.ListTags;

public sealed record TagDto(Guid Id, string Name, string Slug, bool IsActive);

public sealed record ListTagsQuery(bool ActiveOnly = false) : IQuery<IReadOnlyList<TagDto>>;
