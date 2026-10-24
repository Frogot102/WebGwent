using Microsoft.JSInterop;

namespace WebGwent.Server.Services;

/// <summary>Persists the API JWT in browser localStorage for Blazor Server circuits.</summary>
public sealed class TokenStore(IJSRuntime js)
{
    private const string Key = "webgwent.api.token";
    private string? _cached;
    private bool _hydrated;

    public async Task EnsureHydratedAsync()
    {
        if (_hydrated)
            return;
        try
        {
            _cached = await js.InvokeAsync<string?>("localStorage.getItem", Key);
            _hydrated = true;
        }
        catch (InvalidOperationException)
        {
            // Prerender / static SSR — JS not available yet. Keep unhydrated.
        }
        catch (JSDisconnectedException)
        {
            /* circuit gone */
        }
    }

    public async Task<string?> GetAsync()
    {
        await EnsureHydratedAsync();
        return _hydrated ? _cached : null;
    }

    public async Task SetAsync(string? token)
    {
        _cached = string.IsNullOrWhiteSpace(token) ? null : token;
        _hydrated = true;
        try
        {
            if (_cached is null)
                await js.InvokeVoidAsync("localStorage.removeItem", Key);
            else
                await js.InvokeVoidAsync("localStorage.setItem", Key, _cached);
        }
        catch (InvalidOperationException)
        {
            /* prerender — memory cache still holds the token for this circuit */
        }
        catch (JSDisconnectedException)
        {
            /* circuit gone */
        }
    }

    public void SetMemory(string? token)
    {
        _cached = string.IsNullOrWhiteSpace(token) ? null : token;
        _hydrated = true;
    }
}
