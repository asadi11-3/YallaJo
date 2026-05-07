using YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places.Requests;
using YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places.Responses;
using YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places.Mappers;

public static class PlacesMapper
{
    // ── List ─────────────────────────────────────────────────────────────────
    public static PlaceRowVm ToRow(PlaceSummaryResponse r) => new()
    {
        Id            = r.Id,
        Name          = r.Name,
        Slug          = r.Slug,
        PlaceType     = r.PlaceType,
        City          = r.City,
        Country       = r.Country,
        AverageRating = r.AverageRating,
        ReviewCount   = r.ReviewCount,
        IsFeatured    = r.IsFeatured,
        IsVerified    = r.IsVerified,
    };

    // ── Details ──────────────────────────────────────────────────────────────
    public static PlaceDetailsVm ToDetails(PlaceDetailsResponse r) => new()
    {
        Id                     = r.Id,
        Name                   = r.Name,
        Slug                   = r.Slug,
        PlaceType              = r.PlaceType,
        Latitude               = r.Latitude,
        Longitude              = r.Longitude,
        Description            = r.Description,
        Address                = r.Address,
        City                   = r.City,
        Country                = r.Country,
        PostalCode             = r.PostalCode,
        Phone                  = r.Phone,
        Email                  = r.Email,
        Website                = r.Website,
        AverageRating          = r.AverageRating,
        ReviewCount            = r.ReviewCount,
        IsFeatured             = r.IsFeatured,
        IsVerified             = r.IsVerified,
        IsWheelchairAccessible = r.IsWheelchairAccessible,
        HasAudioGuide          = r.HasAudioGuide,
        HasBrailleSignage      = r.HasBrailleSignage,
        MetaTitle              = r.MetaTitle,
        MetaDescription        = r.MetaDescription,
        Translations           = r.Translations
            .Select(t => new PlaceTranslationVm
            {
                LanguageId  = t.LanguageId,
                Name        = t.Name,
                Description = t.Description,
                Address     = t.Address,
            })
            .ToList(),
    };

    // ── Edit form preload ────────────────────────────────────────────────────
    public static EditPlaceVm ToEditVm(PlaceDetailsResponse r) => new()
    {
        Id              = r.Id,
        Name            = r.Name,
        Slug            = r.Slug,
        PlaceType       = r.PlaceType,
        Latitude        = r.Latitude,
        Longitude       = r.Longitude,
        Description     = r.Description,
        Address         = r.Address,
        City            = r.City,
        Country         = r.Country,
        PostalCode      = r.PostalCode,
        Phone           = r.Phone,
        Email           = r.Email,
        Website         = r.Website,
        MetaTitle       = r.MetaTitle,
        MetaDescription = r.MetaDescription,
    };

    // ── VM → wire request ────────────────────────────────────────────────────
    public static CreatePlaceRequest ToCreateRequest(CreatePlaceVm vm) => new(
        Name:            vm.Name.Trim(),
        Slug:            string.IsNullOrWhiteSpace(vm.Slug) ? null : vm.Slug!.Trim(),
        PlaceType:       vm.PlaceType,
        Latitude:        vm.Latitude,
        Longitude:       vm.Longitude,
        Description:     NullIfBlank(vm.Description),
        Address:         NullIfBlank(vm.Address),
        City:            NullIfBlank(vm.City),
        Country:         NullIfBlank(vm.Country),
        PostalCode:      NullIfBlank(vm.PostalCode),
        Phone:           NullIfBlank(vm.Phone),
        Email:           NullIfBlank(vm.Email),
        Website:         NullIfBlank(vm.Website),
        MetaTitle:       NullIfBlank(vm.MetaTitle),
        MetaDescription: NullIfBlank(vm.MetaDescription));

    public static UpdatePlaceRequest ToUpdateRequest(EditPlaceVm vm) => new(
        Name:            vm.Name.Trim(),
        Slug:            vm.Slug.Trim(),
        PlaceType:       vm.PlaceType,
        Latitude:        vm.Latitude,
        Longitude:       vm.Longitude,
        Description:     NullIfBlank(vm.Description),
        Address:         NullIfBlank(vm.Address),
        City:            NullIfBlank(vm.City),
        Country:         NullIfBlank(vm.Country),
        PostalCode:      NullIfBlank(vm.PostalCode),
        Phone:           NullIfBlank(vm.Phone),
        Email:           NullIfBlank(vm.Email),
        Website:         NullIfBlank(vm.Website),
        MetaTitle:       NullIfBlank(vm.MetaTitle),
        MetaDescription: NullIfBlank(vm.MetaDescription));

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
