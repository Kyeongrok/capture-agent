using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using CaptureAgent.Main.Models;

namespace CaptureAgent.Main.PInvoke;

public interface IScreenCaptureService
{
    Bitmap CaptureScreen(Rectangle bounds);
    Task CaptureScreenToFileAsync(string path, Rectangle bounds, Models.ImageFormat format);
}

public class ScreenCaptureService : IScreenCaptureService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, uint rop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private const uint SRCCOPY = 0x00CC0020;

    private readonly IScreenMetricsService _metricsService;

    public ScreenCaptureService(IScreenMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    public Bitmap CaptureScreen(Rectangle bounds)
    {
        // DPI 스케일링 적용
        double dpiScale = _metricsService.GetDpiScaling();
        var scaledBounds = new Rectangle(
            (int)(bounds.X * dpiScale),
            (int)(bounds.Y * dpiScale),
            (int)(bounds.Width * dpiScale),
            (int)(bounds.Height * dpiScale));

        IntPtr hdcScreen = GetDC(IntPtr.Zero);
        IntPtr hdcMemory = CreateCompatibleDC(hdcScreen);

        try
        {
            IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, scaledBounds.Width, scaledBounds.Height);

            if (hBitmap == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create compatible bitmap.");
            }

            IntPtr oldBitmap = SelectObject(hdcMemory, hBitmap);

            bool success = BitBlt(
                hdcMemory,
                0, 0,
                scaledBounds.Width, scaledBounds.Height,
                hdcScreen,
                scaledBounds.X, scaledBounds.Y,
                SRCCOPY);

            SelectObject(hdcMemory, oldBitmap);

            if (!success)
            {
                DeleteObject(hBitmap);
                throw new InvalidOperationException("BitBlt failed.");
            }

            Bitmap bitmap = Image.FromHbitmap(hBitmap);
            DeleteObject(hBitmap);

            return bitmap;
        }
        finally
        {
            DeleteDC(hdcMemory);
            ReleaseDC(IntPtr.Zero, hdcScreen);
        }
    }

    public async Task CaptureScreenToFileAsync(string path, Rectangle bounds, Models.ImageFormat format)
    {
        Bitmap bitmap = CaptureScreen(bounds);

        try
        {
            await Task.Run(() =>
            {
                System.Drawing.Imaging.ImageFormat imageFormat = format switch
                {
                    Models.ImageFormat.PNG => System.Drawing.Imaging.ImageFormat.Png,
                    Models.ImageFormat.JPG => System.Drawing.Imaging.ImageFormat.Jpeg,
                    Models.ImageFormat.BMP => System.Drawing.Imaging.ImageFormat.Bmp,
                    _ => System.Drawing.Imaging.ImageFormat.Png
                };

                bitmap.Save(path, imageFormat);
            });
        }
        finally
        {
            bitmap?.Dispose();
        }
    }
}
