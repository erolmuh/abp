using System;
using System.Threading.Tasks;

namespace Volo.Abp.AspNetCore.Mvc.UI.Theming;

public interface ITheme
{
    [Obsolete("Use GetLayoutAsync instead.")]
    string GetLayout(string name, bool fallbackToDefault = true);

    Task<string> GetLayoutAsync(string name, bool fallbackToDefault = true);
}
