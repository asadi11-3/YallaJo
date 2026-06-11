using YallaJo.Web.Areas.Creator.Models.Dashboard;

namespace YallaJo.Web.Areas.Creator.Models.Profile;

/// <summary>
/// Pure mapping for the creator profile page: backend read DTO → view model, and
/// edit form → outbound request body. Dependency-free so it can be unit-tested
/// without a host.
/// </summary>
public static class CreatorProfileMapper
{
    /// <summary>Builds the profile view model from the current profile (null = no profile).</summary>
    public static CreatorProfileVm ToVm(CreatorProfileMineResponse? profile)
    {
        if (profile is null)
            return new CreatorProfileVm();

        return new CreatorProfileVm
        {
            ProfileId     = profile.Id,
            Status        = profile.Status,
            TrustTier     = profile.TrustTier,
            CurrentSlug   = profile.Slug,
            ArticleCount  = profile.ArticleCount,
            FollowerCount = profile.FollowerCount,
            DisplayName   = profile.DisplayName,
            Bio           = profile.Bio,
            AvatarUrl     = profile.AvatarUrl,
            // NewSlug is left null: the form pre-fills nothing so an unchanged
            // submission does not attempt a slug change.
            NewSlug       = null,
        };
    }

    /// <summary>
    /// Edit form → PUT /profile/mine body. A blank <see cref="CreatorProfileVm.NewSlug"/>
    /// is sent as null so the backend keeps the existing slug; a blank avatar is sent
    /// as null (cleared) only when the user explicitly empties it.
    /// </summary>
    public static UpdateCreatorProfileRequestBody ToUpdateBody(CreatorProfileVm form)
        => new(
            DisplayName: form.DisplayName.Trim(),
            Bio: Trimmed(form.Bio),
            AvatarUrl: Trimmed(form.AvatarUrl),
            NewSlug: Trimmed(form.NewSlug));

    public static UpdateCreatorAvatarRequestBody ToAvatarBody(UpdateAvatarVm form)
        => new(form.AvatarUrl.Trim());

    private static string? Trimmed(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
