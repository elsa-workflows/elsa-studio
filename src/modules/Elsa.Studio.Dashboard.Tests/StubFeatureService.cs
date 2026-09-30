using Elsa.Studio.Contracts;

namespace Elsa.Studio.Dashboard.Tests;

/// <summary>A feature service that reports it is initialized once <see cref="CompleteInitialization"/> is called.</summary>
internal sealed class StubFeatureService : IFeatureService
{
    public event Action? Initialized;
    public bool IsInitialized { get; private set; }
    public IEnumerable<IFeature> GetFeatures() => [];
    public Task InitializeFeaturesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void CompleteInitialization()
    {
        IsInitialized = true;
        Initialized?.Invoke();
    }
}
