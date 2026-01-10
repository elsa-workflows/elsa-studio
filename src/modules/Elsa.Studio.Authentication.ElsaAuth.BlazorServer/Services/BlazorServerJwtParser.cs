using Elsa.Studio.Authentication.ElsaAuth.Contracts;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Elsa.Studio.Authentication.ElsaAuth.BlazorServer.Services;

/// <inheritdoc />
public class BlazorServerJwtParser : IJwtParser
{
    /// <inheritdoc />
    public IEnumerable<Claim> Parse(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return Array.Empty<Claim>();

        var parts = jwt.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return Array.Empty<Claim>();

        var payloadJson = DecodeBase64UrlToString(parts[1]);

        using var document = JsonDocument.Parse(payloadJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            return Array.Empty<Claim>();

        var claims = new List<Claim>();

        foreach (var property in document.RootElement.EnumerateObject())
            AddClaimsFromJson(property.Name, property.Value, claims);

        return claims;
    }

    private static void AddClaimsFromJson(string type, JsonElement value, ICollection<Claim> claims)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return;

            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                    AddClaimsFromJson(type, item, claims);
                return;

            case JsonValueKind.Object:
                // For nested objects, store the raw JSON.
                claims.Add(new Claim(type, value.GetRawText(), ClaimValueTypes.String));
                return;

            case JsonValueKind.True:
            case JsonValueKind.False:
                claims.Add(new Claim(type, value.GetBoolean() ? "true" : "false", ClaimValueTypes.Boolean));
                return;

            case JsonValueKind.Number:
                // Preserve as string; callers can interpret.
                claims.Add(new Claim(type, value.GetRawText(), ClaimValueTypes.String));
                return;

            case JsonValueKind.String:
                claims.Add(new Claim(type, value.GetString() ?? string.Empty, ClaimValueTypes.String));
                return;

            default:
                claims.Add(new Claim(type, value.ToString(), ClaimValueTypes.String));
                return;
        }
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
