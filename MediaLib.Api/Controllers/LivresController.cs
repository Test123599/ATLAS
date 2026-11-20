using System.Security.Claims;
using MediaLib.Api.Common;
using MediaLib.Api.Services;
using MediaLib.Shared.Constants;
using MediaLib.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediaLib.Api.Controllers;

/// <summary>Catalogue browsing (public) and book management (staff only).</summary>
[ApiController]
[Route("api/livres")]
[Produces("application/json")]
public sealed class LivresController : ControllerBase
{
    private readonly ICatalogueService _catalogue;

    public LivresController(ICatalogueService catalogue) => _catalogue = catalogue;

    /// <summary>Lists books with search, author/category filters, sorting and pagination.</summary>
    /// <remarks>Supported tri values: titre, auteur, annee, ajout, disponibilite (prefix with - to reverse).</remarks>
    /// <response code="200">A paginated collection.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<LivreDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LivreDto>>> GetLivres([FromQuery] LivreQueryDto query, CancellationToken ct)
        => Ok(await _catalogue.GetLivresAsync(query, ct));

    /// <summary>Returns one book with author details and live availability.</summary>
    /// <response code="200">Book details.</response>
    /// <response code="404">Unknown book.</response>
    [HttpGet("{id:int}", Name = "GetLivre")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LivreDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LivreDetailDto>> GetLivre(int id, CancellationToken ct)
        => Ok(await _catalogue.GetLivreAsync(id, CurrentUserId(), ct));

    /// <summary>Creates a book. ISBN must be valid and unique.</summary>
    /// <response code="201">Created book with Location header.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">Duplicate ISBN.</response>
    [HttpPost]
    [Authorize(Policy = Policies.PeutGererCatalogue)]
    [ProducesResponseType(typeof(LivreDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreerLivre([FromBody] CreateLivreRequest request, CancellationToken ct)
    {
        var livre = await _catalogue.CreateAsync(request, ct);
        return CreatedAtRoute("GetLivre", new { id = livre.Id }, livre);
    }

    /// <summary>Updates a book, keeping inventory consistent with active loans.</summary>
    /// <response code="200">Updated book.</response>
    /// <response code="404">Unknown book.</response>
    /// <response code="409">Inconsistent stock or duplicate ISBN.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.PeutGererCatalogue)]
    [ProducesResponseType(typeof(LivreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LivreDto>> ModifierLivre(int id, [FromBody] UpdateLivreRequest request, CancellationToken ct)
        => Ok(await _catalogue.UpdateAsync(id, request, ct));

    /// <summary>Deletes a book that has no loan history.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="404">Unknown book.</response>
    /// <response code="409">The book has borrowing history.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.PeutGererCatalogue)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SupprimerLivre(int id, CancellationToken ct)
    {
        await _catalogue.DeleteAsync(id, ct);
        return NoContent();
    }

    private string? CurrentUserId()
        => User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
}
