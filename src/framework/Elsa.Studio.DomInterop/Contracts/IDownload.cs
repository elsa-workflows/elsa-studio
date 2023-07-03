namespace Elsa.Studio.DomInterop.Contracts;

public interface IDownload
{
    Task DownloadFileFromStreamAsync(string fileName, Stream stream);
}