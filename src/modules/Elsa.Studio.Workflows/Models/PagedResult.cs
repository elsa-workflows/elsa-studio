namespace Elsa.Studio.Workflows.Models;

public record PagedResult<T>(ICollection<T> Items, long TotalCount);