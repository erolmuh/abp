using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Volo.Abp.Http.Client.Authentication;

namespace Volo.Abp.AspNetCore.Components.WebAssembly.WebApp;

public class PersistentComponentStateAbpAccessTokenProvider : IAbpAccessTokenProvider
{
    protected string? AccessToken { get; set; }

    protected PersistentComponentState PersistentComponentState { get; }

    protected IJSRuntime JsRuntime { get; }

    public PersistentComponentStateAbpAccessTokenProvider(PersistentComponentState persistentComponentState, IJSRuntime jsRuntime)
    {
        PersistentComponentState = persistentComponentState;
        JsRuntime = jsRuntime;
        AccessToken = null;
    }

    public virtual async Task<string?> GetTokenAsync()
    {
        if (AccessToken != null)
        {
            return AccessToken;
        }

        AccessToken = await JsRuntime.InvokeAsync<string>(
            "localStorage.getItem",
            "access_token"
        );

        if (string.IsNullOrWhiteSpace(AccessToken))
        {
            AccessToken = PersistentComponentState.TryTakeFromJson<PersistentAccessToken>(PersistentAccessToken.Key, out var token)
                ? token?.AccessToken
                : null;
        }

        return AccessToken;
    }
}
