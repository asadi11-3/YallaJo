namespace Auth.Application.Commands.ExternalLogin;
internal readonly record struct AutoLinkOutcome(
    AutoLinkRefusalReason Reason,
    AutoLinkPath Path,
    Guid? LinkedUserId,
    Guid? CandidateUserId,
    bool LocalEmailVerified)
{
    public static AutoLinkOutcome Linked(Guid userId, AutoLinkPath path) =>
        new(AutoLinkRefusalReason.None, path, userId, userId, LocalEmailVerified: true);

    public static AutoLinkOutcome Refused(
        AutoLinkRefusalReason reason,
        AutoLinkPath path,
        Guid? candidateUserId = null,
        bool localEmailVerified = false) =>
        new(reason, path, LinkedUserId: null, candidateUserId, localEmailVerified);
}
