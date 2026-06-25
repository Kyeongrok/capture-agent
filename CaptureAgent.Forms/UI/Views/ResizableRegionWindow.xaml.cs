using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;
using CaptureAgent.Forms.ViewModels;
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
    private HandlePosition _draggedHandle = HandlePosition.None;
    private Point _dragStart;
    private List<(HandlePosition pos, Rectangle handle)> _resizeHandles = new();

    // 메인 윈도우와 좌표를 공유하기 위한 ViewModel (DI 싱글톤). 이동/리사이즈 시 여기에 반영한다.
    private readonly RegionViewModel? _regionViewModel;

    // 자기 자신이 ViewModel을 갱신하는 동안 들어오는 PropertyChanged를 무시하기 위한 가드.
    private bool _isSyncing;

    private enum HandlePosition { None, TopLeft, TopRight, BottomLeft, BottomRight, Top, Bottom, Left, Right }

    public ResizableRegionWindow(int x, int y, int width, int height, RegionViewModel? regionViewModel = null)
    {
        InitializeComponent();

        _regionX = x;
        _regionY = y;
        _regionWidth = width;
        _regionHeight = height;
        _regionViewModel = regionViewModel;

        Loaded += (s, e) => SetupRegion();
    }

    /// <summary>
    /// 현재 영역(DIP)을 물리 픽셀 Rectangle로 변환한다.
    /// 각 꼭짓점을 PointToScreen으로 변환해 모니터별 DPI를 정확히 반영한다.
    /// </summary>
    private System.Drawing.Rectangle GetPhysicalRegion()
    {
        // 아직 화면에 연결되지 않았으면 DIP 값을 그대로 사용 (PointToScreen 불가).
        if (PresentationSource.FromVisual(this) == null)
            return new System.Drawing.Rectangle(_regionX, _regionY, Math.Max(1, _regionWidth), Math.Max(1, _regionHeight));

        Point tl = PointToScreen(new Point(_regionX, _regionY));
        Point br = PointToScreen(new Point(_regionX + _regionWidth, _regionY + _regionHeight));
        int x = (int)Math.Round(tl.X);
        int y = (int)Math.Round(tl.Y);
        int w = Math.Max(1, (int)Math.Round(br.X - tl.X));
        int h = Math.Max(1, (int)Math.Round(br.Y - tl.Y));
        return new System.Drawing.Rectangle(x, y, w, h);
    }

    /// <summary>
    /// 현재 영역을 메인 윈도우의 RegionViewModel에 반영한다 (물리 픽셀 기준).
    /// 자기 자신이 일으킨 변경을 다시 받아 처리하지 않도록 _isSyncing 가드를 건다.
    /// </summary>
    private void SyncToViewModel()
    {
        if (_regionViewModel == null) return;
        _isSyncing = true;
        try
        {
            _regionViewModel.SetRegion(GetPhysicalRegion());
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    /// 메인 윈도우에서 좌표를 입력해 RegionViewModel이 바뀌면 프레임을 다시 그린다.
    /// (물리 픽셀 → DIP 변환 후 RedrawRegion. ViewModel로 되돌려 push 하지 않아 루프가 없다.)
    /// </summary>
    private void OnRegionViewModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_isSyncing || _regionViewModel == null) return;
        if (PresentationSource.FromVisual(this) == null) return;

        var r = _regionViewModel.GetRegion(); // 물리 픽셀
        Point dipTopLeft = PointFromScreen(new Point(r.X, r.Y));
        Point dipBottomRight = PointFromScreen(new Point(r.X + r.Width, r.Y + r.Height));
        _regionX = (int)Math.Round(dipTopLeft.X);
        _regionY = (int)Math.Round(dipTopLeft.Y);
        _regionWidth = Math.Max(1, (int)Math.Round(dipBottomRight.X - dipTopLeft.X));
        _regionHeight = Math.Max(1, (int)Math.Round(dipBottomRight.Y - dipTopLeft.Y));

        RedrawRegion();
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

        // 초기 영역을 메인 윈도우에 동기화
        SyncToViewModel();

        // 메인 윈도우 좌표 입력 → 프레임 갱신 (역방향 동기화) 구독
        if (_regionViewModel != null)
        {
            _regionViewModel.PropertyChanged += OnRegionViewModelChanged;
            Closed += (s, e) => _regionViewModel.PropertyChanged -= OnRegionViewModelChanged;
        }

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

    // 내부 조작(드래그/리사이즈) 시: 프레임을 다시 그리고 그 결과를 메인 윈도우에 반영.
    private void UpdateDisplay()
    {
        RedrawRegion();
        SyncToViewModel();
    }

    // 프레임/컨트롤바/핸들/치수만 다시 그린다 (ViewModel로 push 하지 않음).
    // 메인 윈도우에서 좌표를 입력해 들어온 변경에도 이 메서드만 호출한다 (피드백 루프 방지).
    private void RedrawRegion()
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
        {
            // 실제 캡처되는 물리 픽셀 크기를 표시한다 (메인 윈도우 표시와 일치).
            var r = GetPhysicalRegion();
            _dimensionDisplay.Text = $"{r.Width} × {r.Height}";
        }
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

    private async void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 영역(DIP)을 물리 픽셀로 변환 (창이 보이는 상태에서 미리 계산해 둔다).
            var region = GetPhysicalRegion();
            int physX = region.X;
            int physY = region.Y;
            int physWidth = region.Width;
            int physHeight = region.Height;

            // 오버레이(프레임/핸들/컨트롤바)가 캡처에 찍히지 않도록 잠시 숨긴 뒤,
            // 바탕화면이 다시 그려질 시간을 준다.
            Visibility = Visibility.Hidden;
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
            await Task.Delay(80);

            // 선택된 영역을 캡처
            string fileName;
            using (var bitmap = new System.Drawing.Bitmap(physWidth, physHeight))
            {
                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(physX, physY, 0, 0, new System.Drawing.Size(physWidth, physHeight));
                }

                // 파일 저장
                var timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                fileName = $"capture_{timestamp}.png";
                var desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
                var filePath = System.IO.Path.Combine(desktopPath, fileName);

                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                System.Diagnostics.Debug.WriteLine($"캡춰 저장됨: {filePath}");
            }

            // 오버레이를 다시 표시하고 저장 결과를 알린다.
            Visibility = Visibility.Visible;
            if (_filePathDisplay != null)
            {
                _filePathDisplay.Text = $"저장됨: {fileName}";
                _filePathDisplay.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // 초록색
            }
        }
        catch (System.Exception ex)
        {
            // 예외가 나도 숨긴 오버레이는 반드시 복원한다.
            Visibility = Visibility.Visible;
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
