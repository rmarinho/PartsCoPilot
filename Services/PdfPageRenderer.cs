namespace PartsCopilot.Services;

/// <summary>
/// Renders PDF pages to PNG images using PDFtoImage (PDFium).
/// Works on macOS, Windows, and Linux. Returns null on platforms
/// where native PDFium binaries are unavailable (iOS, Android).
/// </summary>
public class PdfPageRenderer : IPdfPageRenderer
{
    private bool? _isSupported;

    public bool IsSupported
    {
        get
        {
            if (_isSupported.HasValue) return _isSupported.Value;
            try
            {
                var minimalPdf = CreateMinimalPdf();
                using var stream = new MemoryStream();
                PDFtoImage.Compatibility.Conversion.SavePng(stream, minimalPdf, null, 0, 72, 10, 10);
                _isSupported = true;
            }
            catch
            {
                _isSupported = false;
            }
            return _isSupported.Value;
        }
    }

    public async Task<byte[]?> RenderPageToImageAsync(string filePath, int pageNumber, int dpi = 150, CancellationToken ct = default)
    {
        if (!IsSupported) return null;

        try
        {
            return await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                var pdfBytes = File.ReadAllBytes(filePath);
                using var stream = new MemoryStream();
                PDFtoImage.Compatibility.Conversion.SavePng(stream, pdfBytes, null, pageNumber - 1, dpi);
                return (byte[]?)stream.ToArray();
            }, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            return null;
        }
    }

    private static byte[] CreateMinimalPdf()
    {
        var pdf = "%PDF-1.0\n1 0 obj<</Pages 2 0 R>>endobj\n2 0 obj<</Kids[3 0 R]/Count 1>>endobj\n3 0 obj<</MediaBox[0 0 1 1]>>endobj\ntrailer<</Root 1 0 R>>";
        return System.Text.Encoding.ASCII.GetBytes(pdf);
    }
}
