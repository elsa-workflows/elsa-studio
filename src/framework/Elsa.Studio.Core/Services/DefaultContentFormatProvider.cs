using Elsa.Studio.Contracts;
using Elsa.Studio.Formatters;

namespace Elsa.Studio.Core.Services
{
    /// <summary>
    /// The default content format provider.
    /// </summary>
    public class DefaultContentFormatProvider : IContentFormatProvider
    {
        private readonly List<IContentFormatter> _registeredFormatters;
        private readonly IContentFormatter _defaultFormatter = new DefaultContentFormatter();

        /// <summary>
        /// Initializes a new default instance of the <see cref="DefaultContentFormatProvider"/>.
        /// </summary>
        public DefaultContentFormatProvider(IEnumerable<IContentFormatter> formatters) => _registeredFormatters = formatters.ToList();

        /// <summary>
        /// Attempts to resolve the best matching formatter for the input.
        /// Defaults to <see cref="DefaultContentFormatter"/> formatter if none match.
        /// </summary>
        public IContentFormatter MatchOrDefault(object input) => _registeredFormatters.FirstOrDefault(f => f.CanFormat(input)) ?? _defaultFormatter;

        /// <summary>
        /// Returns all registered formatters, including the default formatter.
        /// </summary>
        public IEnumerable<IContentFormatter> GetAll() => _registeredFormatters.Concat([_defaultFormatter]);
    }
}