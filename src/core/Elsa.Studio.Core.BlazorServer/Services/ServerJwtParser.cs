using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Core.BlazorServer.Services;

public class ServerJwtParser : IJwtParser
{
    public IEnumerable<Claim> Parse(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(jwt);
        
        return jwtSecurityToken.Claims;
    }
}