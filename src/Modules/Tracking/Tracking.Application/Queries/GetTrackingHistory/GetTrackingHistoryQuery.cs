using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tracking.Application.Common;
using Tracking.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Tracking.Application.Queries.GetTrackingHistory;

public sealed record GetTrackingHistoryQuery(
    Guid UserId,
    SessionStatus[]? Statuses,
    string? Cursor,
    int PageSize) : IQuery<TrackingHistoryPage>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;
    public const int MinPageSize = 1;

    public int EffectivePageSize =>
        PageSize <= 0
            ? DefaultPageSize
            : Math.Clamp(PageSize, MinPageSize, MaxPageSize);

    public string Fingerprint()
    {
        var builder = new StringBuilder(capacity: 128);
        builder.Append("ps=").Append(EffectivePageSize.ToString(CultureInfo.InvariantCulture));
        builder.Append(";cur=").Append(Cursor ?? string.Empty);

        if (Statuses is { Length: > 0 })
        {
            builder.Append(";st=");
            var ordered = Statuses
                .Select(static s => (int)s)
                .Distinct()
                .OrderBy(static s => s);
            builder.Append(string.Join(',', ordered));
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash, 0, 8);
    }
}
