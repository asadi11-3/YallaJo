using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace YallaJo.SharedKernel.Application.Abstractions.Outbox;

/// <summary>
/// Replays a dead-lettered outbox message by cloning it with a fresh Id and
/// zeroed RetryCount. The original dead-lettered row is preserved as an audit record.
/// </summary>
/// <param name="Module">The module's DbContext name (e.g. "SecurityDbContext").</param>
/// <param name="MessageId">The Id of the dead-lettered message to replay.</param>
public sealed record ReplayDeadLetterCommand(string Module, Guid MessageId) : ICommand;
