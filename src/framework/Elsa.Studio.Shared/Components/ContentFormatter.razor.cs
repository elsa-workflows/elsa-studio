using BlazorMonaco.Editor;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Formatters;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Elsa.Studio.Components
{
    public partial class ContentFormatter : ComponentBase
    {
        private int TabIndex = 0;
        private bool isReadOnly = true;
        private string selectedItem = "";
        private string? FormattedText;
        private TabulatedContentFormat? FormattedTable;
        private IContentFormatter SelectedFormatter = new DefaultContentFormatter();
        private List<IContentFormatter> AvailableFormatters = new();
        private readonly string _monacoEditorId = $"monaco-editor-{Guid.NewGuid()}:N";
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

        [Inject] private IContentFormatProvider FormatProvider { get; set; } = null!;

        [Inject] private IClipboard Clipboard { get; set; } = null!;

        [Inject] private ISnackbar Snackbar { get; set; } = null!;

        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;


        protected override void OnInitialized()
        {
            AvailableFormatters = FormatProvider.GetAll().ToList();
            SelectedFormatter = FormatProvider.MatchOrDefault(DataPanelItem.Text);
            selectedItem = SelectedFormatter.Name;
            FormatUsing(SelectedFormatter);
        }

        private StandaloneEditorConstructionOptions ConfigureMonacoEditor(StandaloneCodeEditor editor)
        {
            return new StandaloneEditorConstructionOptions
            {
                Language = SelectedFormatter.Syntax,
                Value = FormattedText,
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
            var formatter = AvailableFormatters.FirstOrDefault(f => f.Name == SelectedItem);
            if (formatter != null)
            {
                FormatUsing(formatter);

                var model = await _monacoEditor!.GetModel();
                await model.SetValue(FormattedText);
                await Global.SetModelLanguage(JSRuntime, model, SelectedFormatter.Syntax);
            }
        }

        private void FormatUsing(IContentFormatter formatter)
        {
            SelectedFormatter = formatter;
            FormattedText = formatter.ToText(DataPanelItem.Text);
            FormattedTable = formatter.ToTable(DataPanelItem.Text);
            TabIndex = 0;
            StateHasChanged();
        }

        private async Task OnCopyClicked()
        {
            await Clipboard.CopyText(FormattedText ?? DataPanelItem.Text);
            Snackbar.Add($"{DataPanelItem.Label} copied", Severity.Success);
        }

        private void OnClosedClicked()
        {
            MudDialog.Cancel();
        }
    }
}