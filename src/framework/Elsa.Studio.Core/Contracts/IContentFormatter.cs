namespace Elsa.Studio.Contracts
{
    /// <summary>
    /// Defines a contract for formatting objects into formatted representations.
    /// </summary>
    public interface IContentFormatter
    {
        /// <summary>
        /// Gets the name of the content formatter.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Determines whether the content can be formatted with this formatter.
        /// </summary>
        /// <param name="input">The object to evaluate for formatting capability. Cannot be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the input can be formatted; otherwise, <see langword="false"/>.</returns>
        bool CanFormat(object input);

        /// <summary>
        /// Returns a prettified text version of the input.
        /// </summary>
        string? ToText(object input);

        /// <summary>
        /// Returns tabular representation of the input.
        /// Returns <see langword="null"/> if not supported.
        /// </summary>
        TabulatedContentFormat? ToTable(object input);
    }
}