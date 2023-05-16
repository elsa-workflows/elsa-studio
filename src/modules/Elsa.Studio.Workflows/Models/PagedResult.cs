namespace Elsa.Studio.Workflows.Contracts;

public record PagedResult<T>(ICollection<T> Items, long TotalCount);