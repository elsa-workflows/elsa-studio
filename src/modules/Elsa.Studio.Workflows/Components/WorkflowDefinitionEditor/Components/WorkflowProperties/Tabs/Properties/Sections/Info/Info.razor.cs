using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.
    Sections.Info;

public partial class Info
{
    private DataPanelModel _workflowInfo = new ();
    [Inject] private ILocalizer _localizer { get; set; } = default!;
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _workflowInfo =
        [
            new DataPanelItem(_localizer["Definition ID"], WorkflowDefinition.DefinitionId),
            new DataPanelItem(_localizer["Version ID"], WorkflowDefinition.Id),
            new DataPanelItem(_localizer["Version"], WorkflowDefinition.Version.ToString()),
            new DataPanelItem(_localizer["Status"], WorkflowDefinition.IsPublished ? _localizer["Published"] : _localizer["Draft"]),
            new DataPanelItem(_localizer["Readonly"], WorkflowDefinition.IsReadonly ? _localizer["Yes"] : _localizer["No"])
        ];
    }
}