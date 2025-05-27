using System;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties;

/// <summary>
/// Model describing a dynamically rendered input editor.
/// </summary>
/// <param name="Key">A unique key to force component recreation when the model is rebuilt.</param>
/// <param name="Index">The position of the input in the list.</param>
/// <param name="Editor">The editor fragment to render.</param>
public record ActivityInputDisplayModel(Guid Key, int Index, RenderFragment Editor);
