using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.UI.Contracts;

namespace Elsa.Studio.Workflows.UI.Services;

/// <inheritdoc />
public class DefaultDiagramDesignerService : IDiagramDesignerService
{
    private readonly IEnumerable<IDiagramDesignerProvider> _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDiagramDesignerService"/> class.
    /// </summary>
    public DefaultDiagramDesignerService(IEnumerable<IDiagramDesignerProvider> providers)
    {
        _providers = providers;
    }

    /// <inheritdoc />
    public IDiagramDesigner GetDiagramDesigner(JsonObject activity)
    {
        var provider = _providers
            .OrderByDescending(x => x.Priority)
            .FirstOrDefault(x => x.GetSupportsActivity(activity)) ?? throw new Exception($"No diagram editor provider found for activity {activity.GetTypeName()}.");
        return provider.GetEditor();
    }
}