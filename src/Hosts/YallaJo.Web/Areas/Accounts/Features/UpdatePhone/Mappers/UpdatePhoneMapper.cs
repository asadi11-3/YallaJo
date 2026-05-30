using YallaJo.Web.Areas.Accounts.Features.UpdatePhone.Requests;
using YallaJo.Web.Areas.Accounts.Features.UpdatePhone.ViewModels;

namespace YallaJo.Web.Areas.Accounts.Features.UpdatePhone.Mappers;

public static class UpdatePhoneMapper
{
    public static UpdatePrimaryPhoneRequest ToRequest(UpdatePhoneVm vm) =>
        new(vm.PhoneNumber.Trim());
}
