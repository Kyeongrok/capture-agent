using System.Windows;

namespace CaptureAgent.Support.UI.Units;

public class CaptureAgentWindow : Window
{
    static CaptureAgentWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CaptureAgentWindow),
            new FrameworkPropertyMetadata(typeof(CaptureAgentWindow)));
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Maximized)
            MaxHeight = SystemParameters.WorkArea.Height;
        else
            MaxHeight = double.PositiveInfinity;
    }
}
