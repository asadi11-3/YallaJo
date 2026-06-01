namespace YallaJo.SharedKernel.Application.Common;

public static class RowVersionUtil
{
    public static bool Equal(byte[]? a, byte[]? b)
        => a is not null && b is not null && a.SequenceEqual(b);
}
