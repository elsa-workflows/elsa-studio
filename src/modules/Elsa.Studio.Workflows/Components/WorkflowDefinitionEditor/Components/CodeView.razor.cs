using BlazorMonaco.Editor;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using System.Text.Json;
using ThrottleDebounce;
using Microsoft.AspNetCore.Components;
using Elsa.Studio.Localization;
using Elsa.Studio.Extensions;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;

public partial class CodeView : IDisposable
{
    private readonly string _monacoEditorId = $"monaco-editor-{Guid.NewGuid():N}";
    private StandaloneCodeEditor? _monacoEditor;
    private bool _isInternalContentChange;
    private string? _lastMonacoEditorContent;
    private string _applyErrorMessage = string.Empty;
    private RateLimitedFunc<Task> _throttledValueChanged;

    [Inject] private ILocalizer Localizer { get; set; } = null!;

    [Parameter] public string WorkflowDefinitionSerialized { get; set; } = string.Empty;
    [Parameter] public Func<WorkflowDefinition, Task>? ApplyWorkflowDefinition { get; set; }

    [Parameter] public bool AutoApply { get; set; }
    [Parameter] public EventCallback<bool> AutoApplyChanged { get; set; }

    protected override void OnInitialized()
    {
        _throttledValueChanged = Debouncer.Debounce(UpdateEditorFromCodeViewAsync, TimeSpan.FromMilliseconds(500));
    }
    /// <summary>
    /// Updates the code view in the editor with the specified JSON content asynchronously.
    /// </summary>
    /// <param name="json">The JSON string representing the new content to display in the code editor.</param>
    public async Task UpdateCodeViewFromEditorAsync(string json)
    {
        try
        {
            _isInternalContentChange = true;
            var model = await _monacoEditor!.GetModel();
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
        if (_isInternalContentChange || !AutoApply)
            return;

        await _throttledValueChanged.InvokeAsync();
    }

    private async Task OnApplyClicked() => await UpdateEditorFromCodeViewAsync();

    private async Task UpdateEditorFromCodeViewAsync()
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

        if (ApplyWorkflowDefinition is not null)
            await ApplyWorkflowDefinition(deserialized);
    }

    private async Task ReloadMonacoClick()
    {
        try
        {
            _isInternalContentChange = true;
            var model = await _monacoEditor!.GetModel();
            await model.SetValue(WorkflowDefinitionSerialized);
            await UpdateEditorFromCodeViewAsync();
        }
        finally
        {
            _isInternalContentChange = false;
        }
    }
    private async Task OnAutoApplyChanged(bool? value)
    {
        AutoApply = value ?? false;
        if (AutoApplyChanged.HasDelegate)
            await AutoApplyChanged.InvokeAsync(AutoApply);

        if (AutoApply)
            await UpdateEditorFromCodeViewAsync();
    }

    void IDisposable.Dispose() => _throttledValueChanged.Dispose();
}