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
        var canvas = FindName("SelectionCanvas") as Canvas;
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
        _startPoint = Mouse.GetPosition(null);
        _isSelecting = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_isSelecting || _selectionRectangle == null)
            return;

        var currentPoint = Mouse.GetPosition(null);

        double x = Math.Min(_startPoint.X, currentPoint.X);
        double y = Math.Min(_startPoint.Y, currentPoint.Y);
        double width = Math.Abs(currentPoint.X - _startPoint.X);
        double height = Math.Abs(currentPoint.Y - _startPoint.Y);

        // Window 기준 좌표로 변환
        Point windowPos = PointFromScreen(new Point(x, y));
        Canvas.SetLeft(_selectionRectangle, windowPos.X);
        Canvas.SetTop(_selectionRectangle, windowPos.Y);
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
            // DPI 배율 고려한 좌표 계산
            double dpiScale = GetDpiScale();
            Point screenPos = PointToScreen(new Point(Canvas.GetLeft(_selectionRectangle), Canvas.GetTop(_selectionRectangle)));
            int x = (int)(screenPos.X / dpiScale);
            int y = (int)(screenPos.Y / dpiScale);
            int width = (int)(_selectionRectangle.Width / dpiScale);
            int height = (int)(_selectionRectangle.Height / dpiScale);

            // 오버레이2 생성 및 표시
            var resizableWindow = new ResizableRegionWindow(x, y, width, height);
            resizableWindow.Show();

            // 메인 윈도우에 좌표 업데이트
            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainWindow mainWindow && mainWindow.DataContext is MainWindowViewModel viewModel)
                {
                    if (viewModel.RegionViewModel is not null)
                    {
                        var region = new System.Drawing.Rectangle(x, y, width, height);
                        viewModel.RegionViewModel.SetRegion(region);
                    }
                    break;
                }
            }

            Close();
        }
    }
}
