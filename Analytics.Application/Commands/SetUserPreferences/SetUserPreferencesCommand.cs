using Analytics.Application.Queries.GetUserPreferences;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.SetUserPreferences;

public sealed record SetUserPreferencesCommand(
    Guid UserId,
    string? BudgetTier,
    bool IsFamilyTraveler,
    string? CurrentTripStage,
    IReadOnlyList<SetUserPreferredCategory> Categories) : ICommand<UserPreferencesResponse>;

public sealed record SetUserPreferredCategory(Guid CategoryId, decimal PreferenceScore);
