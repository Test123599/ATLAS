using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MediaLib.Web.Services.Authentication;

/// <summary>
/// Lightweight HTTP-level authentication used only to let MVC-style
/// <c>[Authorize(Policy = ...)]</c> page attributes challenge/redirect before the
/// Blazor circuit starts. It reads a marker + role hint written by atlas.js when
/// the real JWT is stored in localStorage. Definitive authentication and
/// authorization are always enforced by the API on every request.
/// </summary>
public sealed class AtlasRouteAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "AtlasRoute";

    private const string MarkerCookie = "atlas_auth";
    private const string RolesCookie = "atlas_roles";
    private const string LoginPath = "/connexion";

    public AtlasRouteAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Cookies[MarkerCookie] != "1")
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var roles = (Request.Cookies[RolesCookie] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var claims = new List<Claim> { new(ClaimTypes.Name, "AtlasSession") };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // Preserve the protected deep link so Login.razor can return there after
        // authentication. Without this, every anonymous reload landed on /connexion
        // and lost the original destination before the Blazor circuit started.
        var retour = Uri.EscapeDataString(
            Request.PathBase.Value + Request.Path.Value + Request.QueryString.Value);
        Response.Redirect($"{LoginPath}?retour={retour}");
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
