using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CaptureAgent.Forms.ViewModels;

namespace CaptureAgent.Forms.UI.Views;

public partial class ScreenOverlayWindow : Window
{
    private Rectangle? _selectionRectangle;
    private Point _startPoint;
    private bool _isSelecting;

    public ScreenOverlayWindow()
    {
        InitializeComponent();

        Cursor = Cursors.Cross;
        Loaded += (s, e) => SetupSelection();
    }

    private void SetupSelection()
    {
        var grid = (Grid)Content;
        var canvas = grid.Children[1] as Canvas;
        if (canvas == null) return;

        _selectionRectangle = new Rectangle
        {
            Stroke = new SolidColorBrush(Color.FromRgb(242, 163, 60)), // #F2A33C
            StrokeThickness = 2,
            Fill = new SolidColorBrush(Color.FromArgb(18, 242, 163, 60)), // rgba(242,163,60,0.07)
            Visibility = Visibility.Hidden
        };

        canvas.Children.Add(_selectionRectangle);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        _startPoint = e.GetPosition(this);
        _isSelecting = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_isSelecting || _selectionRectangle == null)
            return;

        var currentPoint = e.GetPosition(this);

        double x = Math.Min(_startPoint.X, currentPoint.X);
        double y = Math.Min(_startPoint.Y, currentPoint.Y);
        double width = Math.Abs(currentPoint.X - _startPoint.X);
        double height = Math.Abs(currentPoint.Y - _startPoint.Y);

        Canvas.SetLeft(_selectionRectangle, x);
        Canvas.SetTop(_selectionRectangle, y);
        _selectionRectangle.Width = width;
        _selectionRectangle.Height = height;
        _selectionRectangle.Visibility = Visibility.Visible;
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        _isSelecting = false;

        // 선택 완료 - 메인 윈도우에 좌표 전달
        if (_selectionRectangle?.Width > 0 && _selectionRectangle?.Height > 0)
        {
            int x = (int)Canvas.GetLeft(_selectionRectangle);
            int y = (int)Canvas.GetTop(_selectionRectangle);
            int width = (int)_selectionRectangle.Width;
            int height = (int)_selectionRectangle.Height;

            // 메인 윈도우 복원 및 좌표 업데이트
            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainWindow mainWindow && mainWindow.DataContext is MainWindowViewModel viewModel)
                {
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();

                    // 좌표 업데이트
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
