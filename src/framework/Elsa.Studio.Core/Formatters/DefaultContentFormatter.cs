using Elsa.Studio.Contracts;

namespace Elsa.Studio.Formatters
{
    /// <summary>
    /// Provides a default implementation of <see cref="IContentFormatter"/>.
    /// </summary>
    public class DefaultContentFormatter : IContentFormatter
    {
        /// <inheritdoc/>
        public string Name => "Default";

        /// <inheritdoc/>
        public string Syntax => "text";

        /// <inheritdoc/>
        public bool CanFormat(object input) => true;

        /// <inheritdoc/>
        public string? ToText(object input)
        {
            return input?.ToString() ?? string.Empty;
        }

        /// <inheritdoc/>
        public TabulatedContentFormat? ToTable(object input) => null;
    }
}