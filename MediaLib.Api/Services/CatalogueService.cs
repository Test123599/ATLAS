using MediaLib.Api.Common;
using MediaLib.Api.Data;
using MediaLib.Api.Domain;
using MediaLib.Api.Mapping;
using MediaLib.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MediaLib.Api.Services;

public interface ICatalogueService
{
    Task<PagedResult<LivreDto>> GetLivresAsync(LivreQueryDto query, CancellationToken ct = default);
    Task<LivreDetailDto> GetLivreAsync(int id, string? utilisateurId, CancellationToken ct = default);
    Task<IReadOnlyList<LivreDto>> GetRecentsAsync(int count, CancellationToken ct = default);
    Task<CatalogueStatsDto> GetStatsAsync(CancellationToken ct = default);
    Task<LivreDto> CreateAsync(CreateLivreRequest request, CancellationToken ct = default);
    Task<LivreDto> UpdateAsync(int id, UpdateLivreRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Valid ISBN-10/13 including checksum, normalized to digits (uppercase X).</summary>
    string NormalizeIsbn(string isbn);
}

public sealed class CatalogueService : ICatalogueService
{
    private readonly MediaLibDbContext _db;

    public CatalogueService(MediaLibDbContext db) => _db = db;

    public string NormalizeIsbn(string isbn)
        => isbn.Replace("-", string.Empty).Replace(" ", string.Empty).Trim().ToUpperInvariant();

    public async Task<PagedResult<LivreDto>> GetLivresAsync(LivreQueryDto query, CancellationToken ct = default)
    {
        var livres = _db.Livres.AsNoTracking().Include(l => l.Auteur).Include(l => l.Categorie).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Recherche))
        {
            var terme = query.Recherche.Trim();
            var isbnTerme = terme.Replace("-", string.Empty).Replace(" ", string.Empty);
            livres = livres.Where(l =>
                l.Titre.Contains(terme) ||
                (isbnTerme.Length > 0 && l.ISBN.Contains(isbnTerme)) ||
                l.Auteur.Nom.Contains(terme) ||
                l.Auteur.Prenom.Contains(terme));
        }

        if (query.CategorieId.HasValue)
        {
            livres = livres.Where(l => l.CategorieId == query.CategorieId.Value);
        }

        if (query.AuteurId.HasValue)
        {
            livres = livres.Where(l => l.AuteurId == query.AuteurId.Value);
        }

        if (query.DisponibleUniquement == true)
        {
            livres = livres.Where(l => l.NombreDisponibles > 0);
        }

        livres = ApplySort(livres, query.Tri);

        var total = await livres.CountAsync(ct);
        var items = await livres
            .Skip((query.Page - 1) * query.TaillePage)
            .Take(query.TaillePage)
            .ToListAsync(ct);

        return new PagedResult<LivreDto>(items.Select(l => l.ToDto()).ToList(), query.Page, query.TaillePage, total);
    }

    private static IOrderedQueryable<Livre> ApplySort(IQueryable<Livre> source, string? tri)
    {
        var descending = tri?.StartsWith('-') == true;
        var key = (tri ?? string.Empty).TrimStart('-').ToLowerInvariant();

        return key switch
        {
            "auteur" => descending
                ? source.OrderByDescending(l => l.Auteur.Nom).ThenByDescending(l => l.Titre)
                : source.OrderBy(l => l.Auteur.Nom).ThenBy(l => l.Titre),
            "annee" => descending ? source.OrderByDescending(l => l.AnneePublication) : source.OrderBy(l => l.AnneePublication),
            "ajout" => descending ? source.OrderByDescending(l => l.DateAjout) : source.OrderBy(l => l.DateAjout),
            "disponibilite" => descending
                ? source.OrderByDescending(l => l.NombreDisponibles).ThenBy(l => l.Titre)
                : source.OrderBy(l => l.NombreDisponibles).ThenBy(l => l.Titre),
            _ => descending ? source.OrderByDescending(l => l.Titre) : source.OrderBy(l => l.Titre),
        };
    }

    public async Task<LivreDetailDto> GetLivreAsync(int id, string? utilisateurId, CancellationToken ct = default)
    {
        var livre = await _db.Livres
            .AsNoTracking()
            .Include(l => l.Auteur)
            .Include(l => l.Categorie)
            .FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw AppException.NotFound("Ce livre n'existe pas.");

        var empruntsActifs = await _db.Emprunts
            .CountAsync(e => e.LivreId == id && e.DateRetourEffective == null, ct);

        var emprunteParMoi = utilisateurId is not null && await _db.Emprunts
            .AnyAsync(e => e.LivreId == id && e.UtilisateurId == utilisateurId && e.DateRetourEffective == null, ct);

        return livre.ToDetailDto(empruntsActifs, emprunteParMoi);
    }

    public async Task<IReadOnlyList<LivreDto>> GetRecentsAsync(int count, CancellationToken ct = default)
    {
        var livres = await _db.Livres.AsNoTracking()
            .Include(l => l.Auteur).Include(l => l.Categorie)
            .OrderByDescending(l => l.DateAjout)
            .Take(Math.Clamp(count, 1, 50))
            .ToListAsync(ct);

        return livres.Select(l => l.ToDto()).ToList();
    }

    public async Task<CatalogueStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var totalLivres = await _db.Livres.CountAsync(ct);
        var exemplaires = await _db.Livres.SumAsync(l => l.NombreExemplaires, ct);
        var disponibles = await _db.Livres.SumAsync(l => l.NombreDisponibles, ct);
        var auteurs = await _db.Auteurs.CountAsync(ct);
        var categories = await _db.Categories.CountAsync(ct);

        return new CatalogueStatsDto(totalLivres, exemplaires, disponibles, auteurs, categories);
    }

    public async Task<LivreDto> CreateAsync(CreateLivreRequest request, CancellationToken ct = default)
    {
        var isbn = NormalizeIsbn(request.Isbn);
        await EnsureIsbnAvailableAsync(isbn, null, ct);
        await EnsureReferencesExistAsync(request.AuteurId, request.CategorieId, ct);

        var livre = new Livre
        {
            Titre = request.Titre.Trim(),
            ISBN = isbn,
            Resume = request.Resume.Trim(),
            AnneePublication = request.AnneePublication,
            NombreExemplaires = request.NombreExemplaires,
            NombreDisponibles = request.NombreExemplaires,
            CouvertureUrl = string.IsNullOrWhiteSpace(request.CouvertureUrl) ? null : request.CouvertureUrl.Trim(),
            AuteurId = request.AuteurId,
            CategorieId = request.CategorieId,
            DateAjout = DateTime.UtcNow,
        };

        _db.Livres.Add(livre);
        await _db.SaveChangesAsync(ct);

        return (await GetLivreAsync(livre.Id, null, ct)).ToDto();
    }

    public async Task<LivreDto> UpdateAsync(int id, UpdateLivreRequest request, CancellationToken ct = default)
    {
        var livre = await _db.Livres.FirstOrDefaultAsync(l => l.Id == id, ct)
                    ?? throw AppException.NotFound("Ce livre n'existe pas.");

        var isbn = NormalizeIsbn(request.Isbn);
        await EnsureIsbnAvailableAsync(isbn, id, ct);
        await EnsureReferencesExistAsync(request.AuteurId, request.CategorieId, ct);

        // Copies currently on loan must remain covered by the new total.
        var exemplairesEmpruntes = livre.NombreExemplaires - livre.NombreDisponibles;
        if (request.NombreExemplaires < exemplairesEmpruntes)
        {
            throw AppException.Conflict(
                $"Le total ne peut pas être inférieur aux {exemplairesEmpruntes} exemplaire(s) actuellement emprunté(s).",
                "STOCK_INCOHERENT");
        }

        livre.Titre = request.Titre.Trim();
        livre.ISBN = isbn;
        livre.Resume = request.Resume.Trim();
        livre.AnneePublication = request.AnneePublication;
        var deltaExemplaires = request.NombreExemplaires - livre.NombreExemplaires;
        livre.NombreExemplaires = request.NombreExemplaires;
        livre.NombreDisponibles = Math.Clamp(livre.NombreDisponibles + deltaExemplaires, 0, request.NombreExemplaires);
        livre.CouvertureUrl = string.IsNullOrWhiteSpace(request.CouvertureUrl) ? null : request.CouvertureUrl.Trim();
        livre.AuteurId = request.AuteurId;
        livre.CategorieId = request.CategorieId;

        await _db.SaveChangesAsync(ct);
        return (await GetLivreAsync(livre.Id, null, ct)).ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var livre = await _db.Livres
            .Include(l => l.Emprunts)
            .FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw AppException.NotFound("Ce livre n'existe pas.");

        if (livre.Emprunts.Count > 0)
        {
            throw AppException.Conflict(
                "Ce livre possède un historique d'emprunts et ne peut pas être supprimé.",
                "LIVRE_AVEC_EMPRUNTS");
        }

        _db.Livres.Remove(livre);
        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureIsbnAvailableAsync(string isbn, int? ignoreId, CancellationToken ct)
    {
        var exists = await _db.Livres.AnyAsync(
            l => l.ISBN == isbn && (!ignoreId.HasValue || l.Id != ignoreId.Value), ct);

        if (exists)
        {
            throw AppException.Conflict("Cet ISBN est déjà utilisé par un autre livre.", "ISBN_DUPLIQUE");
        }
    }

    private async Task EnsureReferencesExistAsync(int auteurId, int categorieId, CancellationToken ct)
    {
        if (!await _db.Auteurs.AnyAsync(a => a.Id == auteurId, ct))
        {
            throw AppException.BadRequest("L'auteur sélectionné n'existe pas.", "AUTEUR_INCONNU");
        }

        if (!await _db.Categories.AnyAsync(c => c.Id == categorieId, ct))
        {
            throw AppException.BadRequest("La catégorie sélectionnée n'existe pas.", "CATEGORIE_INCONNUE");
        }
    }
}
