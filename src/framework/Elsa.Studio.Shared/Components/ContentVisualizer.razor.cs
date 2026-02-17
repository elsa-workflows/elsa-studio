using BlazorMonaco.Editor;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Visualizers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Elsa.Studio.Components
{
    /// <summary>
    /// Represents the content visualizer.
    /// </summary>
    public partial class ContentVisualizer : ComponentBase
    {
        private int TabIndex = 0;
        private bool isReadOnly = true;
        private string selectedItem = "";
        private string? Pretty;
        private TabulatedContentVisualizer? Table;
        private IContentVisualizer SelectedVisualizer = new DefaultContentVisualizer();
        private List<IContentVisualizer> AvailableVisualizers = new();
        private readonly string _monacoEditorId = $"monaco-editor-{Guid.NewGuid():N}";
        private StandaloneCodeEditor? _monacoEditor;

        public bool IsReadOnly
        {
            get { return isReadOnly; }
            set
            {
                isReadOnly = value;
                _monacoEditor.UpdateOptions(
                    new EditorUpdateOptions
                    {
                        ReadOnly = value
                    });
            }
        }

        /// <summary>
        /// Provides the selected item.
        /// </summary>
        public string SelectedItem
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;
                OnFormatterChanged();
            }
        }

        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

        [Parameter] public DataPanelItem DataPanelItem { get; set; } = null!;

        [Inject] private ILocalizer Localizer { get; set; } = null!;
        [Inject] private IContentVisualizerProvider VisualizationProvider { get; set; } = null!;
        [Inject] private IClipboard Clipboard { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        /// <summary>
        /// Performs the on initialized operation.
        /// </summary>
        protected override void OnInitialized()
        {
            AvailableVisualizers = VisualizationProvider.GetAll().ToList();
            SelectedVisualizer = VisualizationProvider.MatchOrDefault(DataPanelItem.Text);
            selectedItem = SelectedVisualizer.Name;
            FormatUsing(SelectedVisualizer);
        }

        private StandaloneEditorConstructionOptions ConfigureMonacoEditor(StandaloneCodeEditor editor)
        {
            return new StandaloneEditorConstructionOptions
            {
                Language = SelectedVisualizer.Syntax,
                Value = Pretty,
                FontFamily = "Roboto Mono, monospace",
                RenderLineHighlight = "none",
                Minimap = new EditorMinimapOptions
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
                ReadOnly = IsReadOnly,
                DomReadOnly = IsReadOnly
            };
        }

        private async Task OnFormatterChanged()
        {
            var visualizer = AvailableVisualizers.FirstOrDefault(f => f.Name == SelectedItem);
            if (visualizer != null)
            {
                FormatUsing(visualizer);

                try
                {
                    var model = await _monacoEditor!.GetModel();
                    await model.SetValue(Pretty);
                    await Global.SetModelLanguage(JSRuntime, model, SelectedVisualizer.Syntax);
                }
                catch (Microsoft.JSInterop.JSException ex) when (ex.Message.Contains("Couldn't find the editor"))
                {
                    // This can happen when the component is being disposed while the Monaco editor is initializing.
                    // We can safely ignore this error as the component is being recreated anyway.
                }
            }
        }

        private void FormatUsing(IContentVisualizer visualizer)
        {
            SelectedVisualizer = visualizer;
            Pretty = visualizer.ToPretty(DataPanelItem.Text);
            Table = visualizer.ToTable(DataPanelItem.Text);
            TabIndex = 0;
            StateHasChanged();
        }

        private async Task OnCopyClicked()
        {
            await Clipboard.CopyText(Pretty ?? DataPanelItem.Text);
            Snackbar.Add($"{DataPanelItem.Label} copied", Severity.Success);
        }

        private void OnClosedClicked()
        {
            MudDialog.Cancel();
        }
    }
}