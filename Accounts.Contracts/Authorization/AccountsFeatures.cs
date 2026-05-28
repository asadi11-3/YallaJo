namespace Accounts.Contracts.Authorization;

public static class AccountsFeatures
{
    public const string Profile             = nameof(Profile);
    public const string ProviderApplication  = nameof(ProviderApplication);
    public const string AdminProviderQueue   = nameof(AdminProviderQueue);

    // Agency roster features
    public const string AgencyRoster        = nameof(AgencyRoster);
    public const string GuideAgency         = nameof(GuideAgency);
    public const string ProviderDashboard   = nameof(ProviderDashboard);
}
