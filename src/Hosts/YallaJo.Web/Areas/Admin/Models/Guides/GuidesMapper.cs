namespace YallaJo.Web.Areas.Admin.Models.Guides;

public static class GuidesMapper
{
    public static GuidesVm ToVm(TourGuideProfileResponse? detail, string? userEmail = null)
    {
        var vm = new GuidesVm();

        if (detail is not null)
        {
            vm.LookupId = detail.Id;
            vm.Detail = new TourGuideProfileVm
            {
                Id = detail.Id,
                UserId = detail.UserId,
                UserEmail = userEmail,
                DisplayName = detail.DisplayName,
                AvatarUrl = detail.AvatarUrl,
                Bio = detail.Bio,
                YearsOfExperience = detail.YearsOfExperience,
                HasFirstAid = detail.HasFirstAid,
                MoTALicenseNumber = detail.MoTALicenseNumber,
                AverageRating = detail.AverageRating,
                ReviewCount = detail.ReviewCount,
                TourCount = detail.TourCount,
                Languages = detail.Languages
                    .Select(l => l.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n!)
                    .ToList(),
                Specializations = detail.Specializations
                    .Select(s => s.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n!)
                    .ToList(),
            };
        }

        return vm;
    }
}
