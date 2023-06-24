namespace Elsa.Studio.Workflows.Core.Models;

public class ActivityDisplaySettings
{
    public ActivityDisplaySettings()
    {
    }

    public ActivityDisplaySettings(string color, string? icon = default)
    {
        Color = color;
        Icon = icon;
    }
    
    public string? Icon { get; set; }
    public string Color { get; set; } = default!;
}