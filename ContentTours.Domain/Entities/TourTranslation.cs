using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Domain.Entities;

public sealed class TourTranslation : BaseEntity
{
    private TourTranslation() { } // EF Core

    public Guid TourId { get; private set; }
    public Guid LanguageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ShortDescription { get; private set; }
    public string? MeetingPoint { get; private set; }

    public Tour Tour { get; private set; } = default!;
}
