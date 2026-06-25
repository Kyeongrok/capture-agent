using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Rectangle = System.Windows.Shapes.Rectangle;
using Point = System.Windows.Point;

namespace CaptureAgent.Forms.UI.Views;

public partial class ResizableRegionWindow : Window
{
    private int _regionX, _regionY, _regionWidth, _regionHeight;
    private Border? _regionFrame;
    private Border? _controlBar;
    private TextBlock? _dimensionDisplay;
    private TextBlock? _filePathDisplay;
    private Rectangle? _handle;
    private HandlePosition _draggedHandle = HandlePosition.None;
    private Point _dragStart;
    private List<(HandlePosition pos, Rectangle handle)> _resizeHandles = new();

    private enum HandlePosition { None, TopLeft, TopRight, BottomLeft, BottomRight, Top, Bottom, Left, Right }

    public ResizableRegionWindow(int x, int y, int width, int height)
    {
        InitializeComponent();

        _regionX = x;
        _regionY = y;
        _regionWidth = width;
        _regionHeight = height;

        Loaded += (s, e) => SetupRegion();
    }

    private void SetupRegion()
    {
        // 윈도우를 전체 화면으로 설정
        Left = 0;
        Top = 0;
        Width = System.Windows.SystemParameters.VirtualScreenWidth;
        Height = System.Windows.SystemParameters.VirtualScreenHeight;

        _regionFrame = FindName("RegionFrame") as Border;
        _controlBar = FindName("ControlBar") as Border;
        _dimensionDisplay = FindName("DimensionDisplay") as TextBlock;
        _filePathDisplay = FindName("FilePathDisplay") as TextBlock;
        var canvas = FindName("RegionCanvas") as Canvas;

        if (canvas == null || _regionFrame == null) return;

        // 영역 프레임 설정
        Canvas.SetLeft(_regionFrame, _regionX);
        Canvas.SetTop(_regionFrame, _regionY);
        _regionFrame.Width = _regionWidth;
        _regionFrame.Height = _regionHeight;

        // 컨트롤 바 위치
        if (_controlBar != null)
        {
            Canvas.SetLeft(_controlBar, _regionX);
            Canvas.SetTop(_controlBar, _regionY - 36);
        }

        // 치수 텍스트
        UpdateDimensionDisplay();

        // 리사이즈 핸들 추가
        AddResizeHandles(canvas);

        // 컨트롤 바 드래그 이벤트
        if (_controlBar != null)
        {
            _controlBar.MouseDown += ControlBar_MouseDown;
            _controlBar.MouseMove += ControlBar_MouseMove;
            _controlBar.MouseUp += ControlBar_MouseUp;
            _controlBar.Cursor = Cursors.Hand;
        }

        // 버튼 이벤트
        if (_controlBar?.Child is StackPanel stackPanel)
        {
            var buttons = stackPanel.Children.OfType<Button>().ToList();
            if (buttons.Count >= 2)
            {
                // 캡춰 버튼 (첫 번째)
                buttons[0].Click += CaptureButton_Click;
                // 닫기 버튼 (두 번째)
                buttons[1].Click += (s, e) => Close();
            }
        }
    }

    private void AddResizeHandles(Canvas canvas)
    {
        var handles = new[]
        {
            (HandlePosition.TopLeft, _regionX - 4, _regionY - 4),
            (HandlePosition.TopRight, _regionX + _regionWidth, _regionY - 4),
            (HandlePosition.BottomLeft, _regionX - 4, _regionY + _regionHeight),
            (HandlePosition.BottomRight, _regionX + _regionWidth, _regionY + _regionHeight),
            (HandlePosition.Top, _regionX + _regionWidth / 2 - 4, _regionY - 4),
            (HandlePosition.Bottom, _regionX + _regionWidth / 2 - 4, _regionY + _regionHeight),
            (HandlePosition.Left, _regionX - 4, _regionY + _regionHeight / 2 - 4),
            (HandlePosition.Right, _regionX + _regionWidth, _regionY + _regionHeight / 2 - 4)
        };

        foreach (var (pos, x, y) in handles)
        {
            var handle = new Rectangle
            {
                Width = 8,
                Height = 8,
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)),
                Cursor = GetCursorForHandle(pos)
            };

            Canvas.SetLeft(handle, x);
            Canvas.SetTop(handle, y);

            var posValue = pos;
            handle.MouseDown += (s, e) =>
            {
                _draggedHandle = posValue;
                _dragStart = e.GetPosition(this);
                handle.CaptureMouse();
            };

            handle.MouseMove += (s, e) =>
            {
                if (_draggedHandle == HandlePosition.None) return;
                var delta = e.GetPosition(this) - _dragStart;
                ResizeRegion(_draggedHandle, (int)delta.X, (int)delta.Y);
                _dragStart = e.GetPosition(this);
            };

            handle.MouseUp += (s, e) =>
            {
                _draggedHandle = HandlePosition.None;
                handle.ReleaseMouseCapture();
            };

            canvas.Children.Add(handle);
            _resizeHandles.Add((pos, handle));
        }
    }

    private void ResizeRegion(HandlePosition handle, int dx, int dy)
    {
        switch (handle)
        {
            case HandlePosition.TopLeft:
                _regionX += dx;
                _regionY += dy;
                _regionWidth -= dx;
                _regionHeight -= dy;
                break;
            case HandlePosition.TopRight:
                _regionWidth += dx;
                _regionHeight += dy;
                break;
            case HandlePosition.BottomLeft:
                _regionX += dx;
                _regionWidth -= dx;
                _regionHeight += dy;
                break;
            case HandlePosition.BottomRight:
                _regionWidth += dx;
                _regionHeight += dy;
                break;
            case HandlePosition.Top:
                _regionY += dy;
                _regionHeight -= dy;
                break;
            case HandlePosition.Bottom:
                _regionHeight += dy;
                break;
            case HandlePosition.Left:
                _regionX += dx;
                _regionWidth -= dx;
                break;
            case HandlePosition.Right:
                _regionWidth += dx;
                break;
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_regionFrame == null || _controlBar == null) return;

        Canvas.SetLeft(_regionFrame, _regionX);
        Canvas.SetTop(_regionFrame, _regionY);
        _regionFrame.Width = _regionWidth;
        _regionFrame.Height = _regionHeight;

        // 컨트롤 바 위치 계산 (화면 경계 내로 제한)
        var controlBarX = _regionX;
        var controlBarY = _regionY - 30; // 높이를 30으로 줄임

        // 화면 경계 내로 제한
        if (controlBarY < 0) controlBarY = 0;
        if (controlBarX < 0) controlBarX = 0;
        if (controlBarX + _controlBar.ActualWidth > Width)
            controlBarX = (int)(Width - _controlBar.ActualWidth);

        Canvas.SetLeft(_controlBar, controlBarX);
        Canvas.SetTop(_controlBar, controlBarY);

        // 리사이즈 핸들 위치 업데이트
        UpdateResizeHandles();

        UpdateDimensionDisplay();
    }

    private void UpdateResizeHandles()
    {
        var handlePositions = new Dictionary<HandlePosition, (int x, int y)>
        {
            { HandlePosition.TopLeft, (_regionX - 4, _regionY - 4) },
            { HandlePosition.TopRight, (_regionX + _regionWidth, _regionY - 4) },
            { HandlePosition.BottomLeft, (_regionX - 4, _regionY + _regionHeight) },
            { HandlePosition.BottomRight, (_regionX + _regionWidth, _regionY + _regionHeight) },
            { HandlePosition.Top, (_regionX + _regionWidth / 2 - 4, _regionY - 4) },
            { HandlePosition.Bottom, (_regionX + _regionWidth / 2 - 4, _regionY + _regionHeight) },
            { HandlePosition.Left, (_regionX - 4, _regionY + _regionHeight / 2 - 4) },
            { HandlePosition.Right, (_regionX + _regionWidth, _regionY + _regionHeight / 2 - 4) }
        };

        foreach (var (pos, handle) in _resizeHandles)
        {
            if (handlePositions.TryGetValue(pos, out var position))
            {
                Canvas.SetLeft(handle, position.x);
                Canvas.SetTop(handle, position.y);
            }
        }
    }

    private void UpdateDimensionDisplay()
    {
        if (_dimensionDisplay != null)
            _dimensionDisplay.Text = $"{Math.Max(1, _regionWidth)} × {Math.Max(1, _regionHeight)}";
    }

    private Cursor GetCursorForHandle(HandlePosition handle) => handle switch
    {
        HandlePosition.TopLeft or HandlePosition.BottomRight => Cursors.SizeNWSE,
        HandlePosition.TopRight or HandlePosition.BottomLeft => Cursors.SizeNESW,
        HandlePosition.Top or HandlePosition.Bottom => Cursors.SizeNS,
        HandlePosition.Left or HandlePosition.Right => Cursors.SizeWE,
        _ => Cursors.Arrow
    };

    private bool _isDraggingControlBar = false;
    private Point _controlBarDragStart;

    private void ControlBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingControlBar = true;
        _controlBarDragStart = e.GetPosition(this);
        _controlBar?.CaptureMouse();
    }

    private void ControlBar_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingControlBar || _controlBar == null) return;

        var currentPos = e.GetPosition(this);
        var delta = currentPos - _controlBarDragStart;

        _regionX += (int)delta.X;
        _regionY += (int)delta.Y;

        UpdateDisplay();
        _controlBarDragStart = currentPos;
    }

    private void ControlBar_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingControlBar = false;
        _controlBar?.ReleaseMouseCapture();
    }

    private void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 선택된 영역을 캡처
            using (var bitmap = new System.Drawing.Bitmap(_regionWidth, _regionHeight))
            {
                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(_regionX, _regionY, 0, 0, new System.Drawing.Size(_regionWidth, _regionHeight));
                }

                // 파일 저장
                var timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"capture_{timestamp}.png";
                var desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
                var filePath = System.IO.Path.Combine(desktopPath, fileName);

                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

                // 저장 경로 UI에 표시
                if (_filePathDisplay != null)
                {
                    _filePathDisplay.Text = $"저장됨: {fileName}";
                    _filePathDisplay.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // 초록색
                }

                System.Diagnostics.Debug.WriteLine($"캡춰 저장됨: {filePath}");
            }
        }
        catch (System.Exception ex)
        {
            if (_filePathDisplay != null)
            {
                _filePathDisplay.Text = $"오류: {ex.Message}";
                _filePathDisplay.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // 빨간색
            }
            System.Diagnostics.Debug.WriteLine($"캡춰 오류: {ex.Message}");
        }
    }

    public System.Drawing.Rectangle GetFinalRegion() => new(_regionX, _regionY, _regionWidth, _regionHeight);
}
