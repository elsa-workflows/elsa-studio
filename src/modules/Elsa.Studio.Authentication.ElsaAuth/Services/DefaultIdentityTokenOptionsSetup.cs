using Elsa.Studio.Authentication.ElsaAuth.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.ElsaAuth.Services;

/// <summary>
/// Provides default values for <see cref="IdentityTokenOptions"/>.
/// </summary>
public class DefaultIdentityTokenOptionsSetup : IConfigureOptions<IdentityTokenOptions>
{
    /// <inheritdoc />
    public void Configure(IdentityTokenOptions options)
    {
        options.NameClaimType ??= "name";
        options.RoleClaimType ??= "role";
    }
}

