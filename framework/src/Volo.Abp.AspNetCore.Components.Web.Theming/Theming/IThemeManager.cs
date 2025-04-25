using System;
using System.Threading.Tasks;

namespace Volo.Abp.AspNetCore.Components.Web.Theming.Theming;

public interface IThemeManager
{
    [Obsolete("Use GetCurrentThemeAsync instead.")]
    ITheme CurrentTheme { get; }

    Task<ITheme> GetCurrentThemeAsync();
}
