using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Contracts.Abstractions;

public interface IProfileReassignmentService
{
    Task<Result<ProfileReassignmentOutcome>> ResetForReassignmentAsync(
        ProfileReassignmentRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ProfileReassignmentRequest(
    Guid UserId,
    string NewEmail);

public sealed record ProfileReassignmentOutcome(bool Scrubbed);
