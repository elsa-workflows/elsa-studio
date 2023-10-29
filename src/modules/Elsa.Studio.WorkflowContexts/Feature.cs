using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;
using Elsa.Studio.Contracts;
using Elsa.Studio.WorkflowContexts.Widgets;

namespace Elsa.Studio.WorkflowContexts;

/// <summary>
/// Registers the workflow contexts feature.
/// </summary>
[RemoteFeature("Elsa.WorkflowContexts")]
public class Feature : FeatureBase
{
    private readonly IWidgetRegistry _widgetRegistry;

    /// <inheritdoc />
    public Feature(IWidgetRegistry widgetRegistry)
    {
        _widgetRegistry = widgetRegistry;
    }
    
    /// <inheritdoc />
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        _widgetRegistry.Add(new WorkflowContextsEditorWidget());
        return default;
    }
}