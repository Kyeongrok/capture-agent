using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CaptureAgent.Forms.ViewModels;
using System.Windows.Media.Imaging;

namespace CaptureAgent.Forms.UI.Views;

public partial class ScreenOverlayWindow : Window
{
    private Rectangle? _selectionRectangle;
    private TextBlock? _dimensionText;
    private Canvas? _selectionCanvas;
    private Point _startPoint;
    private bool _isSelecting;

    public ScreenOverlayWindow()
    {
        InitializeComponent();

        Cursor = Cursors.Cross;
        Loaded += (s, e) => SetupSelection();
    }

    private double GetDpiScale()
    {
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
    }

    private void SetupSelection()
    {
        _dimensionText = FindName("DimensionText") as TextBlock;
        _selectionCanvas = FindName("SelectionCanvas") as Canvas;
        var canvas = _selectionCanvas;
        if (canvas == null) return;

        _selectionRectangle = new Rectangle
        {
            Stroke = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green #4CAF50
            StrokeThickness = 2,
            Fill = new SolidColorBrush(Color.FromArgb(25, 76, 175, 80)), // rgba(76,175,80,0.1)
            Visibility = Visibility.Hidden
        };

        canvas.Children.Add(_selectionRectangle);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        // 선택 캔버스 기준 좌표(DIP)로 시작점을 기록한다.
        _startPoint = e.GetPosition(_selectionCanvas);
        _isSelecting = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_isSelecting || _selectionRectangle == null)
            return;

        var currentPoint = e.GetPosition(_selectionCanvas);

        double x = Math.Min(_startPoint.X, currentPoint.X);
        double y = Math.Min(_startPoint.Y, currentPoint.Y);
        double width = Math.Abs(currentPoint.X - _startPoint.X);
        double height = Math.Abs(currentPoint.Y - _startPoint.Y);

        // 좌표는 이미 캔버스 기준 DIP 이므로 그대로 배치한다.
        // (PointFromScreen 으로 변환하면 고DPI 에서 좌표가 1/배율 로 줄어
        //  사각형이 마우스를 따라오지 못한다 — 기존 버그의 원인.)
        Canvas.SetLeft(_selectionRectangle, x);
        Canvas.SetTop(_selectionRectangle, y);
        _selectionRectangle.Width = width;
        _selectionRectangle.Height = height;
        _selectionRectangle.Visibility = Visibility.Visible;

        // 차원 텍스트 업데이트
        if (_dimensionText != null)
        {
            _dimensionText.Text = $"{(int)width}x{(int)height}";
            _dimensionText.Visibility = Visibility.Visible;

            // 텍스트 위치: 선택 영역의 우측 하단에 표시
            double textX = x + width + 10;
            double textY = y + height + 10;

            Canvas.SetLeft(_dimensionText, textX);
            Canvas.SetTop(_dimensionText, textY);
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        _isSelecting = false;

        if (_dimensionText != null)
        {
            _dimensionText.Visibility = Visibility.Hidden;
        }

        // 선택 완료 - 오버레이2 (ResizableRegionWindow) 띄우기
        if (_selectionRectangle?.Width > 0 && _selectionRectangle?.Height > 0)
        {
            // 모든 좌표를 DIP(논리 단위)로 통일.
            // x, y: PointToScreen은 물리 픽셀을 주므로 dpiScale로 나눠 DIP로 변환.
            // width, height: 이미 DIP 단위이므로 변환하지 않는다.
            double dpiScale = GetDpiScale();
            Point screenPos = PointToScreen(new Point(Canvas.GetLeft(_selectionRectangle), Canvas.GetTop(_selectionRectangle)));
            int x = (int)(screenPos.X / dpiScale);
            int y = (int)(screenPos.Y / dpiScale);
            int width = (int)_selectionRectangle.Width;
            int height = (int)_selectionRectangle.Height;

            // 메인 윈도우의 RegionViewModel을 찾아 오버레이2에 전달한다.
            // 좌표 동기화는 오버레이2(ResizableRegionWindow)가 전담한다.
            RegionViewModel? regionViewModel = null;
            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainWindow mainWindow && mainWindow.DataContext is MainWindowViewModel viewModel)
                {
                    regionViewModel = viewModel.RegionViewModel;
                    break;
                }
            }

            // 오버레이2 생성 및 표시
            var resizableWindow = new ResizableRegionWindow(x, y, width, height, regionViewModel);
            resizableWindow.Show();

            Close();
        }
    }
}
