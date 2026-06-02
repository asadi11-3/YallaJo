namespace YallaJo.Web.Areas.Auth.Models.Sessions;

public sealed class SessionsVm
{
    public IReadOnlyList<SessionItemVm> Sessions { get; init; } = [];
}
