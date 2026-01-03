using BlazorMonaco.Editor;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using System.Text.Json;
using System.Text.Json.Nodes;
using ThrottleDebounce;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;

/// A workspace for editing a workflow definition.
public partial class WorkflowDefinitionWorkspace : IWorkspace, IDisposable
{
    private readonly RateLimitedFunc<Task> _throttledValueChanged;
    private readonly string _monacoEditorId = $"monaco-editor-{Guid.NewGuid():N}";
    private bool _autoApply;
    private int _tabIndex = 0;
    private bool _isInternalContentChange;
    private string? _lastMonacoEditorContent;
    private string _applyErrorMessage = string.Empty;
    private MudDynamicTabs _dynamicTabs = null!;
    private WorkflowDefinition? _workflowDefinition = null!;
    private WorkflowDefinition? _selectedWorkflowDefinition = null!;
    private StandaloneCodeEditor? _monacoEditor;

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;
    [Inject] private IOptions<WorkflowDefinitionOptions> WorkflowDefinitionOptions { get; set; } = default!;

    /// Gets or sets the workflow definition to edit.
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

    /// Gets or sets a specific version of the workflow definition to view.
    [Parameter] public WorkflowDefinition SelectedWorkflowDefinition { get; set; } = null!;

    /// <summary>An event that is invoked when a workflow definition has been executed.</summary>
    /// <remarks>The ID of the workflow instance is provided as the value to the event callback.</remarks>
    [Parameter] public Func<string, Task>? WorkflowDefinitionExecuted { get; set; }

    /// Gets or sets the event that occurs when the workflow definition version is updated.
    [Parameter] public Func<WorkflowDefinition, Task>? WorkflowDefinitionVersionSelected { get; set; }

    /// Gets or sets the event that occurs when an activity is selected.
    [Parameter] public Func<JsonObject, Task>? ActivitySelected { get; set; }

    private WorkflowEditor WorkflowEditor { get; set; } = null!;

    /// An event that is invoked when the workflow definition is updated.
    public event Func<Task>? WorkflowDefinitionUpdated;

    /// <inheritdoc />
    public bool IsReadOnly => _selectedWorkflowDefinition.GetIsReadOnly();

    /// <inheritdoc />
    public bool HasWorkflowEditPermission => (_selectedWorkflowDefinition?.Links?.Count(l => l.Rel == "publish") ?? 0) > 0;

    /// Gets the selected activity ID.
    public string? SelectedActivityId => WorkflowEditor.SelectedActivityId;
    private bool _isCodeViewTab => _tabIndex == 1;

    /// Gets the workflow definition serialized as a formatted JSON string.
    public string WorkflowDefinitionSerialized => JsonSerializer.Serialize(WorkflowEditor.WorkflowDefinition, new JsonSerializerOptions { WriteIndented = true });
    
    /// <inheritdoc />
    public WorkflowDefinitionWorkspace()
    {
        _throttledValueChanged = Debouncer.Debounce(UpdateEditorFromMonacoAsync, TimeSpan.FromMilliseconds(500));
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _autoApply = WorkflowDefinitionOptions.Value.AutoApplyMonacoChanges;
        _workflowDefinition = WorkflowDefinition;
        _selectedWorkflowDefinition = SelectedWorkflowDefinition;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _workflowDefinition = WorkflowDefinition;
        _selectedWorkflowDefinition = SelectedWorkflowDefinition;

        if (_selectedWorkflowDefinition == null)
            _selectedWorkflowDefinition = _workflowDefinition;
    }

    /// Displays the specified workflow definition version.
    public async Task DisplayWorkflowDefinitionVersionAsync(WorkflowDefinition workflowDefinition)
    {
        if (_selectedWorkflowDefinition == workflowDefinition)
            return;

        _selectedWorkflowDefinition = workflowDefinition;

        if (workflowDefinition.IsLatest)
            _workflowDefinition = workflowDefinition;

        if (WorkflowDefinitionVersionSelected != null)
            await WorkflowDefinitionVersionSelected(_selectedWorkflowDefinition);

        StateHasChanged();
    }

    /// Gets the currently selected workflow definition version.
    public WorkflowDefinition? GetSelectedWorkflowDefinitionVersion() => _selectedWorkflowDefinition;

    /// Determines whether the workspace is currently viewing a specific version of a workflow definition.
    public bool IsSelectedDefinition(string definitionVersionId) => _selectedWorkflowDefinition?.Id == definitionVersionId;

    /// Gets the selected workflow definition.
    public WorkflowDefinition? GetSelectedDefinition() => _selectedWorkflowDefinition;

    /// Displays the latest version of a workflow definition asynchronously.
    public async Task DisplayLatestWorkflowDefinitionVersionAsync()
    {
        var definitionId = _workflowDefinition!.DefinitionId;
        var definition = (await WorkflowDefinitionService.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest))!;
        await DisplayWorkflowDefinitionVersionAsync(definition);
    }

    private async Task OnWorkflowDefinitionPropsUpdated()
    {
        await WorkflowEditor.NotifyWorkflowChangedAsync();
        await UpdateMonacoFromEditorAsync();

        if (WorkflowDefinitionUpdated != null)
            await WorkflowDefinitionUpdated();
    }

    private async Task OnWorkflowDefinitionUpdated()
    {
        _workflowDefinition = WorkflowEditor.WorkflowDefinition!;
        _selectedWorkflowDefinition = _workflowDefinition;
        await InvokeAsync(StateHasChanged);
    }

    private StandaloneEditorConstructionOptions ConfigureMonacoEditor(StandaloneCodeEditor editor)
    {
        _applyErrorMessage = string.Empty;
        return new()
        {
            Language = "json",
            Value = WorkflowDefinitionSerialized,
            FontFamily = "Roboto Mono, monospace",
            RenderLineHighlight = "none",
            FixedOverflowWidgets = true,
            Minimap = new()
            {
                Enabled = false
            },
            AutomaticLayout = true,
            LineNumbers = "on",
            Theme = "vs",
            RoundedSelection = true,
            ScrollBeyondLastLine = false,
            OverviewRulerLanes = 0,
            OverviewRulerBorder = false,
            LineDecorationsWidth = 0,
            HideCursorInOverviewRuler = true,
            GlyphMargin = false,
            ReadOnly = false,
            DomReadOnly = false
        };
    }

    private async Task OnMonacoContentChangedAsync(ModelContentChangedEvent e)
    {
        if (_isInternalContentChange || !_autoApply)
            return;

        await _throttledValueChanged.InvokeAsync();
    }

    private async Task OnApplyClicked() => await UpdateEditorFromMonacoAsync();

    private async Task UpdateEditorFromMonacoAsync()
    {
        var value = await _monacoEditor!.GetValue();
        if (string.Equals(value, _lastMonacoEditorContent))
            return;
        _lastMonacoEditorContent = value;

        WorkflowDefinition? deserialized = null;
        try
        {
            try
            {
                deserialized = JsonSerializer.Deserialize<WorkflowDefinition?>(value);
            }
            catch (JsonException ex)
            {
                _applyErrorMessage = ex.Message;
                return;
            }

            if (deserialized == null)
            {
                _applyErrorMessage = "Failed to deserialize workflow definition.";
                return;
            }

            _applyErrorMessage = string.Empty;
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }

        var incomingJson = JsonSerializer.Serialize(deserialized, new JsonSerializerOptions { WriteIndented = true });
        if (string.Equals(WorkflowDefinitionSerialized, incomingJson, StringComparison.Ordinal))
            return;

        WorkflowEditor.WorkflowDefinition = deserialized;

        await OnWorkflowDefinitionUpdated();

        if (WorkflowDefinitionUpdated != null)
            await WorkflowDefinitionUpdated();

        await InvokeAsync(StateHasChanged);
    }

    private async Task UpdateMonacoFromEditorAsync()
    {
        if (!_isCodeViewTab || _monacoEditor is null)
            return;

        try
        {
            _isInternalContentChange = true;
            var model = await _monacoEditor!.GetModel();
            var json = WorkflowDefinitionSerialized;
            if (json == _lastMonacoEditorContent)
                return;

            _lastMonacoEditorContent = json;
            await model.SetValue(json);
        }
        finally
        {
            _isInternalContentChange = false;
        }
    }

    private async Task ReloadMonacoClick()
    {
        try
        {
            _isInternalContentChange = true;
            var model = await _monacoEditor!.GetModel();
            await model.SetValue(WorkflowDefinitionSerialized);
            await UpdateEditorFromMonacoAsync();
        }
        finally
        {
            _isInternalContentChange = false;
        }
    }

    void IDisposable.Dispose() => _throttledValueChanged.Dispose();
}