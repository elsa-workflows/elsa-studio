using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Elsa.Studio.Login.Contracts;

public class OpenIdConnectPkceService : IOpenIdConnectPkceService
{
    public ValueTask<PkceData> GeneratePkceAsync()
    {
        // 32 bytes -> 43 chars Base64Url (no padding): within RFC 7636 limits.
        var verifierBytes = RandomNumberGenerator.GetBytes(32);
        var codeVerifier = WebEncoders.Base64UrlEncode(verifierBytes);

        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        var codeChallenge = WebEncoders.Base64UrlEncode(challengeBytes);

        return ValueTask.FromResult(new PkceData(
            CodeVerifier: codeVerifier,
            CodeChallenge: codeChallenge,
            Method: "S256"));
    }
}