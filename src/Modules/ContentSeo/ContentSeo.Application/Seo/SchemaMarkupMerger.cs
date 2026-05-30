namespace ContentSeo.Application.Seo;

using System.Text.Json;
using System.Text.Json.Nodes;

public static class SchemaMarkupMerger
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    public static string MergeAggregateRating(
        string? existingSchemaMarkup,
        decimal averageRating,
        int reviewCount,
        int bestRating,
        int worstRating)
    {
        var aggregateRating = new JsonObject
        {
            ["@type"] = "AggregateRating",
            ["ratingValue"] = averageRating,
            ["reviewCount"] = reviewCount,
            ["bestRating"] = bestRating,
            ["worstRating"] = worstRating,
        };

        if (string.IsNullOrWhiteSpace(existingSchemaMarkup))
        {
            aggregateRating["@context"] = "https://schema.org";
            return aggregateRating.ToJsonString(Options);
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(existingSchemaMarkup);
        }
        catch (JsonException)
        {
            root = new JsonObject
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "Thing",
                ["rawSchema"] = existingSchemaMarkup,
            };
        }

        if (root is JsonObject obj)
        {
            obj["aggregateRating"] = aggregateRating;
            if (!obj.ContainsKey("@context"))
            {
                obj["@context"] = "https://schema.org";
            }

            return obj.ToJsonString(Options);
        }

        var wrapped = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = root,
            ["aggregateRating"] = aggregateRating,
        };
        return wrapped.ToJsonString(Options);
    }
}
