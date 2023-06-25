using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Studio.UIHintHandlers.Models;

namespace Elsa.Studio.UIHintHandlers.Converters;

public class SelectListJsonConverter : JsonConverter<SelectList>
{
    public override SelectList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var isFlagsEnum = false;
        var items = new List<SelectListItem>();

        if (reader.TokenType != JsonTokenType.StartObject) 
            return new SelectList(items, isFlagsEnum);
        
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName) 
                continue;
            
            var propName = reader.GetString();
            reader.Read(); // Move to the next token, which is the value

            switch (propName)
            {
                case "isFlagsEnum":
                    isFlagsEnum = reader.GetBoolean();
                    break;
                case "items":
                {
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        reader.Read(); // Read the first element of the array

                        switch (reader.TokenType)
                        {
                            case JsonTokenType.StartObject:
                                items = JsonSerializer.Deserialize<List<SelectListItem>>(ref reader, options)!;
                                break;
                            case JsonTokenType.String:
                            {
                                var stringItems = JsonSerializer.Deserialize<List<string>>(ref reader, options)!;
                                items = stringItems.Select(s => new SelectListItem(s, s)).ToList();
                                break;
                            }
                        }
                    }

                    break;
                }
            }
        }

        return new SelectList(items, isFlagsEnum);
    }

    public override void Write(Utf8JsonWriter writer, SelectList value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
