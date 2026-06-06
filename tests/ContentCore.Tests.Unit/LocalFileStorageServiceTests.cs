using ContentCore.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContentCore.Tests.Unit;

/// <summary>
/// Regression test for the FileSize=0 sub-bug surfaced as part of the staging
/// proxy Blocker #2 verification.
///
/// Root cause: <see cref="LocalFileStorageService"/> used to read
/// <c>new FileInfo(filePath).Length</c> while the underlying
/// <see cref="FileStream"/> still had its 81920-byte buffer un-flushed —
/// the bytes had been written to the buffer but not to disk, so
/// FileInfo.Length sampled <c>0</c>. The file itself reached disk fine once
/// the stream was disposed at method exit, but the DB row was already
/// persisted with FileSize=0 by then. The fix flushes the FileStream and
/// reads <c>fileStream.Length</c> BEFORE disposal.
/// </summary>
[Trait("Category", "blocker-fix")]
public sealed class LocalFileStorageServiceTests
{
    [Fact]
    public async Task UploadAsync_ReturnsFileSizeEqualToStreamLength_ForNonEmptyPayload()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "yallajo-storage-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var sut = BuildService(basePath);
            var payload = new byte[12345]; // anything > 0 will reproduce the original bug
            Random.Shared.NextBytes(payload);

            await using var stream = new MemoryStream(payload);
            var result = await sut.UploadAsync(stream, "fixture.bin", "application/octet-stream", "blogs");

            result.IsSuccess.Should().BeTrue();
            result.Value.FileSize.Should().Be(payload.Length,
                "FileInfo.Length must be sampled AFTER the FileStream's buffer is flushed; " +
                "the previous implementation returned 0 even for non-empty uploads because " +
                "the 81920-byte FileStream buffer had not been flushed at the sample point");

            // The file on disk should equal the payload exactly.
            var savedPath = Path.Combine(basePath, "blogs",
                Path.GetFileName(new Uri("http://x" + result.Value.Url).AbsolutePath));
            File.Exists(savedPath).Should().BeTrue();
            (await File.ReadAllBytesAsync(savedPath)).Should().Equal(payload);
        }
        finally
        {
            if (Directory.Exists(basePath))
                Directory.Delete(basePath, recursive: true);
        }
    }

    [Fact]
    public async Task UploadAsync_RejectsEmptyStream()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "yallajo-storage-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var sut = BuildService(basePath);
            await using var stream = new MemoryStream(Array.Empty<byte>());

            var result = await sut.UploadAsync(stream, "empty.bin", "application/octet-stream", "blogs");

            result.IsSuccess.Should().BeFalse(
                "empty payload guard must remain intact after the FileSize=0 regression fix");
        }
        finally
        {
            if (Directory.Exists(basePath))
                Directory.Delete(basePath, recursive: true);
        }
    }

    private static LocalFileStorageService BuildService(string basePath)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:BasePath"] = basePath,
                ["FileStorage:BaseUrl"]  = "/uploads",
            })
            .Build();
        return new LocalFileStorageService(config, NullLogger<LocalFileStorageService>.Instance);
    }
}
