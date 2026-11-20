using MediaLib.Api.Common;
using MediaLib.Api.Services;
using MediaLib.Shared.Constants;
using MediaLib.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediaLib.Api.Controllers;

/// <summary>Public category directory and staff-only category management.</summary>
[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategorieService _categories;

    public CategoriesController(ICategorieService categories) => _categories = categories;

    /// <summary>Lists all categories with their number of books.</summary>
    /// <response code="200">Categories.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<CategorieDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategorieDto>>> GetCategories(CancellationToken ct)
        => Ok(await _categories.GetAllAsync(ct));

    /// <summary>Returns one category.</summary>
    /// <response code="200">Category.</response>
    /// <response code="404">Unknown category.</response>
    [HttpGet("{id:int}", Name = "GetCategorie")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategorieDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategorieDto>> GetCategorie(int id, CancellationToken ct)
        => Ok(await _categories.GetAsync(id, ct));

    /// <summary>Creates a category.</summary>
    /// <response code="201">Created category with Location header.</response>
    /// <response code="409">Duplicate label.</response>
    [HttpPost]
    [Authorize(Policy = Policies.PeutGererCatalogue)]
    [ProducesResponseType(typeof(CategorieDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreerCategorie([FromBody] CreateCategorieRequest request, CancellationToken ct)
    {
        var categorie = await _categories.CreateAsync(request, ct);
        return CreatedAtRoute("GetCategorie", new { id = categorie.Id }, categorie);
    }

    /// <summary>Updates a category.</summary>
    /// <response code="200">Updated category.</response>
    /// <response code="404">Unknown category.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.PeutGererCatalogue)]
    [ProducesResponseType(typeof(CategorieDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategorieDto>> ModifierCategorie(int id, [FromBody] UpdateCategorieRequest request, CancellationToken ct)
        => Ok(await _categories.UpdateAsync(id, request, ct));

    /// <summary>Deletes a category only when no books are associated with it.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="409">The category still has books.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.PeutGererCatalogue)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SupprimerCategorie(int id, CancellationToken ct)
    {
        await _categories.DeleteAsync(id, ct);
        return NoContent();
    }
}
