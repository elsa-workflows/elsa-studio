using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

/// <summary>
/// Provides a handler for the <see cref="UIHint.Json"/> UI hint.
/// </summary>
public class JsonEditorHandler : IUIHintHandler
{
    /// <inheritdoc />
    public bool GetSupportsUIHint(string uiHint) => uiHint is UIHint.Json;

    /// <inheritdoc />
    public string UISyntax => WellKnownSyntaxNames.Literal;

    /// <inheritdoc />
    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(Json));
            builder.AddAttribute(1, nameof(Json.EditorContext), context);
            builder.CloseComponent();
        };
    }
}