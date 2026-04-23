namespace Auth.Infrastructure.Services;

public sealed class InviteOptions
{
    public const string SectionName = "Invite";

    public string AcceptUrlTemplate { get; set; } =
        "/auth/accept-invite?email={email}&token={token}";
}
