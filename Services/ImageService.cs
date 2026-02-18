using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using Microsoft.AspNetCore.Components.Forms;

namespace CSE325FinalProject.Services;

public interface IImageService
{
    Task<string> ProcessAndSaveAvatarAsync(IBrowserFile file, string webRootPath);
}

public class ImageService : IImageService
{
    private const int MaxWidth = 500;
    private const int MaxHeight = 500;
    private const int Quality = 75;

    public async Task<string> ProcessAndSaveAvatarAsync(IBrowserFile file, string webRootPath)
    {
        try
        {
            // Create uploads directory if it doesn't exist
            var uploadsFolder = Path.Combine(webRootPath, "uploads", "avatars");
            Directory.CreateDirectory(uploadsFolder);

            // Generate unique filename with .webp extension
            var fileName = $"{Guid.NewGuid()}.webp";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Load image from stream
            using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB max load
            using var image = await Image.LoadAsync(stream);

            // Resize if needed (Maintain aspect ratio)
            if (image.Width > MaxWidth || image.Height > MaxHeight)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(MaxWidth, MaxHeight),
                    Mode = ResizeMode.Max
                }));
            }

            // Save as WebP
            await image.SaveAsWebpAsync(filePath, new WebpEncoder { Quality = Quality });

            return $"/uploads/avatars/{fileName}";
        }
        catch (Exception ex)
        {
            throw new Exception($"Image processing failed: {ex.Message}", ex);
        }
    }
}
