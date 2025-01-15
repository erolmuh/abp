using Volo.Abp.Minify;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Volo.Abp.AspNetCore.Mvc.UI.Bundling;

[DependsOn(
    typeof(AbpMinifyModule),
    typeof(AbpVirtualFileSystemModule)
)]
public class AbpAspNetCoreMvcUiBundlingAbstractionsModule : AbpModule
{
}
