namespace Elsa.Studio.Backend.Models;

/// <summary>
/// Represent the backend.
/// </summary>
public class Backend
{
    public Backend(Uri url)
    {
        Url = url;
    }

    /// <summary>
    /// The URL of the backend.
    /// </summary>
    public Uri Url { get; }
}