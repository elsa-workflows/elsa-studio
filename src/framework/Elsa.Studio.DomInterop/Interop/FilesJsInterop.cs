using Elsa.Studio.DomInterop.Contracts;
using Microsoft.JSInterop;

namespace Elsa.Studio.DomInterop.Interop;

public class FilesJsInterop : JsInteropBase, IFiles
{
    public FilesJsInterop(IJSRuntime jsRuntime) : base(jsRuntime)
    {
    }

    protected override string ModuleName => "download";

    public async Task DownloadFileFromStreamAsync(string fileName, Stream stream)
    {
        var streamRef = new DotNetStreamReference(stream);
        await InvokeAsync(async module => await module.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef));
    }
}