using System.Windows;
using System.Windows.Controls;

namespace CaptureAgent.Support.UI.Units;

public class MinimizeButton : Button
{
    static MinimizeButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MinimizeButton),
            new FrameworkPropertyMetadata(typeof(MinimizeButton)));
    }
}
