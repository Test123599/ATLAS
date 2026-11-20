using System.Security.Claims;
using MediaLib.Api.Common;
using MediaLib.Api.Services;
using MediaLib.Shared.Constants;
using MediaLib.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediaLib.Api.Controllers;

/// <summary>User management, role assignment and account activation (admin only).</summary>
[ApiController]
[Route("api/admin")]
[Produces("application/json")]
[Authorize(Policy = Policies.EstAdmin)]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin) => _admin = admin;

    /// <summary>Lists users with optional search and role filter.</summary>
    /// <response code="200">Paginated users.</response>
    [HttpGet("utilisateurs")]
    [ProducesResponseType(typeof(PagedResult<UtilisateurDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UtilisateurDto>>> Utilisateurs([FromQuery] UtilisateurQueryDto query, CancellationToken ct)
        => Ok(await _admin.GetUtilisateursAsync(query, ct));

    /// <summary>Returns a user's details including their loans.</summary>
    /// <response code="200">User details.</response>
    /// <response code="404">Unknown user.</response>
    [HttpGet("utilisateurs/{id}")]
    [ProducesResponseType(typeof(UtilisateurDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UtilisateurDetailDto>> Utilisateur(string id, CancellationToken ct)
        => Ok(await _admin.GetUtilisateurAsync(id, ct));

    /// <summary>Updates a user's name fields.</summary>
    /// <response code="200">Updated user.</response>
    [HttpPut("utilisateurs/{id}")]
    [ProducesResponseType(typeof(UtilisateurDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UtilisateurDto>> ModifierUtilisateur(string id, [FromBody] UpdateUtilisateurRequest request, CancellationToken ct)
        => Ok(await _admin.UpdateUtilisateurAsync(id, request, ct));

    /// <summary>Replaces a user's roles. Cannot modify self or remove the last admin.</summary>
    /// <response code="200">Updated user.</response>
    /// <response code="400">Unknown role or empty list.</response>
    /// <response code="409">Dangerous operation refused.</response>
    [HttpPut("utilisateurs/{id}/roles")]
    [ProducesResponseType(typeof(UtilisateurDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UtilisateurDto>> ModifierRoles(string id, [FromBody] UpdateRolesRequest request, CancellationToken ct)
        => Ok(await _admin.UpdateRolesAsync(UserId(), id, request, ct));

    /// <summary>Activates or deactivates an account; deactivation revokes its sessions.</summary>
    /// <response code="200">Updated user.</response>
    /// <response code="409">Self-deactivation refused.</response>
    [HttpPut("utilisateurs/{id}/actif")]
    [ProducesResponseType(typeof(UtilisateurDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UtilisateurDto>> DefinirActif(string id, [FromBody] SetActifRequest request, CancellationToken ct)
        => Ok(await _admin.SetActifAsync(UserId(), id, request.EstActif, ct));

    private string UserId()
        => User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
           ?? throw AppException.Unauthorized("Jeton invalide.");
}
