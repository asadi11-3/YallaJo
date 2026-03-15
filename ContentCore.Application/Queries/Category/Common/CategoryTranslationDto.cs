using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentCore.Application.Queries.Category.Common
{

    public sealed record CategoryTranslationDto(
        Guid LanguageId,
        string Name,
        string Slug);
}
