using Microsoft.Extensions.FileProviders;

namespace Volo.Abp.AspNetCore.Components.MauiBlazor;

public interface IMauiBlazorContentFileProvider : IFileProvider
{
    string ContentRootPath { get; }
}
