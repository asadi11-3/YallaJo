namespace ContentSeo.Application.Commands.Weather.ResetWeatherBudget;

using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Clock;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class ResetWeatherBudgetCommandHandler(
    IWeatherDailyBudgetRepository weatherDailyBudgetRepository,
    IContentSeoUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ILogger<ResetWeatherBudgetCommandHandler> logger)
    : ICommandHandler<ResetWeatherBudgetCommand>
{
    public async Task<Result> Handle(ResetWeatherBudgetCommand request, CancellationToken ct)
    {
        try
        {
            var targetDate = request.Date ?? DateOnly.FromDateTime(clock.UtcNow);
            var budget = await weatherDailyBudgetRepository.GetByDateAsync(targetDate, ct);
            if (budget is null)
            {
                return Result.Failure(new Error("WeatherBudget.NotFound", $"Weather budget for {targetDate:yyyy-MM-dd} not found."), Outcome.NotFound);
            }

            budget.Reset(clock.UtcNow);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(new Error("WeatherBudget.ConcurrencyConflict", "Weather budget was modified concurrently."), Outcome.Conflict);
            }

            logger.LogInformation("Reset weather budget for {BudgetDate}", targetDate);
            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
