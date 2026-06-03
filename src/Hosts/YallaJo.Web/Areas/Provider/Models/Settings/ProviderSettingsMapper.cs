using YallaJo.Web.Areas.Provider.Models;

namespace YallaJo.Web.Areas.Provider.Models.Settings;

public static class ProviderSettingsMapper
{
    public static ProviderSettingsVm ToVm(ProviderSettingsResponse r) => new()
    {
        BusinessName      = r.BusinessName,
        ProviderTypeLabel = ProviderMapper.Humanize(r.ProviderType),
        ContactEmail      = r.ContactEmail,
        ContactPhone      = r.ContactPhone,
        Address           = r.Address,
        Description       = r.Description,
    };
}
