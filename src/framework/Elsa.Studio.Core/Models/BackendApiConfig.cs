using Elsa.Api.Client.Options;
using Elsa.Studio.Options;

namespace Elsa.Studio.Models;

public class BackendApiConfig
{
    public Action<ElsaClientBuilderOptions>? ConfigureHttpClientBuilder { get; set; }
    public Action<BackendOptions>? ConfigureBackendOptions { get; set; }
}