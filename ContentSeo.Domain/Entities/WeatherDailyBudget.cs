// <copyright file="WeatherDailyBudget.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

using YallaJo.SharedKernel.Domain.Entities;

namespace ContentSeo.Domain.Entities;

/// <summary>
/// Tracks the number of weather API calls made on a given UTC date.
/// PDF §11: max 1000 calls/day (free tier); must be persisted to survive restarts
/// and prevent multi-instance budget doubling.
/// </summary>
public sealed class WeatherDailyBudget : BaseEntity, IAggregateRoot
{
    private WeatherDailyBudget()
    {
    } // EF Core

    /// <summary>UTC date this budget row covers.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Number of API calls consumed today.</summary>
    public int CallsUsed { get; private set; }

    /// <summary>Maximum calls allowed per day (default 1000).</summary>
    public int DailyLimit { get; private set; }

    /// <summary>
    /// UTC timestamp when the budget-exhausted admin alert was last sent.
    /// Null = alert not yet sent today.
    /// </summary>
    public DateTime? AlertSentAt { get; private set; }

    /// <summary>Whether the daily budget has been exhausted.</summary>
    public bool IsExhausted => CallsUsed >= DailyLimit;

    /// <summary>Creates a new budget row for today.</summary>
    public static WeatherDailyBudget Create(DateOnly date, int dailyLimit = 1000)
    {
        if (dailyLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dailyLimit), "DailyLimit must be positive.");
        }

        return new WeatherDailyBudget
        {
            Id = Guid.CreateVersion7(),
            Date = date,
            CallsUsed = 0,
            DailyLimit = dailyLimit,
            AlertSentAt = null,
        };
    }

    /// <summary>
    /// Atomically increments the call counter.
    /// Returns <c>true</c> if the call is within budget; <c>false</c> if budget is exhausted.
    /// </summary>
    public bool TryConsume()
    {
        if (IsExhausted)
        {
            return false;
        }

        CallsUsed++;
        return true;
    }

    /// <summary>Records that the budget-exhausted alert has been sent.</summary>
    public void MarkAlertSent(DateTime utcNow)
    {
        AlertSentAt = utcNow;
    }

    public void Reset(DateTime utcNow)
    {
        CallsUsed = 0;
        AlertSentAt = null;
    }
}
