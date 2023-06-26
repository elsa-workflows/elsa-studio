namespace Elsa.Studio.UIHintHandlers.Models;

public record SelectList(ICollection<SelectListItem> Items, bool IsFlagsEnum);