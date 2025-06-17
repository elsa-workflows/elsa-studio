using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Components
{
    public partial class ContentFormatter : ComponentBase
    {
        private int TabIndex = 0;
        private string selectedItem = "";
        private string? FormattedText;
        private TabulatedContentFormat? FormattedTable;
        private List<IContentFormatter> AvailableFormatters = new();

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

        [Inject] private IContentFormatProvider FormatProvider { get; set; } = null!;

        protected override void OnInitialized()
        {
            AvailableFormatters = FormatProvider.GetAll().ToList();
            var selected = FormatProvider.MatchOrDefault(DataPanelItem.Text);
            selectedItem = selected.Name;
            FormatUsing(selected);
        }

        private void OnFormatterChanged()
        {
            var formatter = AvailableFormatters.FirstOrDefault(f => f.Name == SelectedItem);
            if (formatter != null)
            {
                FormatUsing(formatter);
            }
        }

        private void FormatUsing(IContentFormatter formatter)
        {
            FormattedText = formatter.ToText(DataPanelItem.Text);
            FormattedTable = formatter.ToTable(DataPanelItem.Text);
            TabIndex = 0;
            StateHasChanged();
        }
    }
}