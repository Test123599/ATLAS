using MediaLib.Api.Common;
using MediaLib.Api.Services;
using MediaLib.Shared.Constants;
using MediaLib.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediaLib.Api.Controllers;

/// <summary>Public author directory and staff-only author management.</summary>
[ApiController]
[Route("api/auteurs")]
[Produces("application/json")]
public sealed class AuteursController : ControllerBase
{
    private readonly IAuteurService _auteurs;

    public AuteursController(IAuteurService auteurs) => _auteurs = auteurs;

    /// <summary>Lists all authors with their number of books.</summary>
    /// <response code="200">Authors ordered by name.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<AuteurDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuteurDto>>> GetAuteurs(CancellationToken ct)
        => Ok(await _auteurs.GetAllAsync(ct));

    /// <summary>Returns one author.</summary>
    /// <response code="200">Author.</response>
    /// <response code="404">Unknown author.</response>
    [HttpGet("{id:int}", Name = "GetAuteur")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuteurDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuteurDto>> GetAuteur(int id, CancellationToken ct)
        => Ok(await _auteurs.GetAsync(id, ct));

    /// <summary>Creates an author.</summary>
    /// <response code="201">Created author with Location header.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [Authorize(Policy = Policies.PeutGererCatalogue)]
    [ProducesResponseType(typeof(AuteurDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreerAuteur([FromBody] CreateAuteurRequest request, CancellationToken ct)
    {
        var auteur = await _auteurs.CreateAsync(request, ct);
        return CreatedAtRoute("GetAuteur", new { id = auteur.Id }, auteur);
    }

    /// <summary>Updates an author.</summary>
    /// <response code="200">Updated author.</response>
    /// <response code="404">Unknown author.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.PeutGererCatalogue)]
    [ProducesResponseType(typeof(AuteurDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuteurDto>> ModifierAuteur(int id, [FromBody] UpdateAuteurRequest request, CancellationToken ct)
        => Ok(await _auteurs.UpdateAsync(id, request, ct));

    /// <summary>Deletes an author only when no books are associated with it.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="404">Unknown author.</response>
    /// <response code="409">The author still has books.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.PeutGererCatalogue)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SupprimerAuteur(int id, CancellationToken ct)
    {
        await _auteurs.DeleteAsync(id, ct);
        return NoContent();
    }
}
