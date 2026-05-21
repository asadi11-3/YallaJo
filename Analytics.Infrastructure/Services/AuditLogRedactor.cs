using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Analytics.Application.Interfaces;

namespace Analytics.Infrastructure.Services;

internal sealed class AuditLogRedactor : IAuditLogRedactor
{
    private static readonly HashSet<string> Hard = new(StringComparer.OrdinalIgnoreCase) { "cardNumber", "cvv", "password", "webhookSignature" };
    private static readonly HashSet<string> Soft = new(StringComparer.OrdinalIgnoreCase) { "email", "phone", "ipAddress" };

    public (string? RedactedValue, IReadOnlyList<string> RedactedFields) Redact(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return (json, []);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var fields = new List<string>();
            var value = RedactElement(doc.RootElement, fields).GetRawText();
            return (value, fields);
        }
        catch (JsonException) { return (json, []); }
    }

    private static JsonElement RedactElement(JsonElement element, List<string> fields)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) Write(element, writer, fields);
        return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
    }

    private static void Write(JsonElement element, Utf8JsonWriter writer, List<string> fields)
    {
        if (element.ValueKind != JsonValueKind.Object) { element.WriteTo(writer); return; }
        writer.WriteStartObject();
        foreach (var p in element.EnumerateObject())
        {
            writer.WritePropertyName(p.Name);
            if (Hard.Contains(p.Name)) { writer.WriteStringValue("[REDACTED]"); fields.Add(p.Name); }
            else if (Soft.Contains(p.Name)) { writer.WriteStringValue(Mask(p.Name, p.Value.GetString())); fields.Add(p.Name); }
            else if (p.Value.ValueKind == JsonValueKind.Object) Write(p.Value, writer, fields);
            else p.Value.WriteTo(writer);
        }
        writer.WriteEndObject();
    }

    private static string? Mask(string name, string? value)
    {
        if (value is null) return null;
        if (name.Equals("email", StringComparison.OrdinalIgnoreCase)) return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16];
        if (name.Equals("phone", StringComparison.OrdinalIgnoreCase)) return value.Length <= 4 ? value : new string('*', value.Length - 4) + value[^4..];
        if (name.Equals("ipAddress", StringComparison.OrdinalIgnoreCase)) return value.Contains('.') ? string.Join('.', value.Split('.').Take(3)) + ".0" : value;
        return value;
    }
}
