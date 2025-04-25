using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Theming;

namespace Volo.CmsKit.Public.Web.Pages.CmsKit.Shared.Layouts;

public static class LayoutExtensions
{
    public async static Task<string> GetLayoutByKeyAsync(this ITheme theme, string layoutKey)
    {
        return layoutKey switch
        {
            StandardLayouts.Application => await theme.GetApplicationLayoutAsync(),
            StandardLayouts.Account => await theme.GetAccountLayoutAsync(),
            StandardLayouts.Public => await theme.GetPublicLayoutAsync(),
            StandardLayouts.Empty => await theme.GetEmptyLayoutAsync(),
            _ => await theme.GetApplicationLayoutAsync()
        };
    }
}
