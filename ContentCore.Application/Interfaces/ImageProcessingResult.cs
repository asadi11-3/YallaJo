namespace ContentCore.Application.Interfaces;

public sealed record ImageProcessingResult(
    int Width,
    int Height,
    string? ThumbnailUrl
);