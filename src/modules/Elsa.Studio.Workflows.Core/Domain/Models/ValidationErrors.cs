namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Represents a collection of validation errors.
/// </summary>
public record ValidationErrors(IReadOnlyCollection<ValidationError> Errors);