using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Elsa.Studio.Components
{
    public partial class CodeEditorDialog
    {
        private StandaloneCodeEditor? _monacoEditor = null!;
        private bool _isInternalContentChange;
        private string _monacoEditorId = $"monaco-editor-{Guid.NewGuid()}:N";
        private string? _lastMonacoEditorContent;
        private string _initialValue;
        [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

        [Parameter] public string Value { get; set; } = null!;
        [Parameter] public string Label { get; set; } = null!;
        [Parameter] public string HelperText { get; set; } = null!;
        [Parameter] public string LanguageLabel { get; set; } = null!;
        [Parameter] public string MonacoLanguage { get; set; } = null!;

        private bool canSave => _lastMonacoEditorContent != _initialValue;

        private async Task OnMonacoInitializedAsync()
        {
            _initialValue = Value;
            _isInternalContentChange = true;
            var model = await _monacoEditor!.GetModel();
            _lastMonacoEditorContent = Value;
            await model.SetValue(Value);
            _isInternalContentChange = false;
            await Global.SetModelLanguage(JSRuntime, model, MonacoLanguage);
        }

        private StandaloneEditorConstructionOptions ConfigureMonacoEditor(StandaloneCodeEditor editor)
        {
            return new()
            {
                Language = MonacoLanguage,
                Value = Value,
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
            if (_isInternalContentChange)
                return;

            var value = await _monacoEditor!.GetValue();
            if (value == _lastMonacoEditorContent)
                return;

            Value = value;
        }

        private void OnSaveClicked() => MudDialog.Close(DialogResult.Ok(Value));
        private async Task OnClosedClicked()
        {
            if (Value != _initialValue)
            {
                bool? result = await DialogService.ShowMessageBox(
                    "Discard Changes",
                    "Are you sure you want to discard changes?",
                    yesText: "Yes", cancelText: "Cancel");
                if (result != true) return;
            }
            MudDialog.Close();
        }
    }
}