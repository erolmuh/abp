using System;
using System.Threading.Tasks;

namespace Volo.Abp.AspNetCore.Components.Web.Theming.Theming;

public interface ITheme
{
    [Obsolete("Use GetLayoutAsync instead.")]
    Type GetLayout(string name, bool fallbackToDefault = true);

    Task<Type> GetLayoutAsync(string name, bool fallbackToDefault = true);
}
