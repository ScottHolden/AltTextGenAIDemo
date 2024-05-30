using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace AltTextGenAi;

internal static class ImageResize
{
    public static ContentPayload ResizeToJpeg(ReadOnlyMemory<byte> imageData, int width, int height)
    {
        using var image = Image.Load(imageData.Span);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Max
        }));

        // Find the smallest supported format

        using var pngOutput = new MemoryStream();
        image.SaveAsPng(pngOutput);

        using var jpegOutput = new MemoryStream();
        image.SaveAsJpeg(jpegOutput);

        return pngOutput.Length < jpegOutput.Length
            ? new ContentPayload(pngOutput.ToArray(), "image/png")
            : new ContentPayload(jpegOutput.ToArray(), "image/jpeg");
    }
}
