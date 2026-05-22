using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

public sealed class ExperimentRepository(AnalyticsDbContext context)
    : EfRepository<Experiment, Guid>(context), IExperimentRepository
{
    public async Task<Experiment?> GetByNameAsync(string name, CancellationToken ct = default)
        => await context.Set<Experiment>().FirstOrDefaultAsync(e => e.Name == name, ct);

    public async Task<IReadOnlyList<Experiment>> GetRunningAsync(CancellationToken ct = default)
        => await context.Set<Experiment>()
            .Where(e => e.Status == "Running" && e.StartsAt <= DateTime.UtcNow && e.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

    public async Task<ExperimentAssignment?> GetAssignmentAsync(Guid userId, Guid experimentId, CancellationToken ct = default)
        => await context.Set<ExperimentAssignment>()
            .FirstOrDefaultAsync(a => a.UserId == userId && a.ExperimentId == experimentId, ct);

    public async Task AddAssignmentAsync(ExperimentAssignment assignment, CancellationToken ct = default)
        => await context.Set<ExperimentAssignment>().AddAsync(assignment, ct);
}
