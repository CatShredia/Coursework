using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CatshrediasNews.Client.Services;

public class UnauthorizedLogoutHandler(IJSRuntime js, NavigationManager nav) : DelegatingHandler
{
    private static int _logoutTriggered;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized &&
            Interlocked.Exchange(ref _logoutTriggered, 1) == 0)
        {
            await ClearAuthStorageAsync();
            nav.NavigateTo(nav.Uri, forceLoad: true);
        }

        return response;
    }

    private async Task ClearAuthStorageAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", "auth_token");
        await js.InvokeVoidAsync("localStorage.removeItem", "auth_username");
        await js.InvokeVoidAsync("localStorage.removeItem", "auth_email");
        await js.InvokeVoidAsync("localStorage.removeItem", "auth_role");
        await js.InvokeVoidAsync("localStorage.removeItem", "auth_id");
        await js.InvokeVoidAsync("localStorage.removeItem", "auth_avatar_url");
        await js.InvokeVoidAsync("localStorage.removeItem", "auth_avatar_color");
    }
}
