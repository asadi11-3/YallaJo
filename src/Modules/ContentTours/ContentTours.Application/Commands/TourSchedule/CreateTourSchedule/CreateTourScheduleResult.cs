namespace ContentTours.Application.Commands.TourSchedule.CreateTourSchedule;

/// <summary>Result returned after a recurrence expansion. Tells the caller how many rows were created vs skipped (idempotency).</summary>
public sealed record CreateTourScheduleResult(int Created, int Skipped);
