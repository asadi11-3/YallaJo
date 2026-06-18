using System.Text;

namespace YallaJo.Web.Areas.Provider.Models.ProviderDocuments;

public static class ProviderDocumentsMapper
{
    public static ProviderDocumentRowVm ToRowVm(ProviderDocumentResponse d) => new()
    {
        Id              = d.Id,
        Type            = d.Type,
        TypeLabel       = Humanize(d.Type.ToString()),
        FileName        = d.FileName,
        ExpiresAt       = d.ExpiresAt,
        Status          = d.Status,
        RejectionReason = d.RejectionReason,
        RowVersion      = d.RowVersion,
    };

    // "MoTALicense" → "Mo TA License" is ugly; keep acronyms intact by only splitting
    // lower→Upper boundaries.
    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var sb = new StringBuilder(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(value[i - 1])) sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
