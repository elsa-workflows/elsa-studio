using Elsa.Studio.Authentication.ElsaAuth.Contracts;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;

namespace Elsa.Studio.Authentication.ElsaAuth.BlazorWasm.Services;

/// <inheritdoc />
public class BlazorWasmJwtParser : IJwtParser
{
    /// <inheritdoc />
    public IEnumerable<Claim> Parse(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return Array.Empty<Claim>();

        var parts = jwt.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return Array.Empty<Claim>();

        JsonNode? root;

        try
        {
            var payloadJson = DecodeBase64UrlToString(parts[1]);
            root = JsonNode.Parse(payloadJson);
        }
        catch
        {
            return Array.Empty<Claim>();
        }

        if (root is not JsonObject obj)
            return Array.Empty<Claim>();

        var claims = new List<Claim>();

        foreach (var (key, value) in obj)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null)
                continue;

            switch (value)
            {
                case JsonArray array:
                    foreach (var item in array)
                    {
                        if (item != null)
                            claims.Add(new Claim(key, item.ToString()));
                    }

                    break;

                case JsonObject nestedObj:
                    // Preserve nested objects as JSON.
                    claims.Add(new Claim(key, nestedObj.ToJsonString()));
                    break;

                default:
                    claims.Add(new Claim(key, value.ToString()));
                    break;
            }
        }

        return claims;
    }

    private static string DecodeBase64UrlToString(string base64Url)
    {
        // base64url -> base64
        var padded = base64Url.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        var bytes = Convert.FromBase64String(padded);
        return Encoding.UTF8.GetString(bytes);
    }
}
