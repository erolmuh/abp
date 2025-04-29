using System.Threading.Tasks;

namespace Volo.Abp.AspNetCore.Mvc.UI.Bundling;

public interface IAbpGlobalAssetsBundleService
{
    Task<string> GetStylesAsync();

    Task<string> GetScriptsAsync();
}
