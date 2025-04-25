using System;
using System.Threading.Tasks;

namespace Volo.Abp.AspNetCore.Mvc.UI.Theming;

public static class ThemeExtensions
{
    [Obsolete("Use GetApplicationLayoutAsync instead.")]
    public static string GetApplicationLayout(this ITheme theme, bool fallbackToDefault = true)
    {
        return theme.GetLayout(StandardLayouts.Application, fallbackToDefault);
    }

    [Obsolete("Use GetAccountLayoutAsync instead.")]
    public static string GetAccountLayout(this ITheme theme, bool fallbackToDefault = true)
    {
        return theme.GetLayout(StandardLayouts.Account, fallbackToDefault);
    }

    [Obsolete("Use GetPublicLayoutAsync instead.")]
    public static string GetPublicLayout(this ITheme theme, bool fallbackToDefault = true)
    {
        return theme.GetLayout(StandardLayouts.Public, fallbackToDefault);
    }

    [Obsolete("Use GetEmptyLayoutAsync instead.")]
    public static string GetEmptyLayout(this ITheme theme, bool fallbackToDefault = true)
    {
        return theme.GetLayout(StandardLayouts.Empty, fallbackToDefault);
    }

    public async static Task<string> GetApplicationLayoutAsync(this ITheme theme, bool fallbackToDefault = true)
    {
        return await theme.GetLayoutAsync(StandardLayouts.Application, fallbackToDefault);
    }

    public async static Task<string> GetAccountLayoutAsync(this ITheme theme, bool fallbackToDefault = true)
    {
        return await theme.GetLayoutAsync(StandardLayouts.Account, fallbackToDefault);
    }

    public async static Task<string> GetPublicLayoutAsync(this ITheme theme, bool fallbackToDefault = true)
    {
        return await theme.GetLayoutAsync(StandardLayouts.Public, fallbackToDefault);
    }

    public async static Task<string> GetEmptyLayoutAsync(this ITheme theme, bool fallbackToDefault = true)
    {
        return await theme.GetLayoutAsync(StandardLayouts.Empty, fallbackToDefault);
    }

    public async static Task<string> GetCurrentApplicationLayoutAsync(this IThemeManager themeManager, bool fallbackToDefault = true)
    {
        return await (await themeManager.GetCurrentThemeAsync()).GetLayoutAsync(StandardLayouts.Application, fallbackToDefault);
    }

    public async static Task<string> GetCurrentAccountLayoutAsync(this IThemeManager themeManager, bool fallbackToDefault = true)
    {
        return await (await themeManager.GetCurrentThemeAsync()).GetLayoutAsync(StandardLayouts.Account, fallbackToDefault);
    }

    public async static Task<string> GetCurrentPublicLayoutAsync(this IThemeManager themeManager, bool fallbackToDefault = true)
    {
        return await (await themeManager.GetCurrentThemeAsync()).GetLayoutAsync(StandardLayouts.Public, fallbackToDefault);
    }

    public async static Task<string> GetCurrentEmptyLayoutAsync(this IThemeManager themeManager, bool fallbackToDefault = true)
    {
        return await (await themeManager.GetCurrentThemeAsync()).GetLayoutAsync(StandardLayouts.Empty, fallbackToDefault);
    }
}
