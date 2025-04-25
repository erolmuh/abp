using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Components.Web.Theming.Layout;

namespace Volo.Abp.AspNetCore.Components.Web.Theming.Theming;

public static class ThemeExtensions
{
    [Obsolete("Use GetApplicationLayoutAsync instead.")]
    public static Type GetApplicationLayout(this ITheme theme, bool fallbackToDefault = true)
    {
        return theme.GetLayout(StandardLayouts.Application, fallbackToDefault);
    }

    [Obsolete("Use GetAccountLayoutAsync instead.")]
    public static Type GetAccountLayout(this ITheme theme, bool fallbackToDefault = true)
    {
        return theme.GetLayout(StandardLayouts.Account, fallbackToDefault);
    }

    [Obsolete("Use GetPublicLayoutAsync instead.")]
    public static Type GetPublicLayout(this ITheme theme, bool fallbackToDefault = true)
    {
        return theme.GetLayout(StandardLayouts.Public, fallbackToDefault);
    }

    [Obsolete("Use GetEmptyLayoutAsync instead.")]
    public static Type GetEmptyLayout(this ITheme theme, bool fallbackToDefault = true)
    {
        return theme.GetLayout(StandardLayouts.Empty, fallbackToDefault);
    }

    public async static Task<Type> GetApplicationLayoutAsync(this ITheme theme, bool fallbackToDefault = true)
    {
        return await theme.GetLayoutAsync(StandardLayouts.Application, fallbackToDefault);
    }

    public async static Task<Type> GetAccountLayoutAsync(this ITheme theme, bool fallbackToDefault = true)
    {
        return await theme.GetLayoutAsync(StandardLayouts.Account, fallbackToDefault);
    }

    public async static Task<Type> GetPublicLayoutAsync(this ITheme theme, bool fallbackToDefault = true)
    {
        return await theme.GetLayoutAsync(StandardLayouts.Public, fallbackToDefault);
    }

    public async static Task<Type> GetEmptyLayoutAsync(this ITheme theme, bool fallbackToDefault = true)
    {
        return await theme.GetLayoutAsync(StandardLayouts.Empty, fallbackToDefault);
    }

    public async static Task<Type> GetCurrentApplicationLayoutAsync(this IThemeManager themeManager, bool fallbackToDefault = true)
    {
        return await (await themeManager.GetCurrentThemeAsync()).GetLayoutAsync(StandardLayouts.Application, fallbackToDefault);
    }

    public async static Task<Type> GetCurrentAccountLayoutAsync(this IThemeManager themeManager, bool fallbackToDefault = true)
    {
        return await (await themeManager.GetCurrentThemeAsync()).GetLayoutAsync(StandardLayouts.Account, fallbackToDefault);
    }

    public async static Task<Type> GetCurrentPublicLayoutAsync(this IThemeManager themeManager, bool fallbackToDefault = true)
    {
        return await (await themeManager.GetCurrentThemeAsync()).GetLayoutAsync(StandardLayouts.Public, fallbackToDefault);
    }

    public async static Task<Type> GetCurrentEmptyLayoutAsync(this IThemeManager themeManager, bool fallbackToDefault = true)
    {
        return await (await themeManager.GetCurrentThemeAsync()).GetLayoutAsync(StandardLayouts.Empty, fallbackToDefault);
    }
}
