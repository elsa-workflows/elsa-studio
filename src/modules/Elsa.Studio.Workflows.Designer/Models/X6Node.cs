using Elsa.Api.Client.Activities;

namespace Elsa.Studio.Workflows.Designer.Models;

public class X6Node
{
    public string Id { get; set; } = default!;
    public string Shape { get; set; } = default!;
    public X6Position Position { get; set; } = default!;
    public X6Size Size { get; set; } = default!;

    public Activity Data { get; set; } = default!;
    public X6Ports Ports { get; set; } = new();
}