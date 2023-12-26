using System.Text.Json;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Shared.UIHints.CheckList;
using Elsa.Api.Client.Shared.UIHints.DropDown;

namespace Elsa.Studio.UIHints.Extensions;

/// <summary>
/// Provides extension methods for <see cref="InputDescriptor"/>.
/// </summary>
public static class InputDescriptorExtensions
{
    /// <summary>
    /// Gets the <see cref="SelectList"/> for the specified <see cref="InputDescriptor"/>.
    /// </summary>
    public static SelectList GetSelectList(this InputDescriptor descriptor)
    {
        var specifications = descriptor.UISpecifications;
        var props = specifications != null ? specifications.TryGetValue("dropdown", out var propsValue) ? propsValue is JsonElement value ? value : default : default : default;

        if (props.ValueKind == JsonValueKind.Undefined)
            return new SelectList(new List<SelectListItem>());

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var dropDownProps = props.Deserialize<DropDownProps>(serializerOptions);
        return dropDownProps?.SelectList ?? new SelectList(new List<SelectListItem>(), false);
    }

    /// <summary>
    /// Gets a list of <see cref="CheckListItem"/>s for the specified <see cref="InputDescriptor"/>.
    /// </summary>
    public static CheckList GetCheckList(this InputDescriptor descriptor)
    {
        var specifications = descriptor.UISpecifications;
        var props = specifications != null ? specifications.TryGetValue("checklist", out var propsValue) ? propsValue is JsonElement value ? value : default : default : default;

        if (props.ValueKind == JsonValueKind.Undefined)
            return new CheckList(Array.Empty<CheckListItem>());

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var checkListProps = props.Deserialize<CheckListProps>(serializerOptions);
        return checkListProps?.CheckList ?? new CheckList(Array.Empty<CheckListItem>());
    }
}