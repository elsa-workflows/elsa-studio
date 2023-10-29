using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Elsa.Studio.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="RenderTreeBuilder"/> class.
/// </summary>
public static class RenderTreeBuilderExtensions
{
    /// <summary>
    /// Creates a component of the specified type.
    /// </summary>
    public static void CreateComponent<T>(this RenderTreeBuilder builder) where T : IComponent
    {
        var sequence = 0;
        CreateComponent<T>(builder, ref sequence);
    }

    /// <summary>
    /// Creates a component of the specified type.
    /// </summary>
    public static void CreateComponent<T>(this RenderTreeBuilder builder, ref int sequence) where T : IComponent
    {
        builder.OpenComponent<T>(sequence++);
        builder.CloseComponent();
    }
}