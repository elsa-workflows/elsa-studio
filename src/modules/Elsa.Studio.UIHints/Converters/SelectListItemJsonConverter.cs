using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Studio.UIHints.Models;

namespace Elsa.Studio.UIHints.Converters;

public class SelectListItemJsonConverter : JsonConverter<SelectListItem>
{
    public override SelectListItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
            {
                var newOptions = new JsonSerializerOptions(options);
                newOptions.Converters.Remove(this);
                var result = JsonSerializer.Deserialize<SelectListItem>(ref reader, newOptions)!;
                return result;
            }
                
            case JsonTokenType.String:
            {
                var stringValue = reader.GetString()!;
                return new SelectListItem(stringValue, stringValue);
            }
            default:
                throw new JsonException($"Unexpected token {reader.TokenType}");
        }
    }

    public override void Write(Utf8JsonWriter writer, SelectListItem value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
