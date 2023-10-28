namespace Elsa.Studio.Contracts;

/// <summary>
/// Manages features.
/// </summary>
public interface IFeatureService
{
    /// <summary>
    /// Returns all features.
    /// </summary>
    IEnumerable<IFeature> GetFeatures();

    /// <summary>
    /// Initializes all features.
    /// </summary>
    Task InitializeFeaturesAsync(CancellationToken cancellationToken = default);
}