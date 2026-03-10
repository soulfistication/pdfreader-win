using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Melville.Pdf.Model.Documents;
using Melville.Pdf.SkiaSharp;
using SkiaSharp;

namespace PdfReader;

/// <summary>
/// Loads PDFs and renders pages to bitmaps at custom pixel dimensions, with optional crop.
/// </summary>
public sealed class PdfRenderService
{
    private PdfDocument? _document;
    private string? _currentPath;

    public int PageCount => _document?.TotalPages ?? 0;
    public bool HasDocument => _document != null;

    public async Task LoadAsync(string filePath)
    {
        _document = await new Melville.Pdf.Model.PdfReader().ReadFromFileAsync(filePath);
        _currentPath = filePath;
    }

    /// <summary>
    /// Renders a page (1-based index) to a WPF BitmapSource for display.
    /// </summary>
    public async Task<BitmapSource?> RenderPageForDisplayAsync(int oneBasedPageIndex)
    {
        if (_document == null || oneBasedPageIndex < 1 || oneBasedPageIndex > _document.TotalPages)
            return null;

        await using var mem = new MemoryStream();
        await RenderWithSkia.ToPngStreamAsync(_document, oneBasedPageIndex, mem);
        mem.Position = 0;

        using var skBitmap = SKBitmap.Decode(mem);
        if (skBitmap == null) return null;

        return ToBitmapSource(skBitmap);
    }

    /// <summary>
    /// Renders the given page at exact output width and height in pixels.
    /// If cropW/cropH are &gt; 0, only that region (in page coordinates, 72 DPI) is used and scaled to outputWidth x outputHeight.
    /// </summary>
    public async Task<BitmapSource?> RenderPageToBitmapAsync(
        int oneBasedPageIndex,
        int outputWidth,
        int outputHeight,
        double cropX = 0,
        double cropY = 0,
        double cropW = 0,
        double cropH = 0)
    {
        if (_document == null || oneBasedPageIndex < 1 || oneBasedPageIndex > _document.TotalPages)
            return null;
        if (outputWidth <= 0 || outputHeight <= 0) return null;

        await using var mem = new MemoryStream();
        await RenderWithSkia.ToPngStreamAsync(_document, oneBasedPageIndex, mem);
        mem.Position = 0;

        using var sourceBitmap = SKBitmap.Decode(mem);
        if (sourceBitmap == null) return null;

        var useCrop = cropW > 0 && cropH > 0;
        double srcW = sourceBitmap.Width;
        double srcH = sourceBitmap.Height;

        if (useCrop)
        {
            // Crop region in pixels of the default-rendered page
            int x = (int)Math.Max(0, Math.Min(cropX, srcW - 1));
            int y = (int)Math.Max(0, Math.Min(cropY, srcH - 1));
            int w = (int)Math.Max(1, Math.Min(cropW, srcW - x));
            int h = (int)Math.Max(1, Math.Min(cropH, srcH - y));

            using var cropped = new SKBitmap(w, h, sourceBitmap.ColorType, sourceBitmap.AlphaType);
            if (!sourceBitmap.ExtractSubset(cropped, new SKRectI(x, y, x + w, y + h)))
            {
                using var resized = ResizeBitmap(sourceBitmap, outputWidth, outputHeight);
                return ToBitmapSource(resized);
            }
            using var resizedCropped = ResizeBitmap(cropped, outputWidth, outputHeight);
            return ToBitmapSource(resizedCropped);
        }

        using var resizedFull = ResizeBitmap(sourceBitmap, outputWidth, outputHeight);
        return ToBitmapSource(resizedFull);
    }

    /// <summary>
    /// Renders the page to a PNG file at the specified pixel dimensions.
    /// </summary>
    public async Task ExportPageAsync(
        int oneBasedPageIndex,
        string outputPath,
        int widthPx,
        int heightPx,
        double cropX = 0,
        double cropY = 0,
        double cropW = 0,
        double cropH = 0)
    {
        var bmp = await RenderPageToBitmapAsync(oneBasedPageIndex, widthPx, heightPx, cropX, cropY, cropW, cropH);
        if (bmp == null) return;

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        using var stream = File.Create(outputPath);
        encoder.Save(stream);
    }

    private static SKBitmap ResizeBitmap(SKBitmap source, int targetWidth, int targetHeight)
    {
        var result = new SKBitmap(targetWidth, targetHeight, source.ColorType, source.AlphaType);
        using (var canvas = new SKCanvas(result))
        {
            canvas.Clear(SKColors.White);
            canvas.SetSamplingOptions(SKFilterQuality.High);
            var dest = new SKRect(0, 0, targetWidth, targetHeight);
            canvas.DrawBitmap(source, dest);
        }
        return result;
    }

    private static BitmapSource ToBitmapSource(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = data.AsStream();
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        return decoder.Frames[0];
    }
}
