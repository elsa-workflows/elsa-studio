using System.Text.Json;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.UIHints.Converters;
using Elsa.Studio.UIHints.Models;

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
        var options = descriptor.Options;

        var selectListOptions = options?.TryGetValue("items", out var selectList) == true
            ? selectList is JsonElement list
                ? list
                : default
            : default;

        if (options == null || selectListOptions.ValueKind == JsonValueKind.Null)
            return new SelectList(new List<SelectListItem>(), false);

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        serializerOptions.Converters.Add(new SelectListJsonConverter());

        if (selectListOptions.ValueKind == JsonValueKind.Object)
        {
            if (selectListOptions.TryGetPropertySafe("items", out var items))
            {
                return new SelectList(items.Deserialize<List<SelectListItem>>(serializerOptions)!, false);
            }

            if (selectListOptions.TryGetPropertySafe("provider", out var provider))
            {
                // TODO: Invoke remote provider                
                return new SelectList(new List<SelectListItem>(), false);
            }
        }

        if (selectListOptions.ValueKind == JsonValueKind.Array)
        {
            serializerOptions.Converters.Add(new SelectListItemJsonConverter());
            var items = selectListOptions.Deserialize<List<SelectListItem>>(serializerOptions)!;
            return new SelectList(items, false);
        }

        return new SelectList(new List<SelectListItem>(), false);
    }
}
