using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.EntityTag.GetEntityTags;

public sealed record EntityTagDto(Guid TagId, string Name, string Slug);

public sealed record GetEntityTagsQuery(string EntityType, Guid EntityId)
    : IQuery<IReadOnlyList<EntityTagDto>>;
