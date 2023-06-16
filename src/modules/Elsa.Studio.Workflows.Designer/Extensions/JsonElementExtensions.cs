using System.Text.Json;

namespace Elsa.Studio.Workflows.Designer.Extensions;

public static class JsonElementExtensions
{
    public static bool TryGetPropertySafe(this JsonElement element, string[] propertyPath, out JsonElement value)
    {
        value = default;
        var currentElement = element;

        foreach (var propertyName in propertyPath)
        {
            if(!currentElement.TryGetProperty(propertyName, out currentElement))
                return false;
        }

        if(currentElement.ValueKind == JsonValueKind.Undefined)
            return false;
        
        value = currentElement;
        return true;
    }
    
    public static bool TryGetPropertySafe(this JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;

        if(element.ValueKind != JsonValueKind.Object)
            return false;
        
        if (!element.TryGetProperty(propertyName, out var property))
            return false;

        value = property;
        return true;
    }
    
    public static bool TryGetDoubleSafe(this JsonElement element, string propertyName, out double value)
    {
        value = 0;
        
        if(element.ValueKind != JsonValueKind.Object)
            return false;

        if (!element.TryGetProperty(propertyName, out var property))
            return false;

        if (property.ValueKind != JsonValueKind.Number)
            return false;

        value = property.GetDouble();
        return true;
    }
}