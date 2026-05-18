// <copyright file="IWeatherDailyBudgetRepository.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

using ContentSeo.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentSeo.Domain.Repositories;

public interface IWeatherDailyBudgetRepository : IRepository<WeatherDailyBudget>
{
    /// <summary>Returns the budget row for the given UTC date (tracked, for mutation).</summary>
    Task<WeatherDailyBudget?> GetByDateAsync(DateOnly date, CancellationToken ct = default);
}
