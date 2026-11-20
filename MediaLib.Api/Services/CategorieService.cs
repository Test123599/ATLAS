using MediaLib.Api.Common;
using MediaLib.Api.Data;
using MediaLib.Api.Mapping;
using MediaLib.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MediaLib.Api.Services;

public interface ICategorieService
{
    Task<IReadOnlyList<CategorieDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategorieDto> GetAsync(int id, CancellationToken ct = default);
    Task<CategorieDto> CreateAsync(CreateCategorieRequest request, CancellationToken ct = default);
    Task<CategorieDto> UpdateAsync(int id, UpdateCategorieRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class CategorieService : ICategorieService
{
    private readonly MediaLibDbContext _db;

    public CategorieService(MediaLibDbContext db) => _db = db;

    public async Task<IReadOnlyList<CategorieDto>> GetAllAsync(CancellationToken ct = default)
    {
        var categories = await _db.Categories.AsNoTracking()
            .Include(c => c.Livres)
            .OrderBy(c => c.Libelle)
            .ToListAsync(ct);

        return categories.Select(c => c.ToDto()).ToList();
    }

    public async Task<CategorieDto> GetAsync(int id, CancellationToken ct = default)
    {
        var categorie = await _db.Categories.AsNoTracking()
            .Include(c => c.Livres)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw AppException.NotFound("Cette catégorie n'existe pas.");

        return categorie.ToDto();
    }

    public async Task<CategorieDto> CreateAsync(CreateCategorieRequest request, CancellationToken ct = default)
    {
        var libelle = request.Libelle.Trim();
        if (await _db.Categories.AnyAsync(c => c.Libelle == libelle, ct))
        {
            throw AppException.Conflict("Cette catégorie existe déjà.", "CATEGORIE_DUPLIQUEE");
        }

        var categorie = new Domain.Categorie { Libelle = libelle };
        _db.Categories.Add(categorie);
        await _db.SaveChangesAsync(ct);

        return categorie.ToDto();
    }

    public async Task<CategorieDto> UpdateAsync(int id, UpdateCategorieRequest request, CancellationToken ct = default)
    {
        var categorie = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
                        ?? throw AppException.NotFound("Cette catégorie n'existe pas.");

        var libelle = request.Libelle.Trim();
        if (await _db.Categories.AnyAsync(c => c.Libelle == libelle && c.Id != id, ct))
        {
            throw AppException.Conflict("Cette catégorie existe déjà.", "CATEGORIE_DUPLIQUEE");
        }

        categorie.Libelle = libelle;
        await _db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var categorie = await _db.Categories
            .Include(c => c.Livres)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw AppException.NotFound("Cette catégorie n'existe pas.");

        if (categorie.Livres.Count > 0)
        {
            throw AppException.Conflict(
                $"Impossible de supprimer cette catégorie : {categorie.Livres.Count} livre(s) lui sont associés.",
                "CATEGORIE_AVEC_LIVRES");
        }

        _db.Categories.Remove(categorie);
        await _db.SaveChangesAsync(ct);
    }
}
