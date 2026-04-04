using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Queries.Place.Common
{
    public sealed record PlaceTranslationDto(
    Guid LanguageId,
    string Name,
    string? Description,
    string? Address);
}
