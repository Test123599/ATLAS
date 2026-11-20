using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MediaLib.Shared.DTOs;
using Microsoft.AspNetCore.Components.Authorization;

namespace MediaLib.Web.Services.Authentication;

/// <summary>
/// Builds the Blazor authentication state from JWT claims, silently refreshes
/// expired tokens and raises change notifications after login/logout.
/// </summary>
public sealed class AtlasAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly AtlasTokenStore _tokens;
    private readonly IAuthApi _auth;
    private readonly AtlasApiClient _api;

    public AtlasAuthenticationStateProvider(AtlasTokenStore tokens, IAuthApi auth, AtlasApiClient api)
    {
        _tokens = tokens;
        _auth = auth;
        _api = api;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await _tokens.EnsureInitializedAsync();
        var token = _tokens.AccessToken;

        if (string.IsNullOrWhiteSpace(token))
        {
            return Anonymous;
        }

        var claims = ParseClaims(token);
        if (claims.Count == 0)
        {
            // A malformed token is not a session. Keep the stored pair untouched so a
            // public page can still render, but never treat it as authenticated.
            return Anonymous;
        }

        if (IsExpired(claims))
        {
            if (!await _api.RefreshAsync() || _tokens.AccessToken is null)
            {
                await _tokens.ClearAsync();
                return Anonymous;
            }

            claims = ParseClaims(_tokens.AccessToken);
        }

        var identity = new ClaimsIdentity(claims, "JWT", ClaimTypes.NameIdentifier, ClaimTypes.Role);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task SignInAsync(LoginRequest request)
    {
        var response = await _auth.LoginAsync(request);
        await _tokens.SetAsync(response);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task CompleteRegistrationAsync(AuthResponse response)
    {
        await _tokens.SetAsync(response);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync()
    {
        var refresh = _tokens.RefreshToken;
        if (!string.IsNullOrEmpty(refresh))
        {
            try
            {
                await _auth.LogoutAsync(refresh);
            }
            catch (ApiRequestException)
            {
                // Local logout must succeed even if the API is unreachable.
            }
            catch (HttpRequestException)
            {
            }
        }

        await _tokens.ClearAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    public void NotifyProfileChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static bool IsExpired(IReadOnlyList<Claim> claims)
    {
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        return long.TryParse(exp, out var seconds) &&
               DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= seconds;
    }

    private static List<Claim> ParseClaims(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2) return [];

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload += (payload.Length % 4) switch { 2 => "==", 3 => "=", _ => string.Empty };

        try
        {
            using var json = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            var claims = new List<Claim>();

            foreach (var property in json.RootElement.EnumerateObject())
            {
                var type = MapClaimType(property.Name);
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        var value = item.GetString();
                        if (!string.IsNullOrEmpty(value)) claims.Add(new Claim(type, value));
                    }
                }
                else
                {
                    var value = property.Value.ToString();
                    if (!string.IsNullOrEmpty(value)) claims.Add(new Claim(type, value));
                }
            }

            return claims;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            // Corrupt/stale browser storage must not hang the Blazor circuit.
            return [];
        }
    }

    private static string MapClaimType(string name) => name switch
    {
        "sub" => ClaimTypes.NameIdentifier,
        "email" => ClaimTypes.Email,
        "given_name" => ClaimTypes.GivenName,
        "family_name" => ClaimTypes.Surname,
        _ => name,
    };
}
