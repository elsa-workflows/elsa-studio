namespace Elsa.Studio.DomInterop.Contracts;

public interface IFiles
{
    Task DownloadFileFromStreamAsync(string fileName, Stream stream);
}