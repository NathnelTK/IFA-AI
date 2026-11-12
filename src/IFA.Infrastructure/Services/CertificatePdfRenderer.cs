using System.Text.Json;
using IFA.Application.Common.Interfaces;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using QRCoder;

namespace IFA.Infrastructure.Services
{
    public sealed class CertificatePdfRenderer : ICertificateRenderer
    {
        private static readonly object FontLock = new();
        private static bool _fontConfigured;

        public Task<Stream> RenderAsync(
            string snapshotJson,
            string serialNumber,
            string verificationUrl,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureFontResolver();
            using var snapshot = JsonDocument.Parse(snapshotJson);
            var root = snapshot.RootElement;
            var learnerName = root.GetProperty("learnerName").GetString() ?? "Learner";
            var courseTitle = root.GetProperty("courseTitle").GetString() ?? "Course";
            var completedAt = root.GetProperty("completedAt").GetDateTime();

            var document = new PdfDocument();
            var page = document.AddPage();
            page.Orientation = PdfSharp.PageOrientation.Landscape;
            page.Size = PdfSharp.PageSize.A4;
            using var graphics = XGraphics.FromPdfPage(page);
            graphics.DrawRectangle(new XPen(XColor.FromArgb(23, 107, 74), 4), 24, 24, page.Width.Point - 48, page.Height.Point - 48);
            graphics.DrawString("WITFORGE", new XFont("DejaVu Sans", 18, XFontStyleEx.Bold), new XSolidBrush(XColor.FromArgb(23, 107, 74)), new XRect(0, 55, page.Width.Point, 30), XStringFormats.Center);
            graphics.DrawString("Certificate of Completion", new XFont("DejaVu Sans", 30, XFontStyleEx.Bold), XBrushes.Black, new XRect(0, 105, page.Width.Point, 45), XStringFormats.Center);
            graphics.DrawString("This certifies that", new XFont("DejaVu Sans", 14), XBrushes.DimGray, new XRect(0, 175, page.Width.Point, 25), XStringFormats.Center);
            graphics.DrawString(learnerName, new XFont("DejaVu Sans", 26, XFontStyleEx.Bold), new XSolidBrush(XColor.FromArgb(23, 107, 74)), new XRect(70, 210, page.Width.Point - 140, 40), XStringFormats.Center);
            graphics.DrawString("completed the immutable course release", new XFont("DejaVu Sans", 14), XBrushes.DimGray, new XRect(0, 270, page.Width.Point, 25), XStringFormats.Center);
            graphics.DrawString(courseTitle, new XFont("DejaVu Sans", 22, XFontStyleEx.Bold), XBrushes.Black, new XRect(70, 305, page.Width.Point - 140, 50), XStringFormats.Center);
            graphics.DrawString($"Completed {completedAt:MMMM d, yyyy}", new XFont("DejaVu Sans", 12), XBrushes.DimGray, new XRect(0, 380, page.Width.Point, 22), XStringFormats.Center);
            graphics.DrawString($"Serial: {serialNumber}", new XFont("DejaVu Sans", 9), XBrushes.DimGray, new XPoint(55, page.Height.Point - 60));

            using var generator = new QRCodeGenerator();
            using var qrData = generator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);
            var qrBytes = new PngByteQRCode(qrData).GetGraphic(5);
            using var qrStream = new MemoryStream(qrBytes);
            using var qr = XImage.FromStream(qrStream);
            graphics.DrawImage(qr, page.Width.Point - 130, page.Height.Point - 135, 78, 78);
            graphics.DrawString("Verify online", new XFont("DejaVu Sans", 8), XBrushes.DimGray, new XRect(page.Width.Point - 145, page.Height.Point - 55, 110, 15), XStringFormats.Center);

            var output = new MemoryStream();
            document.Save(output, closeStream: false);
            output.Position = 0;
            return Task.FromResult<Stream>(output);
        }

        private static void EnsureFontResolver()
        {
            if (_fontConfigured) return;
            lock (FontLock)
            {
                if (_fontConfigured) return;
                GlobalFontSettings.FontResolver = new DejaVuFontResolver();
                _fontConfigured = true;
            }
        }

        private sealed class DejaVuFontResolver : IFontResolver
        {
            private static readonly string[] Paths =
            [
                "/usr/share/fonts/ttf-dejavu/DejaVuSans.ttf",
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                @"C:\Windows\Fonts\arial.ttf"
            ];

            public byte[] GetFont(string faceName)
            {
                var path = Paths.FirstOrDefault(File.Exists)
                    ?? throw new InvalidOperationException("A certificate font is not installed.");
                return File.ReadAllBytes(path);
            }

            public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) => new("DejaVuSans");
        }
    }
}
