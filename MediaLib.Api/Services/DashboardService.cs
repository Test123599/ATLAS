using MediaLib.Api.Data;
using MediaLib.Api.Domain;
using MediaLib.Api.Mapping;
using MediaLib.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MediaLib.Api.Services;

public interface IDashboardService
{
    Task<MemberDashboardDto> GetMemberAsync(string utilisateurId, CancellationToken ct = default);
    Task<LibrarianDashboardDto> GetLibrarianAsync(CancellationToken ct = default);
}

public sealed class DashboardService : IDashboardService
{
    private readonly MediaLibDbContext _db;
    private readonly ICatalogueService _catalogue;

    public DashboardService(MediaLibDbContext db, ICatalogueService catalogue)
    {
        _db = db;
        _catalogue = catalogue;
    }

    public async Task<MemberDashboardDto> GetMemberAsync(string utilisateurId, CancellationToken ct = default)
    {
        var utilisateur = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == utilisateurId, ct)
                          ?? throw Common.AppException.NotFound("Profil introuvable.");

        var emprunts = await _db.Emprunts.AsNoTracking()
            .Include(e => e.Livre)!.ThenInclude(l => l.Auteur)
            .Include(e => e.Livre)!.ThenInclude(l => l.Categorie)
            .Include(e => e.Utilisateur)
            .Where(e => e.UtilisateurId == utilisateurId)
            .OrderByDescending(e => e.DateEmprunt)
            .ToListAsync(ct);

        var maintenant = DateTime.UtcNow;
        var actifs = emprunts.Where(e => e.DateRetourEffective is null)
            .OrderBy(e => e.DateRetourPrevue).Select(e => e.ToDto()).ToList();

        var historique = emprunts.Where(e => e.DateRetourEffective is not null)
            .OrderByDescending(e => e.DateRetourEffective)
            .ToList();

        return new MemberDashboardDto(
            utilisateur.Prenom,
            actifs.Count,
            EmpruntService.LimiteEmprunts,
            actifs.Count(e => e.DateRetourPrevue < maintenant),
            historique.Count,
            actifs.Count > 0 ? actifs.Min(e => e.DateRetourPrevue) : null,
            actifs,
            historique.Take(10).Select(e => e.ToDto()).ToList(),
            await _catalogue.GetRecentsAsync(4, ct));
    }

    public async Task<LibrarianDashboardDto> GetLibrarianAsync(CancellationToken ct = default)
    {
        var maintenant = DateTime.UtcNow;
        var debutJour = maintenant.Date;
        var debutSemaine = debutJour.AddDays(-7);

        var emprunts = await _db.Emprunts.AsNoTracking()
            .Include(e => e.Livre)!.ThenInclude(l => l.Auteur)
            .Include(e => e.Utilisateur)
            .OrderByDescending(e => e.DateEmprunt)
            .Take(12)
            .ToListAsync(ct);

        var activite = emprunts.Select(e => new ActiviteDto(
            e.Id,
            e.LivreId,
            e.Livre.Titre,
            e.Utilisateur.NomComplet,
            e.DateRetourEffective ?? e.DateEmprunt,
            e.DateRetourEffective is null ? "Emprunt" : "Retour",
            e.DateRetourEffective is null && e.DateRetourPrevue < maintenant)).ToList();

        var retoursRecents = (await _db.Emprunts.AsNoTracking()
                .Include(e => e.Livre)!.ThenInclude(l => l.Auteur)
                .Include(e => e.Livre)!.ThenInclude(l => l.Categorie)
                .Include(e => e.Utilisateur)
                .Where(e => e.DateRetourEffective != null)
                .OrderByDescending(e => e.DateRetourEffective)
                .Take(5)
                .ToListAsync(ct))
            .Select(e => e.ToDto())
            .ToList();

        var actifs = await _db.Emprunts.CountAsync(e => e.DateRetourEffective == null, ct);
        var enRetard = await _db.Emprunts.CountAsync(
            e => e.DateRetourEffective == null && e.DateRetourPrevue < maintenant, ct);
        var aujourdHui = await _db.Emprunts.CountAsync(e => e.DateEmprunt >= debutJour, ct);
        var retoursSemaine = await _db.Emprunts.CountAsync(
            e => e.DateRetourEffective != null && e.DateRetourEffective >= debutSemaine, ct);

        return new LibrarianDashboardDto(
            await _catalogue.GetStatsAsync(ct),
            actifs,
            enRetard,
            aujourdHui,
            retoursSemaine,
            activite,
            retoursRecents);
    }
}
