using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Components.Web.BasicTheme.Themes.Basic;
using Volo.Abp.AspNetCore.Components.Web.Theming.Layout;
using Volo.Abp.AspNetCore.Components.Web.Theming.Theming;
using Volo.Abp.DependencyInjection;

namespace Volo.Abp.AspNetCore.Components.Web.BasicTheme;

[ThemeName(Name)]
public class BasicTheme : ITheme, ITransientDependency
{
    public const string Name = "Basic";

    [Obsolete("Use GetLayoutAsync instead.")]
    public virtual Type GetLayout(string name, bool fallbackToDefault = true)
    {
        switch (name)
        {
            case StandardLayouts.Application:
            case StandardLayouts.Account:
            case StandardLayouts.Empty:
                return typeof(MainLayout);
            default:
                return fallbackToDefault ? typeof(MainLayout) : typeof(NullLayout);
        }
    }

    public virtual Task<Type> GetLayoutAsync(string name, bool fallbackToDefault = true)
    {
        switch (name)
        {
            case StandardLayouts.Application:
            case StandardLayouts.Account:
            case StandardLayouts.Empty:
                return Task.FromResult(typeof(MainLayout));
            default:
                return Task.FromResult(fallbackToDefault ? typeof(MainLayout) : typeof(NullLayout));
        }
    }
}
