namespace ContentTours.Application.Common;

/// <summary>
/// Shared helper for comparing optimistic-concurrency RowVersion byte arrays.
/// Replaces the <c>RowVersionsEqual</c> private method that was previously
/// copy-pasted across every Tour command handler.
/// </summary>
internal static class RowVersionUtil
{
    /// <summary>
    /// Returns <c>true</c> when both arrays are non-null, the same length, and
    /// contain identical bytes in order. Two nulls return <c>false</c> — callers
    /// that pass a stale or missing RowVersion must be treated as a conflict.
    /// </summary>
    public static bool Equal(byte[]? a, byte[]? b)
        => a is not null && b is not null && a.SequenceEqual(b);
}
