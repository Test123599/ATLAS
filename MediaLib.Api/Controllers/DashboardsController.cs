using System.Security.Claims;
using MediaLib.Api.Services;
using MediaLib.Shared.Constants;
using MediaLib.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediaLib.Api.Controllers;

/// <summary>Role-specific dashboard data computed from real database state.</summary>
[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public sealed class DashboardsController : ControllerBase
{
    private readonly IDashboardService _dashboards;

    public DashboardsController(IDashboardService dashboards) => _dashboards = dashboards;

    /// <summary>Member dashboard: active loans, due dates, overdue warnings, history.</summary>
    /// <response code="200">Member dashboard.</response>
    [HttpGet("membre")]
    [Authorize(Policy = Policies.PeutEmprunter)]
    [ProducesResponseType(typeof(MemberDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MemberDashboardDto>> Membre(CancellationToken ct)
    {
        var id = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
                 ?? throw Common.AppException.Unauthorized("Jeton invalide.");
        return Ok(await _dashboards.GetMemberAsync(id, ct));
    }

    /// <summary>Librarian dashboard: catalogue totals, loans, overdue loans and activity.</summary>
    /// <response code="200">Librarian dashboard.</response>
    [HttpGet("bibliothecaire")]
    [Authorize(Policy = Policies.PeutGererCatalogue)]
    [ProducesResponseType(typeof(LibrarianDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LibrarianDashboardDto>> Bibliothecaire(CancellationToken ct)
        => Ok(await _dashboards.GetLibrarianAsync(ct));
}
