using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.ActivityPickers.Accordion;

/// <summary>
/// Represents a provider for an Accordion-based activity picker component.
/// </summary>
public class AccordionActivityPickerComponentProvider : IActivityPickerComponentProvider
{
    /// <inheritdoc />
    public RenderFragment GetActivityPickerComponent() => builder =>
    {
        builder.OpenComponent<ActivityPicker>(0);
        builder.CloseComponent();
    };
}