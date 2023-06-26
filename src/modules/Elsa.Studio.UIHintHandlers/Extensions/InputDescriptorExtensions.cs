using System.Text.Json;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.UIHintHandlers.Converters;
using Elsa.Studio.UIHintHandlers.Models;

namespace Elsa.Studio.UIHintHandlers.Extensions;

public static class InputDescriptorExtensions
{
    public static async Task<SelectList> GetSelectListAsync(this InputDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        var options = (JsonElement?)descriptor.Options;

        if (options == null || options.Value.ValueKind == JsonValueKind.Null)
            return new SelectList(new List<SelectListItem>(), false);

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        
        serializerOptions.Converters.Add(new SelectListJsonConverter());

        if(options.Value.ValueKind == JsonValueKind.Object)
        {
            if(options.Value.TryGetPropertySafe("items", out var items))
            {
                return items.Deserialize<SelectList>(serializerOptions)!;
            }

            if (options.Value.TryGetPropertySafe("provider", out var provider))
            {
                // TODO: Invoke remote provider                
                return new SelectList(new List<SelectListItem>(), false);
            }
        }

        if (options.Value.ValueKind == JsonValueKind.Array)
        {
            serializerOptions.Converters.Add(new SelectListItemJsonConverter());
            var items = options.Value.Deserialize<List<SelectListItem>>(serializerOptions)!;
            return new SelectList(items, false);
        }

        return new SelectList(new List<SelectListItem>(), false);
    }
}