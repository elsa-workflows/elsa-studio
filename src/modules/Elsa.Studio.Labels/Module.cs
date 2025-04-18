using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.WorkflowContexts.Widgets;

namespace Elsa.Studio.Labels;

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
        _widgetRegistry.Add(new WorkflowDefinitionLabelsEditorWidget());
        return base.InitializeAsync(cancellationToken);
    }
}