using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaptureAgent.Forms.UI.Views;

namespace CaptureAgent.Forms.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private RegionViewModel? regionViewModel;

    public MainWindowViewModel(RegionViewModel regionViewModel)
    {
        RegionViewModel = regionViewModel;
    }

    [RelayCommand]
    private void SelectNewRegion()
    {
        // 현재 메인 윈도우 최소화
        if (Application.Current.MainWindow is Window mainWindow)
        {
            mainWindow.WindowState = WindowState.Minimized;
        }

        // 오버레이 윈도우 생성 및 표시
        var overlayWindow = new ScreenOverlayWindow();
        overlayWindow.Show();
    }

    [RelayCommand]
    private void ShowMessage()
    {
        MessageBox.Show("Hello!", "CaptureAgent");
    }
}
