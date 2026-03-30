using ContentCore.Presentation.Endpoints.Attachment;
using ContentCore.Presentation.Endpoints.Category;
using ContentCore.Presentation.Endpoints.EntityCategory;
using ContentCore.Presentation.Endpoints.EntityTag;
using ContentCore.Presentation.Endpoints.Language;
using ContentCore.Presentation.Endpoints.Specialization;
using ContentCore.Presentation.Endpoints.Tag;
using ContentCore.Presentation.Endpoints.Translation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ContentCore.Presentation;

public static class ContentCoreEndpoints
{
    public static IEndpointRouteBuilder MapContentCoreEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/content-core")
            .WithTags("ContentCore");

        LanguageEndpoints.MapLanguageEndpoints(group);
        TranslationEndpoints.MapTranslationEndpoints(group);
        CategoryEndpoints.MapCategoryEndpoints(group);
        AttachmentEndpoints.MapAttachmentEndpoints(group);
        TagEndpoints.MapTagEndpoints(group);
        EntityCategoryEndpoints.MapEntityCategoryEndpoints(group);
        EntityTagEndpoints.MapEntityTagEndpoints(group);
        SpecializationEndpoints.MapSpecializationEndpoints(group);

        return endpoints;
    }
}
