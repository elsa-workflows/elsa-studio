using System.Text.Json.Serialization;
using Elsa.Studio.UIHintHandlers.Converters;

namespace Elsa.Studio.UIHintHandlers.Models;

[JsonConverter(typeof(SelectListJsonConverter))]
public record SelectList(ICollection<SelectListItem> Items, bool IsFlagsEnum);