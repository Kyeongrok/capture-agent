using System.Runtime.InteropServices;
using System.Drawing;

namespace CaptureAgent.Main.PInvoke;

public interface IScreenMetricsService
{
    Rectangle GetScreenBounds();
    double GetDpiScaling(IntPtr hMonitor = default);
    Point ScreenToLocal(Point screenPoint, double dpiScale);
    Point LocalToScreen(Point localPoint, double dpiScale);
}

public class ScreenMetricsService : IScreenMetricsService
{
    private const uint DefaultDpi = 96;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const int MDT_EFFECTIVE_DPI = 0;

    public Rectangle GetScreenBounds()
    {
        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);
        return new Rectangle(0, 0, width, height);
    }

    public double GetDpiScaling(IntPtr hMonitor = default)
    {
        uint dpi = DefaultDpi;

        try
        {
            if (hMonitor == IntPtr.Zero)
            {
                var hwnd = GetActiveWindow();
                if (hwnd != IntPtr.Zero)
                {
                    hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                }
            }

            if (hMonitor != IntPtr.Zero)
            {
                int result = GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out uint dpiX, out uint dpiY);
                if (result == 0)
                {
                    dpi = dpiX;
                }
            }
        }
        catch
        {
            // DPI 조회 실패 시 기본값 사용
            dpi = DefaultDpi;
        }

        return dpi / (double)DefaultDpi;
    }

    public Point ScreenToLocal(Point screenPoint, double dpiScale)
    {
        return new Point(
            (int)(screenPoint.X / dpiScale),
            (int)(screenPoint.Y / dpiScale));
    }

    public Point LocalToScreen(Point localPoint, double dpiScale)
    {
        return new Point(
            (int)(localPoint.X * dpiScale),
            (int)(localPoint.Y * dpiScale));
    }
}
