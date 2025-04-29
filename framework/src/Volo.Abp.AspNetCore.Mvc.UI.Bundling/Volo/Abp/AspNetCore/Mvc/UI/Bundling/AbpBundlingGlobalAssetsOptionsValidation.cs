using System;
using Microsoft.Extensions.Options;

namespace Volo.Abp.AspNetCore.Mvc.UI.Bundling;

public class AbpBundlingGlobalAssetsOptionsValidation : IValidateOptions<AbpBundlingOptions>
{
    public ValidateOptionsResult Validate(string? name, AbpBundlingOptions options)
    {
        if (options.GlobalAssets.Enabled)
        {
            if (options.GlobalAssets.JavaScriptFileName.IsNullOrWhiteSpace())
            {
                return ValidateOptionsResult.Fail("JavaScriptFileName property must be set when GlobalAssets is enabled.");
            }

            if (options.GlobalAssets.CssFileName.IsNullOrWhiteSpace())
            {
                return ValidateOptionsResult.Fail("CssFileName property must be set when GlobalAssets is enabled.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
