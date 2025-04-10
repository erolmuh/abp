using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Volo.Abp.AspNetCore.Components.Web;
using Volo.Abp.Http.Client.Authentication;

namespace Volo.Abp.AspNetCore.Components.WebAssembly.WebApp;

public class PersistentComponentStateAbpAccessTokenProvider : IAbpAccessTokenProvider
{
    protected string? AccessToken { get; set; }

    protected PersistentComponentState PersistentComponentState { get; }

    protected ILocalStorageService LocalStorageService { get; }

    public PersistentComponentStateAbpAccessTokenProvider(PersistentComponentState persistentComponentState, ILocalStorageService localStorageService)
    {
        PersistentComponentState = persistentComponentState;
        LocalStorageService = localStorageService;

        AccessToken = null;
    }

    public virtual async Task<string?> GetTokenAsync()
    {
        if (!AccessToken.IsNullOrWhiteSpace())
        {
            return AccessToken;
        }

        AccessToken = await LocalStorageService.GetItemAsync(PersistentAccessToken.Key);
        if (string.IsNullOrWhiteSpace(AccessToken))
        {
            AccessToken = PersistentComponentState.TryTakeFromJson<PersistentAccessToken>(PersistentAccessToken.Key, out var token)
                ? token?.AccessToken
                : null;
        }

        return AccessToken;
    }
}
