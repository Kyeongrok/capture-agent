using System.Windows;
using System.Windows.Controls;
using CaptureAgent.Forms.ViewModels;
using CaptureAgent.Support.UI.Units;

namespace CaptureAgent.Forms.UI.Views;

public class MainWindow : CaptureAgentWindow
{
    static MainWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MainWindow),
            new FrameworkPropertyMetadata(typeof(MainWindow)));
    }

    public MainWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        Closing += (s, e) => CloseAllOtherWindows();

        // 앱 아이콘 설정
        try
        {
            Icon = new System.Windows.Media.Imaging.BitmapImage(
                new System.Uri("pack://application:,,,/CaptureAgent;component/../../../CaptureAgent/App.ico.png"));
        }
        catch { /* 아이콘 로드 실패 무시 */ }
    }

    private void CloseAllOtherWindows()
    {
        // 메인 윈도우를 제외한 모든 윈도우 닫기
        var windowsToClose = Application.Current.Windows
            .Cast<Window>()
            .Where(w => w != this)
            .ToList();

        foreach (var window in windowsToClose)
        {
            window.Close();
        }
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        var minimizeButton = GetTemplateChild("PART_MinimizeButton") as Button;
        if (minimizeButton != null)
            minimizeButton.Click += (s, e) => WindowState = System.Windows.WindowState.Minimized;

        var maximizeButton = GetTemplateChild("PART_MaximizeButton") as Button;
        if (maximizeButton != null)
            maximizeButton.Click += (s, e) =>
                WindowState = WindowState == System.Windows.WindowState.Maximized
                    ? System.Windows.WindowState.Normal
                    : System.Windows.WindowState.Maximized;

        var closeButton = GetTemplateChild("PART_CloseButton") as Button;
        if (closeButton != null)
            closeButton.Click += (s, e) => Close();
    }
}
