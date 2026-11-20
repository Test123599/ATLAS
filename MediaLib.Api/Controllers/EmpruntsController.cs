using System.Security.Claims;
using MediaLib.Api.Common;
using MediaLib.Api.Services;
using MediaLib.Shared.Constants;
using MediaLib.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediaLib.Api.Controllers;

/// <summary>Borrowing for members and loan management for library staff.</summary>
[ApiController]
[Route("api/emprunts")]
[Produces("application/json")]
public sealed class EmpruntsController : ControllerBase
{
    private readonly IEmpruntService _emprunts;

    public EmpruntsController(IEmpruntService emprunts) => _emprunts = emprunts;

    /// <summary>Borrows a book for the authenticated member (21 days, max 5 active loans).</summary>
    /// <response code="201">Loan created with Location header.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">Unknown book.</response>
    /// <response code="409">No copy available or loan limit reached.</response>
    [HttpPost]
    [Authorize(Policy = Policies.PeutEmprunter)]
    [ProducesResponseType(typeof(EmpruntDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Emprunter([FromBody] CreateEmpruntRequest request, CancellationToken ct)
    {
        var emprunt = await _emprunts.BorrowAsync(UserId(), request, ct);
        return CreatedAtAction(nameof(MesEmprunts), new { }, emprunt);
    }

    /// <summary>Returns the authenticated member's active loans and history.</summary>
    /// <remarks>The user id always comes from the JWT; client-supplied ids are ignored.</remarks>
    /// <response code="200">Active loans and history.</response>
    [HttpGet("moi")]
    [Authorize(Policy = Policies.PeutEmprunter)]
    [ProducesResponseType(typeof(MesEmpruntsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MesEmpruntsDto>> MesEmprunts(CancellationToken ct)
        => Ok(await _emprunts.GetMineAsync(UserId(), ct));

    /// <summary>Lists all loans for staff; supports enRetard, actif, search and pagination.</summary>
    /// <response code="200">Paginated loans.</response>
    /// <response code="403">Staff role required.</response>
    [HttpGet]
    [Authorize(Policy = Policies.PeutGererCatalogue)]
    [ProducesResponseType(typeof(PagedResult<EmpruntDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<EmpruntDto>>> TousLesEmprunts([FromQuery] EmpruntQueryDto query, CancellationToken ct)
        => Ok(await _emprunts.GetAllAsync(query, ct));

    /// <summary>Records the return of a loan and restores inventory exactly once.</summary>
    /// <response code="200">Completed loan.</response>
    /// <response code="404">Unknown loan.</response>
    /// <response code="409">Loan was already returned.</response>
    [HttpPost("{id:int}/retour")]
    [Authorize(Policy = Policies.PeutGererCatalogue)]
    [ProducesResponseType(typeof(EmpruntDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmpruntDto>> Retourner(int id, CancellationToken ct)
        => Ok(await _emprunts.ReturnAsync(id, ct));

    private string UserId()
        => User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
           ?? throw AppException.Unauthorized("Jeton invalide.");
}
