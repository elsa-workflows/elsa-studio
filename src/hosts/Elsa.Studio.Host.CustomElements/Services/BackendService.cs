namespace Elsa.Studio.Host.CustomElements.Services;

public class BackendService
{
    public string RemoteEndpoint { get; set; } = default!;
    public string? ApiKey { get; set; }
    public string? AccessToken { get; set; }
}