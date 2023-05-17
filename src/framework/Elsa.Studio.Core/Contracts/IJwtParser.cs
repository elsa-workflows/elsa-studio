using System.Security.Claims;

namespace Elsa.Studio.Contracts;

public interface IJwtParser
{
    IEnumerable<Claim> Parse(string jwt);
}