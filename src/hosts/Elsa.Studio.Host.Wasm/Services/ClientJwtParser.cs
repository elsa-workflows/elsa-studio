using System.Security.Claims;
using System.Text.Json;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Host.Wasm.Services;

public class ClientJwtParser : IJwtParser
{
    // Taken and adapted from: https://trystanwilcock.com/2022/09/28/net-6-0-blazor-webassembly-jwt-token-authentication-from-scratch-c-sharp-wasm-tutorial/
    public IEnumerable<Claim> Parse(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = DecodeBase64Url(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes)!;
        return keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!));
    }
    
    private static byte[] DecodeBase64Url(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }

        return Convert.FromBase64String(base64);
    }
}