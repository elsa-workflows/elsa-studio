using Elsa.Api.Client.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.UI.Contracts;
using JetBrains.Annotations;
using MudBlazor;
using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;

/// <summary>
/// Provides flowchart diagram designer services.
/// </summary>
/// <param name="localizer">The localizer used to provide localized strings for the designer interface.</param>
/// <param name="dialogService">The dialog service used to display dialogs within the designer.</param>
[UsedImplicitly]
public class FlowchartDiagramDesignerProvider(ILocalizer localizer, IDialogService dialogService) : IDiagramDesignerProvider
{
    /// <inheritdoc />
    public double Priority => 0;

    /// <inheritdoc />
    public bool GetSupportsActivity(JsonObject activity) => activity.GetTypeName() == "Elsa.Flowchart";

    /// <inheritdoc />
    public IDiagramDesigner GetEditor() => new FlowchartDiagramDesigner(localizer, dialogService);
}