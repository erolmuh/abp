using System;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

public static class AbpNavigationManagerExtensions
{
    public static void AbpNavigateToLogin(this NavigationManager manager, string loginUrl)
    {
        manager.NavigateToLogin(loginUrl, new InteractiveRequestOptions
        {
            Interaction = InteractionType.SignIn,
            ReturnUrl = manager.GetReturnUrl()
        });
    }
    
    public static string GetReturnUrl(this NavigationManager manager)
    {
        return manager.Uri.EndsWith("authentication/logged-out", StringComparison.OrdinalIgnoreCase) ? manager.BaseUri : manager.Uri;
    }
}