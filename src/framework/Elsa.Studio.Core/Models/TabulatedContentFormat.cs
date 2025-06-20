/// <summary>
/// Represents a tabulated content format consisting of headers and rows.
/// </summary>
public class TabulatedContentFormat
{
    /// <summary>
    /// Gets or sets the collection of headers associated with the content.
    /// </summary>
    public IReadOnlyList<string> Headers { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of content rows, where each row is represented as a read-only list of strings.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; set; } = [];
}