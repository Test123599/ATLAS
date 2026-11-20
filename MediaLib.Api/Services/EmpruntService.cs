using MediaLib.Api.Common;
using MediaLib.Api.Data;
using MediaLib.Api.Domain;
using MediaLib.Api.Mapping;
using MediaLib.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MediaLib.Api.Services;

public interface IEmpruntService
{
    Task<EmpruntDto> BorrowAsync(string utilisateurId, CreateEmpruntRequest request, CancellationToken ct = default);
    Task<MesEmpruntsDto> GetMineAsync(string utilisateurId, CancellationToken ct = default);
    Task<PagedResult<EmpruntDto>> GetAllAsync(EmpruntQueryDto query, CancellationToken ct = default);
    Task<EmpruntDto> ReturnAsync(int id, CancellationToken ct = default);
}

/// <summary>
/// Loan rules: max 5 active loans, 21-day period, one copy per loan, stock kept
/// consistent with conditional SQL updates inside a transaction.
/// </summary>
public sealed class EmpruntService : IEmpruntService
{
    public const int LimiteEmprunts = 5;
    public static readonly TimeSpan DureeEmprunt = TimeSpan.FromDays(21);

    private readonly MediaLibDbContext _db;
    private readonly ILogger<EmpruntService> _logger;

    public EmpruntService(MediaLibDbContext db, ILogger<EmpruntService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<EmpruntDto> BorrowAsync(string utilisateurId, CreateEmpruntRequest request, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var maintenant = DateTime.UtcNow;

        var livre = await _db.Livres.AsNoTracking().FirstOrDefaultAsync(l => l.Id == request.LivreId, ct)
                    ?? throw AppException.NotFound("Ce livre n'existe pas.");

        if (livre.NombreDisponibles <= 0)
        {
            throw AppException.Conflict("Aucun exemplaire de ce livre n'est disponible.", "STOCK_EPUISE");
        }

        var empruntsActifs = await _db.Emprunts.CountAsync(
            e => e.UtilisateurId == utilisateurId && e.DateRetourEffective == null, ct);

        if (empruntsActifs >= LimiteEmprunts)
        {
            throw AppException.Conflict(
                $"La limite de {LimiteEmprunts} emprunts actifs est atteinte. Rendez un livre avant d'en emprunter un autre.",
                "LIMITE_EMPRUNTS_ATTEINTE");
        }

        // Atomic, concurrency-safe decrement: the WHERE clause fails if another request
        // took the last copy between our read and our write.
        var decremente = await _db.Livres
            .Where(l => l.Id == request.LivreId && l.NombreDisponibles > 0)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(l => l.NombreDisponibles, l => l.NombreDisponibles - 1), ct);

        if (decremente == 0)
        {
            throw AppException.Conflict("Le dernier exemplaire vient d'être emprunté. Veuillez réessayer.", "STOCK_EPUISE");
        }

        // Re-check after the write lock is held: two concurrent requests may both have
        // seen 4 active loans before the decrement. The losing request rolls back.
        var empruntsApresDecrement = await _db.Emprunts.CountAsync(
            e => e.UtilisateurId == utilisateurId && e.DateRetourEffective == null, ct);

        if (empruntsApresDecrement >= LimiteEmprunts)
        {
            throw AppException.Conflict(
                $"La limite de {LimiteEmprunts} emprunts actifs est atteinte. Rendez un livre avant d'en emprunter un autre.",
                "LIMITE_EMPRUNTS_ATTEINTE");
        }

        var emprunt = new Emprunt
        {
            LivreId = request.LivreId,
            UtilisateurId = utilisateurId,
            DateEmprunt = maintenant,
            DateRetourPrevue = maintenant.Add(DureeEmprunt),
        };

        _db.Emprunts.Add(emprunt);

        try
        {
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Concurrent borrow failed for book {LivreId}", request.LivreId);
            throw AppException.Conflict("L'emprunt n'a pas pu être enregistré en raison d'une opération concurrente.", "CONCURRENCE_EMPRUNT");
        }

        emprunt.Livre = livre;
        emprunt.Utilisateur = (await _db.Users.FindAsync(new object[] { utilisateurId }, ct))!;
        return emprunt.ToDto();
    }

    public async Task<MesEmpruntsDto> GetMineAsync(string utilisateurId, CancellationToken ct = default)
    {
        var emprunts = await QueryWithDetails()
            .Where(e => e.UtilisateurId == utilisateurId)
            .OrderByDescending(e => e.DateEmprunt)
            .ToListAsync(ct);

        var actifs = emprunts.Where(e => e.DateRetourEffective is null)
            .OrderBy(e => e.DateRetourPrevue)
            .Select(e => e.ToDto()).ToList();

        var historique = emprunts.Where(e => e.DateRetourEffective is not null)
            .Select(e => e.ToDto()).ToList();

        return new MesEmpruntsDto(actifs, historique, LimiteEmprunts);
    }

    public async Task<PagedResult<EmpruntDto>> GetAllAsync(EmpruntQueryDto query, CancellationToken ct = default)
    {
        var maintenant = DateTime.UtcNow;
        var emprunts = QueryWithDetails().AsQueryable();

        if (query.EnRetard == true)
        {
            emprunts = emprunts.Where(e => e.DateRetourEffective == null && e.DateRetourPrevue < maintenant);
        }

        if (query.Actif.HasValue)
        {
            emprunts = query.Actif.Value
                ? emprunts.Where(e => e.DateRetourEffective == null)
                : emprunts.Where(e => e.DateRetourEffective != null);
        }

        if (!string.IsNullOrWhiteSpace(query.Recherche))
        {
            var terme = query.Recherche.Trim();
            emprunts = emprunts.Where(e =>
                e.Livre.Titre.Contains(terme) ||
                e.Livre.Auteur.Nom.Contains(terme) ||
                e.Livre.Auteur.Prenom.Contains(terme) ||
                e.Utilisateur.Nom.Contains(terme) ||
                e.Utilisateur.Prenom.Contains(terme) ||
                e.Utilisateur.Email!.Contains(terme));
        }

        emprunts = emprunts.OrderByDescending(e => e.DateEmprunt);

        var total = await emprunts.CountAsync(ct);
        var page = await emprunts
            .Skip((query.Page - 1) * query.TaillePage)
            .Take(query.TaillePage)
            .ToListAsync(ct);

        return new PagedResult<EmpruntDto>(page.Select(e => e.ToDto()).ToList(), query.Page, query.TaillePage, total);
    }

    public async Task<EmpruntDto> ReturnAsync(int id, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var emprunt = await _db.Emprunts
            .Include(e => e.Livre)!.ThenInclude(l => l.Auteur)
            .Include(e => e.Livre)!.ThenInclude(l => l.Categorie)
            .Include(e => e.Utilisateur)
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw AppException.NotFound("Cet emprunt n'existe pas.");

        var maintenant = DateTime.UtcNow;

        // Make the return state transition atomic before touching inventory so two
        // simultaneous returns cannot both restore a copy.
        var retournes = await _db.Emprunts
            .Where(e => e.Id == id && e.DateRetourEffective == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.DateRetourEffective, maintenant), ct);

        if (retournes == 0)
        {
            throw AppException.Conflict("Cet emprunt a déjà été rendu.", "RETOUR_DUPLIQUE");
        }

        // MIN() protects the invariant even if inventory was corrected manually.
        var lignes = await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Livres SET NombreDisponibles = MIN(NombreExemplaires, NombreDisponibles + 1) WHERE Id = {emprunt.LivreId}",
            ct);

        if (lignes == 0)
        {
            throw AppException.NotFound("Le livre associé à cet emprunt n'existe plus.");
        }

        emprunt.DateRetourEffective = maintenant;
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return emprunt.ToDto();
    }

    private IQueryable<Emprunt> QueryWithDetails()
        => _db.Emprunts
            .AsNoTracking()
            .Include(e => e.Livre)!.ThenInclude(l => l.Auteur)
            .Include(e => e.Livre)!.ThenInclude(l => l.Categorie)
            .Include(e => e.Utilisateur);
}
