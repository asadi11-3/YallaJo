using Analytics.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Application.Interfaces.Repositories;

public interface IExperimentRepository : IRepository<Experiment, Guid>
{
    Task<Experiment?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Experiment>> GetRunningAsync(CancellationToken ct = default);
    Task<ExperimentAssignment?> GetAssignmentAsync(Guid userId, Guid experimentId, CancellationToken ct = default);
    Task AddAssignmentAsync(ExperimentAssignment assignment, CancellationToken ct = default);
}
