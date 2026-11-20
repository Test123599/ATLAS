using MediaLib.Api.Common;
using MediaLib.Api.Data;
using MediaLib.Api.Mapping;
using MediaLib.Shared.Constants;
using MediaLib.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MediaLib.Api.Services;

public interface IAdminService
{
    Task<PagedResult<UtilisateurDto>> GetUtilisateursAsync(UtilisateurQueryDto query, CancellationToken ct = default);
    Task<UtilisateurDetailDto> GetUtilisateurAsync(string id, CancellationToken ct = default);
    Task<UtilisateurDto> UpdateUtilisateurAsync(string id, UpdateUtilisateurRequest request, CancellationToken ct = default);
    Task<UtilisateurDto> UpdateRolesAsync(string administrateurId, string id, UpdateRolesRequest request, CancellationToken ct = default);
    Task<UtilisateurDto> SetActifAsync(string administrateurId, string id, bool estActif, CancellationToken ct = default);
}

/// <summary>User administration with protections against self-lockout and last-admin removal.</summary>
public sealed class AdminService : IAdminService
{
    private readonly UserManager<Domain.Utilisateur> _users;
    private readonly RoleManager<IdentityRole> _roles;
    private readonly MediaLibDbContext _db;
    private readonly IAuthService _auth;

    public AdminService(
        UserManager<Domain.Utilisateur> users,
        RoleManager<IdentityRole> roles,
        MediaLibDbContext db,
        IAuthService auth)
    {
        _users = users;
        _roles = roles;
        _db = db;
        _auth = auth;
    }

    public async Task<PagedResult<UtilisateurDto>> GetUtilisateursAsync(UtilisateurQueryDto query, CancellationToken ct = default)
    {
        var users = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Recherche))
        {
            var terme = query.Recherche.Trim();
            users = users.Where(u =>
                u.Email!.Contains(terme) || u.Nom.Contains(terme) || u.Prenom.Contains(terme));
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = await _roles.FindByNameAsync(query.Role.Trim());
            if (role is null)
            {
                throw AppException.BadRequest("Le rôle demandé n'existe pas.", "ROLE_INCONNU");
            }

            var ids = await _db.UserRoles.Where(ur => ur.RoleId == role.Id).Select(ur => ur.UserId).ToListAsync(ct);
            users = users.Where(u => ids.Contains(u.Id));
        }

        users = users.OrderBy(u => u.Nom).ThenBy(u => u.Prenom);

        var total = await users.CountAsync(ct);
        var page = await users.Skip((query.Page - 1) * query.TaillePage).Take(query.TaillePage).ToListAsync(ct);
        var idsPage = page.Select(u => u.Id).ToList();

        var rolesParUser = await (from ur in _db.UserRoles
                                  join r in _db.Roles on ur.RoleId equals r.Id
                                  where idsPage.Contains(ur.UserId)
                                  select new { ur.UserId, r.Name }).ToListAsync(ct);

        var empruntsParUser = await _db.Emprunts
            .Where(e => idsPage.Contains(e.UtilisateurId) && e.DateRetourEffective == null)
            .GroupBy(e => e.UtilisateurId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var items = page.Select(u => u.ToDto(
            rolesParUser.Where(r => r.UserId == u.Id).Select(r => r.Name!).OrderBy(r => r).ToList(),
            empruntsParUser.FirstOrDefault(e => e.UserId == u.Id)?.Count ?? 0)).ToList();

        return new PagedResult<UtilisateurDto>(items, query.Page, query.TaillePage, total);
    }

    public async Task<UtilisateurDetailDto> GetUtilisateurAsync(string id, CancellationToken ct = default)
    {
        var utilisateur = await _db.Users
            .Include(u => u.Emprunts)
                .ThenInclude(e => e.Livre)
                    .ThenInclude(l => l!.Auteur)
            .Include(u => u.Emprunts)
                .ThenInclude(e => e.Livre)
                    .ThenInclude(l => l!.Categorie)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw AppException.NotFound("Cet utilisateur n'existe pas.");

        var roles = (await _users.GetRolesAsync(utilisateur)).ToList();
        return utilisateur.ToDetailDto(roles);
    }

    public async Task<UtilisateurDto> UpdateUtilisateurAsync(string id, UpdateUtilisateurRequest request, CancellationToken ct = default)
    {
        var utilisateur = await _users.FindByIdAsync(id)
                          ?? throw AppException.NotFound("Cet utilisateur n'existe pas.");

        utilisateur.Prenom = request.Prenom.Trim();
        utilisateur.Nom = request.Nom.Trim();
        var result = await _users.UpdateAsync(utilisateur);
        if (!result.Succeeded)
        {
            throw AppException.BadRequest("La mise à jour a échoué.", "UTILISATEUR_INVALIDE");
        }

        var roles = (await _users.GetRolesAsync(utilisateur)).ToList();
        var actifs = await _db.Emprunts.CountAsync(e => e.UtilisateurId == id && e.DateRetourEffective == null, ct);
        return utilisateur.ToDto(roles, actifs);
    }

    public async Task<UtilisateurDto> UpdateRolesAsync(string administrateurId, string id, UpdateRolesRequest request, CancellationToken ct = default)
    {
        if (id == administrateurId)
        {
            throw AppException.Conflict("Vous ne pouvez pas modifier vos propres rôles.", "AUTO_MODIFICATION");
        }

        var utilisateur = await _users.FindByIdAsync(id)
                          ?? throw AppException.NotFound("Cet utilisateur n'existe pas.");

        var nouveauxRoles = request.Roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (nouveauxRoles.Count == 0)
        {
            throw AppException.BadRequest("Au moins un rôle doit être attribué.", "ROLES_VIDES");
        }

        var invalides = nouveauxRoles.Where(r => !Roles.EstValide(r)).ToList();
        if (invalides.Count > 0)
        {
            throw AppException.BadRequest($"Rôle inconnu : {string.Join(", ", invalides)}.", "ROLE_INCONNU");
        }

        var rolesActuels = (await _users.GetRolesAsync(utilisateur)).ToList();
        var estAdminActuellement = rolesActuels.Contains(Roles.Admin);
        var conserveraAdmin = nouveauxRoles.Contains(Roles.Admin);

        if (estAdminActuellement && !conserveraAdmin)
        {
            var adminRole = await _roles.FindByNameAsync(Roles.Admin);
            var nbAdmins = await _db.UserRoles.CountAsync(ur => ur.RoleId == adminRole!.Id, ct);
            if (nbAdmins <= 1)
            {
                throw AppException.Conflict("Le dernier administrateur ne peut pas perdre son rôle.", "DERNIER_ADMIN");
            }
        }

        var retrait = await _users.RemoveFromRolesAsync(utilisateur, rolesActuels.Except(nouveauxRoles));
        if (!retrait.Succeeded)
        {
            throw AppException.BadRequest("La modification des rôles a échoué.", "ROLES_MODIFICATION");
        }

        var ajout = await _users.AddToRolesAsync(utilisateur, nouveauxRoles.Except(rolesActuels));
        if (!ajout.Succeeded)
        {
            throw AppException.BadRequest("La modification des rôles a échoué.", "ROLES_MODIFICATION");
        }

        if (estAdminActuellement && !conserveraAdmin)
        {
            await _auth.RevokeAllRefreshTokensAsync(id, ct);
        }

        var actifs = await _db.Emprunts.CountAsync(e => e.UtilisateurId == id && e.DateRetourEffective == null, ct);
        return utilisateur.ToDto(nouveauxRoles.OrderBy(r => r).ToList(), actifs);
    }

    public async Task<UtilisateurDto> SetActifAsync(string administrateurId, string id, bool estActif, CancellationToken ct = default)
    {
        if (id == administrateurId && !estActif)
        {
            throw AppException.Conflict("Vous ne pouvez pas désactiver votre propre compte.", "AUTO_DESACTIVATION");
        }

        var utilisateur = await _users.FindByIdAsync(id)
                          ?? throw AppException.NotFound("Cet utilisateur n'existe pas.");

        utilisateur.EstActif = estActif;
        var result = await _users.UpdateAsync(utilisateur);
        if (!result.Succeeded)
        {
            throw AppException.BadRequest("La mise à jour du compte a échoué.", "COMPTE_INVALIDE");
        }

        if (!estActif)
        {
            await _auth.RevokeAllRefreshTokensAsync(id, ct);
        }

        var roles = (await _users.GetRolesAsync(utilisateur)).ToList();
        var actifs = await _db.Emprunts.CountAsync(e => e.UtilisateurId == id && e.DateRetourEffective == null, ct);
        return utilisateur.ToDto(roles, actifs);
    }
}
