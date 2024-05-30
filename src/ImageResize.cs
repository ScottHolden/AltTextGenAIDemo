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
        using var output = new MemoryStream();
        image.SaveAsJpeg(output);
        return ContentPayload.FromPng(output.ToArray());
    }
}
