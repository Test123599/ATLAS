using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MediaLib.Shared.DTOs;
using MediaLib.Web.Services.Authentication;

namespace MediaLib.Web.Services;

/// <summary>
/// Single HTTP gateway to the API: bearer injection, one transparent refresh
/// retry, and consistent error translation for all feature services.
/// </summary>
public sealed class AtlasApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _factory;
    private readonly AtlasTokenStore _tokens;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AtlasApiClient(IHttpClientFactory factory, AtlasTokenStore tokens)
    {
        _factory = factory;
        _tokens = tokens;
    }

    public async Task<T> GetAsync<T>(string url, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Get, url, null, true, ct);

    /// <summary>Reads an anonymous endpoint while still attaching a token when present.
    /// A stale token never forces a refresh (or logout) on a public page.</summary>
    public async Task<T> GetOptionalAsync<T>(string url, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Get, url, null, true, ct, refreshOnUnauthorized: false);

    public async Task<T> PostAsync<T>(string url, object body, bool authenticated = true, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Post, url, body, authenticated, ct);

    public async Task<T> PutAsync<T>(string url, object body, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Put, url, body, true, ct);

    public async Task DeleteAsync(string url, CancellationToken ct = default)
        => await SendAsync<object?>(HttpMethod.Delete, url, null, true, ct);

    public async Task<T> SendAsync<T>(HttpMethod method, string url, object? body = null,
        bool authenticated = true, CancellationToken ct = default, bool refreshOnUnauthorized = true)
    {
        await _tokens.EnsureInitializedAsync();
        var tokenUsed = _tokens.AccessToken;

        var response = await SendOnceAsync(method, url, body, authenticated, tokenUsed, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized && authenticated &&
            refreshOnUnauthorized && _tokens.RefreshToken is not null)
        {
            await _refreshLock.WaitAsync(ct);
            try
            {
                HttpResponseMessage retryResponse;
                if (!string.IsNullOrEmpty(_tokens.AccessToken) && _tokens.AccessToken != tokenUsed)
                {
                    // Another concurrent request already rotated the tokens.
                    SyncRefreshBody(body);
                    retryResponse = await SendOnceAsync(method, url, body, true, _tokens.AccessToken, ct);
                }
                else if (await TryRefreshAsync(ct))
                {
                    SyncRefreshBody(body);
                    retryResponse = await SendOnceAsync(method, url, body, true, _tokens.AccessToken, ct);
                }
                else
                {
                    await _tokens.ClearAsync();
                    throw new ApiRequestException(401, "SESSION_EXPIREE",
                        "Votre session a expiré. Veuillez vous reconnecter.");
                }

                response.Dispose();
                response = retryResponse;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        await EnsureSuccessAsync(response, ct);

        if (response.StatusCode == HttpStatusCode.NoContent ||
            response.Content.Headers.ContentLength == 0)
        {
            return default!;
        }

        return await response.Content.ReadFromJsonAsync<T>(Json, ct) ?? default!;
    }

    private async Task<HttpResponseMessage> SendOnceAsync(HttpMethod method, string url, object? body,
        bool authenticated, string? token, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        if (authenticated && !string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return await _factory.CreateClient("AtlasApi").SendAsync(request, ct);
    }

    /// <summary>Explicitly rotates the token pair; used by the authentication state provider.</summary>
    public async Task<bool> RefreshAsync(CancellationToken ct = default)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            return await TryRefreshAsync(ct);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<bool> TryRefreshAsync(CancellationToken ct)
    {
        // Another tab (or another Blazor circuit) may have rotated the shared pair.
        await _tokens.ReloadAsync();
        var refreshToken = _tokens.RefreshToken;
        if (string.IsNullOrEmpty(refreshToken)) return false;

        try
        {
            var client = _factory.CreateClient("AtlasApi");
            using var response = await client.PostAsJsonAsync("api/auth/refresh",
                new RefreshRequest { RefreshToken = refreshToken }, Json, ct);

            if (!response.IsSuccessStatusCode)
            {
                // A stale token is a benign race after rotation, not a lost session.
                // The API keeps the current token family alive; adopt the newer pair.
                var error = await ReadErrorAsync(response, ct);
                if (error?.Code == "REFRESH_STALE")
                {
                    await _tokens.ReloadAsync();
                    return !string.IsNullOrEmpty(_tokens.RefreshToken) &&
                           _tokens.RefreshToken != refreshToken;
                }

                return false;
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json, ct);
            if (auth is null) return false;

            await _tokens.SetAsync(auth);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private void SyncRefreshBody(object? body)
    {
        if (body is RefreshRequest refreshRequest)
        {
            refreshRequest.RefreshToken = _tokens.RefreshToken ?? refreshRequest.RefreshToken;
        }
    }

    private static async Task<ApiError?> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ApiError>(Json, ct);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        ApiError? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<ApiError>(Json, ct);
        }
        catch (JsonException)
        {
            // Non-JSON upstream response (e.g. gateway HTML) is still reported safely.
        }

        var status = (int)response.StatusCode;
        throw new ApiRequestException(
            status,
            error?.Code ?? "ERREUR_API",
            error?.Message ?? $"Le service a répondu avec le code {(int)response.StatusCode}.",
            error?.Errors);
    }
}
