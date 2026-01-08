namespace Elsa.Studio.Login.Contracts;

public record PkceData(string CodeVerifier, string CodeChallenge, string Method);