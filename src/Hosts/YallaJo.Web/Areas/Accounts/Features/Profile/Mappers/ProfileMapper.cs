using YallaJo.Web.Areas.Accounts.Features.Profile.Requests;
using YallaJo.Web.Areas.Accounts.Features.Profile.Responses;
using YallaJo.Web.Areas.Accounts.Features.Profile.ViewModels;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.Features.Profile.Mappers;

public static class ProfileMapper
{
    public static ProfileVm ToVm(ProfileResponse r, IApiAssetUrlResolver? assetResolver = null) => new()
    {
        UserId       = r.UserId,
        FirstName    = r.FirstName,
        LastName     = r.LastName,
        DisplayName  = r.DisplayName,
        // Avatar URLs stored by the API are relative to the API origin
        // (e.g. "/uploads/avatars/<guid>.png"). The browser resolves a
        // relative <img src> against the WEB origin — where the file does
        // not exist — so resolve to an absolute API URL here.
        AvatarUrl    = assetResolver?.Resolve(r.AvatarUrl) ?? r.AvatarUrl,
        PhoneNumber  = r.PhoneNumber,
        DateOfBirth  = r.DateOfBirth,
        Gender       = r.Gender,
        Country      = r.Country,
        City         = r.City,
        AddressLine  = r.AddressLine,
        Email        = r.Email,
        Update = new UpdateProfileVm
        {
            FirstName   = r.FirstName,
            LastName    = r.LastName,
            DateOfBirth = r.DateOfBirth,
            Gender      = ParseGender(r.Gender),
            Country     = r.Country,
            City        = r.City,
            AddressLine = r.AddressLine,
        },
    };

    public static UpdateProfileRequest ToUpdateRequest(UpdateProfileVm vm) => new(
        FirstName:   vm.FirstName.Trim(),
        LastName:    vm.LastName.Trim(),
        DateOfBirth: vm.DateOfBirth,
        Gender:      vm.Gender.HasValue ? (int)vm.Gender.Value : null,
        Country:     string.IsNullOrWhiteSpace(vm.Country) ? null : vm.Country.Trim(),
        City:        string.IsNullOrWhiteSpace(vm.City) ? null : vm.City.Trim(),
        AddressLine: string.IsNullOrWhiteSpace(vm.AddressLine) ? null : vm.AddressLine.Trim());

    private static GenderOption? ParseGender(string? raw) =>
        Enum.TryParse<GenderOption>(raw, ignoreCase: true, out var g) ? g : null;
}
