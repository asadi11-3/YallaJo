namespace YallaJo.Web.Areas.Business.Models.Services;

public static class ServicesMapper
{
    public static IReadOnlyList<ServiceRowVm> ToRows(IReadOnlyList<ServiceItemResponse> items) =>
        items
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .Select(i => new ServiceRowVm(i.Id, i.Name, i.Price, i.DurationMinutes, i.Currency))
            .ToList();
}
