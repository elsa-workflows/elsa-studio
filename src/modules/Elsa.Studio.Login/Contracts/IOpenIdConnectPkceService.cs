namespace Elsa.Studio.Login.Contracts;

public interface IOpenIdConnectPkceService
{
    ValueTask<PkceData> GeneratePkceAsync();
}