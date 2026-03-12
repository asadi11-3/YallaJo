namespace ContentCore.Application.Interfaces;

public interface IImageProcessingService
{
    Task<ImageProcessingResult?> ProcessAsync(string filePath, CancellationToken ct = default);
}