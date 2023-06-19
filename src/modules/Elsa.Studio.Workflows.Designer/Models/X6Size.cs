using System.Text.Json.Serialization;

namespace Elsa.Studio.Workflows.Designer.Models;

public class X6Size
{
    [JsonConstructor]
    public X6Size()
    {
    }
    
    public X6Size(double width, double height)
    {
        Width = width;
        Height = height;
    }

    public double Width { get; set; }
    public double Height { get; set; }
}