using System.Text.Json.Serialization;

namespace Elsa.Studio.Workflows.Designer.Models;

public class X6Position
{
    [JsonConstructor]
    public X6Position()
    {
    }

    public X6Position(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; set; }
    public double Y { get; set; }
}