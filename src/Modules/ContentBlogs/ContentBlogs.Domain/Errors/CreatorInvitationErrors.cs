using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Domain.Errors;

/// <summary>Domain errors for <see cref="Entities.Creators.CreatorInvitation"/>.</summary>
public static class CreatorInvitationErrors
{
    public static readonly Error NotFound =
        new("CreatorInvitation.NotFound", "Creator invitation was not found.");

    public static readonly Error AlreadyRedeemed =
        new("CreatorInvitation.AlreadyRedeemed", "This invitation has already been redeemed.");

    public static readonly Error Expired =
        new("CreatorInvitation.Expired", "This invitation has expired.");

    public static readonly Error Revoked =
        new("CreatorInvitation.Revoked", "This invitation has been revoked.");

    public static readonly Error NotPending =
        new("CreatorInvitation.NotPending", "Only pending invitations can be redeemed or revoked.");

    public static readonly Error InvalidToken =
        new("CreatorInvitation.InvalidToken", "The invitation token is invalid.");
}
