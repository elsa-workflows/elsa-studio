using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit;

using WorkflowDefinition = Api.Client.Resources.WorkflowDefinitions.Models.WorkflowDefinition;

public partial class Workspace
{
    private MudDynamicTabs _dynamicTabs = default!;
    private readonly List<TabView> _tabs = new();
    private int _tabIndex = 0;

    [Parameter] public ICollection<WorkflowDefinition> WorkflowDefinitions { get; set; } = new List<WorkflowDefinition>();

    public class TabView
    {
        public string Label { get; set; } = default!;
        public Guid Id { get; set; }
        public bool ShowCloseIcon { get; set; } = true;
        public RenderFragment? ChildContent { get; set; }
    }

    protected override void OnParametersSet()
    {
        SetTabs();
    }

    private void AddTab(Guid id)
    {
        WorkflowDefinitions.Add(new WorkflowDefinition());
        SetTabs();
        _tabIndex = _tabs.Count - 1;
    }

    private void RemoveTab(Guid id)
    {
        var tabView = _tabs.SingleOrDefault((t) => Equals(t.Id, id));
        if (tabView is not null)
        {
            _tabs.Remove(tabView);
            StateHasChanged();
        }
    }

    private void SetTabs()
    {
        _tabs.Clear();

        foreach (var workflowDefinition in WorkflowDefinitions)
        {
            workflowDefinition.Name ??= "New Workflow";
            _tabs.Add(new TabView { Id = Guid.NewGuid(), Label = workflowDefinition.Name, ChildContent = RenderWorkflowEditor(workflowDefinition) });
        }

        _tabIndex = 0;
        StateHasChanged();
    }

    private RenderFragment RenderWorkflowEditor(WorkflowDefinition workflowDefinition)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(WorkflowEditor));
            builder.AddAttribute(1, nameof(WorkflowEditor.WorkflowDefinition), workflowDefinition);
            builder.CloseComponent();
        };
    }

    private void AddTabCallback() => AddTab(Guid.NewGuid());
    private void CloseTabCallback(MudTabPanel panel) => RemoveTab((Guid)panel.ID);
}