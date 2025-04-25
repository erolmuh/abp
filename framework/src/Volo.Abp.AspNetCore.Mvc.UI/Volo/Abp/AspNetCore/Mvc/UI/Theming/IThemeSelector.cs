using System;
using System.Threading.Tasks;

namespace Volo.Abp.AspNetCore.Mvc.UI.Theming;

public interface IThemeSelector
{
    [Obsolete("Use GetCurrentThemeInfoAsync instead.")]
    ThemeInfo GetCurrentThemeInfo();

    Task<ThemeInfo> GetCurrentThemeInfoAsync();
}
