namespace YallaJo.Web.Areas.Auth.Features.Sessions.ViewModels;

public sealed class SessionsVm
{
    public IReadOnlyList<SessionItemVm> Sessions { get; init; } = [];
}
