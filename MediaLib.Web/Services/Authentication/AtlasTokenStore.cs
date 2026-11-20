using MediaLib.Shared.DTOs;
using Microsoft.JSInterop;

namespace MediaLib.Web.Services.Authentication;

/// <summary>
/// Holds the current token pair for one Blazor circuit. A copy is persisted in
/// browser localStorage so a page reload can restore the session; see README for
/// the XSS/CSRF trade-offs of this choice.
/// </summary>
public sealed class AtlasTokenStore
{
    private const string AccessKey = "atlas.access";
    private const string RefreshKey = "atlas.refresh";
    private readonly IJSRuntime _js;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;

    public AtlasTokenStore(IJSRuntime js) => _js = js;

    public string? AccessToken { get; private set; }

    public string? RefreshToken { get; private set; }

    public async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        await _gate.WaitAsync();
        try
        {
            if (_initialized) return;
            await LoadFromStorageAsync();
            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Re-reads the shared browser storage after another tab rotated the pair.</summary>
    public async Task ReloadAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await LoadFromStorageAsync();
            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetAsync(AuthResponse response)
    {
        await _gate.WaitAsync();
        try
        {
            // Persist both keys in one JS call so a failed interop never leaves
            // a new access token paired with an old (already rotated) refresh token.
            await _js.InvokeVoidAsync("atlasStorage.setPair",
                AccessKey, RefreshKey, response.AccessToken, response.RefreshToken);
            AccessToken = response.AccessToken;
            RefreshToken = response.RefreshToken;
            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await _js.InvokeVoidAsync("atlasStorage.removePair", AccessKey, RefreshKey);
            AccessToken = null;
            RefreshToken = null;
            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task LoadFromStorageAsync()
    {
        AccessToken = await ReadAsync(AccessKey);
        RefreshToken = await ReadAsync(RefreshKey);
    }

    private async Task<string?> ReadAsync(string key)
    {
        try
        {
            return await _js.InvokeAsync<string?>("atlasStorage.get", key);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
        {
            return null;
        }
    }
}
