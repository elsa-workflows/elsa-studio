using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;

/// <summary>
/// Presents browsable activities for a StateMachine transition slot.
/// </summary>
public partial class StateMachineActivityPickerDialog
{
    private readonly string _id = $"state-machine-activity-picker-{Guid.NewGuid():N}";
    private IReadOnlyList<ActivityDescriptor> _descriptors = [];
    private string _searchText = string.Empty;
    private Exception? _loadError;
    private bool _isLoading = true;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;

    private string HeadingId => $"{_id}-heading";
    private string SearchInputId => $"{_id}-search";

    private IEnumerable<ActivityDescriptor> FilteredDescriptors =>
        _descriptors.Where(MatchesSearch);

    private IEnumerable<IGrouping<string, ActivityDescriptor>> GroupedDescriptors =>
        FilteredDescriptors
            .Where(x => !IsPromotedSequence(x))
            .OrderBy(GetDisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.TypeName, StringComparer.OrdinalIgnoreCase)
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Category) ? Localizer["Other"].Value : x.Category, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase);

    private ActivityDescriptor? SequenceDescriptor =>
        _descriptors.FirstOrDefault(x => string.Equals(x.TypeName, "Elsa.Sequence", StringComparison.Ordinal));

    private bool IsPromotedSequence(ActivityDescriptor descriptor) =>
        SequenceDescriptor != null && string.IsNullOrWhiteSpace(_searchText) && ReferenceEquals(descriptor, SequenceDescriptor);

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ActivityRegistry.EnsureLoadedAsync();
            var descriptors = ActivityRegistry.List().ToList();
            var browsableDescriptors = ActivityRegistry.ListBrowsable().ToList();

            // Sequence is the editor's composition escape hatch. The backend may mark it
            // non-browsable because it is normally opened through a designer, but it must
            // still be available as the explicit multi-activity shortcut here.
            var sequence = descriptors.FirstOrDefault(x => string.Equals(x.TypeName, "Elsa.Sequence", StringComparison.Ordinal))
                           ?? ActivityRegistry.Find("Elsa.Sequence");
            if (sequence != null && browsableDescriptors.All(x => !string.Equals(x.TypeName, sequence.TypeName, StringComparison.Ordinal)))
                browsableDescriptors.Add(sequence);

            _descriptors = browsableDescriptors
                .OrderBy(GetDisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.TypeName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception exception)
        {
            _loadError = exception;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private bool MatchesSearch(ActivityDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(_searchText))
            return true;

        var search = _searchText.Trim();
        return Contains(GetDisplayName(descriptor), search)
               || Contains(descriptor.Name, search)
               || Contains(descriptor.TypeName, search)
               || Contains(descriptor.Category, search)
               || Contains(descriptor.Description, search);
    }

    private static bool Contains(string? value, string search) =>
        value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;

    private string GetDisplayName(ActivityDescriptor descriptor) =>
        Localizer[descriptor.DisplayName ?? descriptor.Name].Value;

    private string DisplayCategory(string category) => Localizer[category].Value;

    private string GetGroupId(string category) => $"{_id}-group-{SanitizeId(category)}";

    private static string SanitizeId(string value) =>
        new(value.Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray());

    private Task SelectAsync(ActivityDescriptor descriptor)
    {
        MudDialog.Close(DialogResult.Ok(descriptor));
        return Task.CompletedTask;
    }

    private void Cancel() => MudDialog.Cancel();
}
