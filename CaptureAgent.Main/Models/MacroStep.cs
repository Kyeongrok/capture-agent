using System.Drawing;

namespace CaptureAgent.Main.Models;

public class MacroStep
{
    public string Type { get; set; } = string.Empty;  // "Click" or "Capture"
    public int? X { get; set; }
    public int? Y { get; set; }
    public Rectangle? CaptureRegion { get; set; }
    public double WaitSeconds { get; set; } = 0.1;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
