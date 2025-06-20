namespace Elsa.Studio.Contracts
{
    /// <summary>
    /// Provides content providers.
    /// </summary>
    public interface IContentFormatProvider
    {
        /// <summary>
        /// Attempts to resolve the best matching formatter for the input.
        /// </summary>
        /// <param name="content">The content to check formats against.</param>
        /// <returns></returns>
        IContentFormatter MatchOrDefault(object content);

        /// <summary>
        /// Returns all content formatters.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IContentFormatter> GetAll();
    }
}