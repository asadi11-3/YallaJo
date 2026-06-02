using YallaJo.Web.Areas.Accounts.Models.UpdatePhone;
using YallaJo.Web.Areas.Accounts.Models.UpdatePhone;

namespace YallaJo.Web.Areas.Accounts.Models.UpdatePhone;

public static class UpdatePhoneMapper
{
    public static UpdatePrimaryPhoneRequest ToRequest(UpdatePhoneVm vm) =>
        new(vm.PhoneNumber.Trim());
}
