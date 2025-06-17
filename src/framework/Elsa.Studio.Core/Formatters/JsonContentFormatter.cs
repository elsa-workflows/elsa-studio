using Elsa.Studio.Contracts;
using System.Text.Json;

namespace Elsa.Studio.Formatters
{
    /// <summary>
    /// Provides a Json content formatter.
    /// </summary>
    public class JsonContentFormatter : IContentFormatter
    {
        /// <inheritdoc/>
        public string Name => "Json";

        /// <inheritdoc/>
        public bool CanFormat(object input)
        {
            if (input is not string str) return false;
            str = str.Trim();
            return str.StartsWith("{") || str.StartsWith("[");
        }

        /// <inheritdoc/>
        public string? ToText(object input)
        {
            try
            {
                string formatted = input is string str
                    ? JsonSerializer.Serialize(JsonDocument.Parse(str), new JsonSerializerOptions { WriteIndented = true })
                    : JsonSerializer.Serialize(input, new JsonSerializerOptions { WriteIndented = true });

                return formatted;
            }
            catch
            {
                return input?.ToString() ?? string.Empty;
            }
        }

        /// <inheritdoc/>
        public TabulatedContentFormat? ToTable(object input)
        {
            if (input is not string str) return null;

            try
            {
                using var doc = JsonDocument.Parse(str);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;

                var rows = new List<IReadOnlyList<string>>();
                var headers = new HashSet<string>();

                // First pass: collect all unique property names
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) return null;
                    foreach (var prop in item.EnumerateObject())
                    {
                        headers.Add(prop.Name);
                    }
                }

                var headerList = headers.ToList();

                // Second pass: build rows in header order
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var row = new List<string>();
                    foreach (var header in headerList)
                    {
                        if (item.TryGetProperty(header, out var val))
                            row.Add(JsonElementToString(val));
                        else
                            row.Add(""); // missing value
                    }
                    rows.Add(row);
                }

                return new TabulatedContentFormat
                {
                    Headers = headerList,
                    Rows = rows
                };
            }
            catch
            {
                return null;
            }
        }
        private string JsonElementToString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? "",
                JsonValueKind.Number => element.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "null",
                _ => JsonSerializer.Serialize(element)
            };
        }
    }
}