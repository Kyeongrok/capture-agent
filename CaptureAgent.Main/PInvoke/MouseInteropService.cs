using System.Runtime.InteropServices;
using System.Drawing;

namespace CaptureAgent.Main.PInvoke;

public interface IMouseInteropService
{
    (int X, int Y) GetCursorPosition();
    void SetCursorPosition(int x, int y);
    void ClickMouse(int x, int y, int delayMs = 100);
    Color GetPixelColor(int x, int y);
}

public class MouseInteropService : IMouseInteropService
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(IntPtr hdc, int x, int y);

    private const int INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    public (int X, int Y) GetCursorPosition()
    {
        GetCursorPos(out var pt);
        return (pt.X, pt.Y);
    }

    public void SetCursorPosition(int x, int y)
    {
        SetCursorPos(x, y);
    }

    public void ClickMouse(int x, int y, int delayMs = 100)
    {
        SetCursorPosition(x, y);

        if (delayMs > 0)
        {
            System.Threading.Thread.Sleep(delayMs);
        }

        // 마우스 클릭 시뮬레이션
        INPUT[] inputs = new INPUT[2];

        inputs[0] = new INPUT
        {
            type = INPUT_MOUSE,
            mi = new MOUSEINPUT
            {
                dx = x,
                dy = y,
                dwFlags = MOUSEEVENTF_LEFTDOWN,
                mouseData = 0,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            }
        };

        inputs[1] = new INPUT
        {
            type = INPUT_MOUSE,
            mi = new MOUSEINPUT
            {
                dx = x,
                dy = y,
                dwFlags = MOUSEEVENTF_LEFTUP,
                mouseData = 0,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            }
        };

        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }

    public Color GetPixelColor(int x, int y)
    {
        IntPtr hdc = GetDC(IntPtr.Zero);

        try
        {
            uint pixelColor = GetPixel(hdc, x, y);

            // BGR 형식을 RGB로 변환
            byte r = (byte)(pixelColor & 0xFF);
            byte g = (byte)((pixelColor >> 8) & 0xFF);
            byte b = (byte)((pixelColor >> 16) & 0xFF);

            return Color.FromArgb(r, g, b);
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hdc);
        }
    }
}
