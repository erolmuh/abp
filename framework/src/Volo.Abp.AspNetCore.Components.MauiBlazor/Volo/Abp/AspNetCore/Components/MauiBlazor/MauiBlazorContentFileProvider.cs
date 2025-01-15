using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Volo.Abp.DependencyInjection;
using Volo.Abp.VirtualFileSystem;

namespace Volo.Abp.AspNetCore.Components.MauiBlazor;

public class MauiBlazorContentFileProvider : IMauiBlazorContentFileProvider, ISingletonDependency
{
    private readonly IVirtualFileProvider _virtualFileProvider;
    private readonly IFileProvider _fileProvider;
    private string _rootPath = "/wwwroot";

    public MauiBlazorContentFileProvider(IVirtualFileProvider virtualFileProvider)
    {
        _virtualFileProvider = virtualFileProvider;
        _fileProvider = CreateFileProvider();
    }

    public string ContentRootPath => AppDomain.CurrentDomain.BaseDirectory;

    public IFileInfo GetFileInfo(string subpath)
    {
        if (string.IsNullOrEmpty(subpath))
        {
            return new NotFoundFileInfo(subpath);
        }

        var fileInfo = _fileProvider.GetFileInfo(subpath);
        return fileInfo.Exists ? fileInfo : _fileProvider.GetFileInfo( _rootPath + subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        if (string.IsNullOrEmpty(subpath))
        {
            return NotFoundDirectoryContents.Singleton;
        }

        var directory = _fileProvider.GetDirectoryContents(subpath);
        return directory.Exists ? directory : _fileProvider.GetDirectoryContents( _rootPath + subpath);
    }

    public IChangeToken Watch(string filter)
    {
        return new CompositeChangeToken(
            [
                _fileProvider.Watch(_rootPath + filter),
                _fileProvider.Watch(filter)
            ]
        );
    }

    protected virtual IFileProvider CreateFileProvider()
    {
        return new CompositeFileProvider(new PhysicalFileProvider(Path.Combine(ContentRootPath, _rootPath.TrimStart('/'))), _virtualFileProvider);
    }
}
