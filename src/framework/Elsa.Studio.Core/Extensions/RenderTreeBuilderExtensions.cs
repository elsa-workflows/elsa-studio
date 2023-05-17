using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Elsa.Studio.Extensions;

public static class RenderTreeBuilderExtensions
{
    public static void CreateComponent<T>(this RenderTreeBuilder builder) where T : IComponent
    {
        var sequence = 0;
        CreateComponent<T>(builder, ref sequence);
    }

    public static void CreateComponent<T>(this RenderTreeBuilder builder, ref int sequence) where T : IComponent
    {
        builder.OpenComponent<T>(sequence++);
        builder.CloseComponent();
    }
}