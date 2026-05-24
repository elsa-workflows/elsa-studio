using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.ConsoleLogs.UI.Components;
using Elsa.Studio.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.UI.Widgets;

/// <summary>
/// Adds a console logs tab to the workflow instance viewer bottom panel.
/// </summary>
public class WorkflowInstanceConsoleLogsTabWidget : IWidget
{
    private const string ZoneName = "workflow-instance-viewer-bottom-tabs";
    private readonly ILocalizer _localizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowInstanceConsoleLogsTabWidget"/> class.
    /// </summary>
    public WorkflowInstanceConsoleLogsTabWidget(ILocalizer localizer)
    {
        _localizer = localizer;
    }

    /// <inheritdoc />
    public string Zone => ZoneName;

    /// <inheritdoc />
    public double Order => 100;

    /// <inheritdoc />
    public Func<IDictionary<string, object?>, RenderFragment> Render => attributes => builder =>
    {
        var workflowInstanceId = attributes.TryGetValue("WorkflowInstanceId", out var id) ? id as string : null;
        var visiblePaneHeight = attributes.TryGetValue("VisiblePaneHeight", out var height) && height is int value ? value : (int?)null;
        var style = visiblePaneHeight is > 0 ? $"height: {Math.Max(visiblePaneHeight.Value - 48, 120)}px;" : null;

        builder.OpenComponent<MudTabPanel>(0);
        builder.AddAttribute(1, nameof(MudTabPanel.Text), _localizer["Console"]);
        builder.AddAttribute(2, nameof(MudTabPanel.ChildContent), (RenderFragment)(contentBuilder =>
        {
            contentBuilder.OpenComponent<ConsoleLogViewer>(0);
            contentBuilder.AddAttribute(1, nameof(ConsoleLogViewer.ShowFilters), false);
            contentBuilder.AddAttribute(2, nameof(ConsoleLogViewer.WorkflowInstanceId), workflowInstanceId);
            contentBuilder.AddAttribute(3, nameof(ConsoleLogViewer.Class), "console-log-viewer-embedded");
            contentBuilder.AddAttribute(4, nameof(ConsoleLogViewer.Style), style);
            contentBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    };
}
