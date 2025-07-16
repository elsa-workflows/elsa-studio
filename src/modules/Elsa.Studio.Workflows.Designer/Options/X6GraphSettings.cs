namespace Elsa.Studio.Workflows.Designer.Options;

public class X6GraphSettings
{
    public X6Grid Grid { get; set; } = new();
    public double MagnetThreshold { get; set; }
    public X6Panning Panning { get; set; } = new();
    public X6Mousewheel Mousewheel { get; set; } = new();
    public bool ResizingEnabled { get; set; } = true;
}