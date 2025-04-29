using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Theming;
using Volo.Abp.DependencyInjection;

namespace Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic;

[ThemeName(Name)]
public class BasicTheme : ITheme, ITransientDependency
{
    public const string Name = "Basic";

    [Obsolete("Use GetLayoutAsync instead.")]
    public virtual string GetLayout(string name, bool fallbackToDefault = true)
    {
        switch (name)
        {
            case StandardLayouts.Application:
                return "~/Themes/Basic/Layouts/Application.cshtml";
            case StandardLayouts.Account:
                return "~/Themes/Basic/Layouts/Account.cshtml";
            case StandardLayouts.Empty:
                return "~/Themes/Basic/Layouts/Empty.cshtml";
            default:
                return fallbackToDefault ? "~/Themes/Basic/Layouts/Application.cshtml" : null;
        }
    }

    public virtual Task<string> GetLayoutAsync(string name, bool fallbackToDefault = true)
    {
        switch (name)
        {
            case StandardLayouts.Application:
                return Task.FromResult("~/Themes/Basic/Layouts/Application.cshtml");
            case StandardLayouts.Account:
                return Task.FromResult("~/Themes/Basic/Layouts/Account.cshtml");
            case StandardLayouts.Empty:
                return Task.FromResult("~/Themes/Basic/Layouts/Empty.cshtml");
            default:
                return Task.FromResult(fallbackToDefault ? "~/Themes/Basic/Layouts/Application.cshtml" : null);
        }
    }
}
