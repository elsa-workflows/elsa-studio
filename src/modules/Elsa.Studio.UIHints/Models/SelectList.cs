namespace Elsa.Studio.UIHints.Models;

public record SelectList(ICollection<SelectListItem> Items, bool IsFlagsEnum);