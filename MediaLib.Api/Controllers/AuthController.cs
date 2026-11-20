using System.Security.Claims;
using MediaLib.Api.Common;
using MediaLib.Api.Services;
using MediaLib.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MediaLib.Api.Controllers;

/// <summary>Registration, login, token rotation and profile operations.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Registers a new member and assigns the Membre role.</summary>
    /// <remarks>Validates email uniqueness and the shared password policy.</remarks>
    /// <response code="201">The account was created.</response>
    /// <response code="400">The payload is invalid.</response>
    /// <response code="409">The email is already registered.</response>
    /// <response code="429">Too many authentication attempts.</response>
    [HttpPost("inscription")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Inscription([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var profile = await _auth.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(Moi), new { }, profile);
    }

    /// <summary>Authenticates a user and returns a 15-minute JWT plus a refresh token.</summary>
    /// <remarks>Accounts are locked after 5 failed attempts.</remarks>
    /// <response code="200">Credentials accepted; tokens returned.</response>
    /// <response code="401">Invalid credentials.</response>
    /// <response code="403">The account is disabled.</response>
    /// <response code="423">The account is locked after 5 failed attempts.</response>
    /// <response code="429">Too many authentication attempts.</response>
    [HttpPost("connexion")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status423Locked)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Connexion([FromBody] LoginRequest request, CancellationToken ct)
        => Ok(await _auth.LoginAsync(request, ct));

    /// <summary>Rotates a refresh token and returns a fresh token pair.</summary>
    /// <remarks>Reusing a revoked token revokes all of that user's refresh tokens.</remarks>
    /// <response code="200">New token pair.</response>
    /// <response code="401">Invalid, expired or reused refresh token.</response>
    /// <response code="429">Too many authentication attempts.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
        => Ok(await _auth.RefreshAsync(request, ct));

    /// <summary>Revokes the supplied refresh token (logout).</summary>
    /// <response code="204">Token revoked.</response>
    /// <response code="401">Authentication required.</response>
    [HttpPost("deconnexion")]
    [Authorize]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Deconnexion([FromBody] RefreshRequest request, CancellationToken ct)
    {
        await _auth.LogoutAsync(UserId(), request.RefreshToken, ct);
        return NoContent();
    }

    /// <summary>Returns the authenticated user's profile and roles.</summary>
    /// <response code="200">Profile.</response>
    /// <response code="401">Authentication required.</response>
    [HttpGet("moi")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> Moi(CancellationToken ct)
        => Ok(await _auth.GetProfileAsync(UserId(), ct));

    /// <summary>Updates the allowed profile fields (first and last name).</summary>
    /// <response code="200">Updated profile.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPut("profil")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserProfileDto>> ModifierProfil([FromBody] UpdateProfileRequest request, CancellationToken ct)
        => Ok(await _auth.UpdateProfileAsync(UserId(), request, ct));

    /// <summary>Changes the authenticated user's password and revokes all refresh tokens.</summary>
    /// <response code="204">Password changed.</response>
    /// <response code="400">Current password or policy check failed.</response>
    [HttpPut("mot-de-passe")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ModifierMotDePasse([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await _auth.ChangePasswordAsync(UserId(), request, ct);
        return NoContent();
    }

    private string UserId() => User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
                               ?? throw AppException.Unauthorized("Jeton invalide.");
}
