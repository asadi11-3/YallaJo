using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.ViewModels;

public sealed class UserListVm
{
    public IReadOnlyList<UserRowVm> Users    { get; init; } = [];
    public int  Page        { get; init; } = 1;
    public int  PageSize    { get; init; } = 20;
    public int  TotalCount  { get; init; }
    public bool HasPrevious { get; init; }
    public bool HasNext     { get; init; }
}
