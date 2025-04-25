using System;
using System.Threading.Tasks;

namespace Volo.Abp.AspNetCore.Components.Web.Theming.Theming;

public interface IThemeSelector
{
    [Obsolete("Use GetCurrentThemeInfoAsync instead.")]
    ThemeInfo GetCurrentThemeInfo();

    Task<ThemeInfo> GetCurrentThemeInfoAsync();
}
