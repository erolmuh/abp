using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace Volo.Abp.AspNetCore.Components.Web.Theming.Theming;

public class DefaultThemeManager : IThemeManager, IScopedDependency, IServiceProviderAccessor
{
    public IServiceProvider ServiceProvider { get; }
    private ITheme? _currentTheme;
    protected IThemeSelector ThemeSelector { get; }

    public DefaultThemeManager(
        IServiceProvider serviceProvider,
        IThemeSelector themeSelector)
    {
        ServiceProvider = serviceProvider;
        ThemeSelector = themeSelector;
    }

    [Obsolete("Use GetCurrentThemeAsync instead.")]
    public ITheme CurrentTheme => GetCurrentTheme();

    protected virtual ITheme GetCurrentTheme()
    {
        if (_currentTheme != null)
        {
            return _currentTheme;
        }

        _currentTheme = (ITheme)ServiceProvider.GetRequiredService(ThemeSelector.GetCurrentThemeInfo().ThemeType);
        return _currentTheme;
    }

    public virtual async Task<ITheme> GetCurrentThemeAsync()
    {
        if (_currentTheme != null)
        {
            return _currentTheme;
        }

        _currentTheme = (ITheme)ServiceProvider.GetRequiredService((await ThemeSelector.GetCurrentThemeInfoAsync()).ThemeType);
        return _currentTheme;
    }
}
