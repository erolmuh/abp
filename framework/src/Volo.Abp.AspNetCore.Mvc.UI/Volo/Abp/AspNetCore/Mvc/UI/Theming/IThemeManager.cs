using System;
using System.Threading.Tasks;

namespace Volo.Abp.AspNetCore.Mvc.UI.Theming;

public interface IThemeManager
{
    [Obsolete("Use GetCurrentThemeAsync instead.")]
    ITheme CurrentTheme { get; }

    Task<ITheme> GetCurrentThemeAsync();
}
