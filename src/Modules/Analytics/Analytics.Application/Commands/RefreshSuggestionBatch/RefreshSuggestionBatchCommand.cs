using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.RefreshSuggestionBatch;

public sealed record RefreshSuggestionBatchCommand(
    EntityType SourceKind,
    Guid SourceId,
    SuggestionContext Context) : ICommand;
