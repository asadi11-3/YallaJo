using FluentAssertions;
using YallaJo.SharedKernel.Application.Common;

namespace SharedKernel.Tests.Unit;

/// <summary>
/// Pins the behavior of the shared <see cref="RowVersionUtil"/> after it was
/// consolidated from the duplicated module-local copies (ContentTours/ContentBlogs).
/// Behavior must remain: non-null + same length + identical bytes => true; any null => false.
/// </summary>
public class RowVersionUtilTests
{
    [Fact]
    public void Equal_ReturnsTrue_WhenBothArraysHaveIdenticalBytes()
    {
        var a = new byte[] { 1, 2, 3, 4 };
        var b = new byte[] { 1, 2, 3, 4 };

        RowVersionUtil.Equal(a, b).Should().BeTrue();
    }

    [Fact]
    public void Equal_ReturnsTrue_WhenComparingSameReference()
    {
        var a = new byte[] { 9, 8, 7 };

        RowVersionUtil.Equal(a, a).Should().BeTrue();
    }

    [Fact]
    public void Equal_ReturnsFalse_WhenBytesDiffer()
    {
        var a = new byte[] { 1, 2, 3, 4 };
        var b = new byte[] { 1, 2, 3, 5 };

        RowVersionUtil.Equal(a, b).Should().BeFalse();
    }

    [Fact]
    public void Equal_ReturnsFalse_WhenLengthsDiffer()
    {
        var a = new byte[] { 1, 2, 3 };
        var b = new byte[] { 1, 2, 3, 4 };

        RowVersionUtil.Equal(a, b).Should().BeFalse();
    }

    [Fact]
    public void Equal_ReturnsFalse_WhenBothNull()
    {
        RowVersionUtil.Equal(null, null).Should().BeFalse();
    }

    [Fact]
    public void Equal_ReturnsFalse_WhenFirstNull()
    {
        RowVersionUtil.Equal(null, new byte[] { 1 }).Should().BeFalse();
    }

    [Fact]
    public void Equal_ReturnsFalse_WhenSecondNull()
    {
        RowVersionUtil.Equal(new byte[] { 1 }, null).Should().BeFalse();
    }

    [Fact]
    public void Equal_ReturnsTrue_WhenBothEmptyAndNonNull()
    {
        RowVersionUtil.Equal(Array.Empty<byte>(), Array.Empty<byte>()).Should().BeTrue();
    }
}
