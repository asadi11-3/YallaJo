using ContentCore.Application.Queries.Tag.ListTags;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Tag.GetTagById;

public sealed record GetTagByIdQuery(Guid Id) : IQuery<TagDto>;
