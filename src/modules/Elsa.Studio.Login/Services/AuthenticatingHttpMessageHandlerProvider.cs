using Elsa.Api.Client.Contracts;
using Elsa.Studio.Login.HttpMessageHandlers;

namespace Elsa.Studio.Login.Services;

/// <summary>
/// An <see cref="IHttpMessageHandlerProvider"/> that provides an <see cref="AuthenticatingApiHttpMessageHandler"/>.
/// </summary>
public class AuthenticatingHttpMessageHandlerProvider : IHttpMessageHandlerProvider
{
    private readonly AuthenticatingApiHttpMessageHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticatingHttpMessageHandlerProvider"/> class.
    /// </summary>
    public AuthenticatingHttpMessageHandlerProvider(AuthenticatingApiHttpMessageHandler handler)
    {
        _handler = handler;
    }

    /// <inheritdoc />
    public HttpMessageHandler GetHandler() => _handler;
}