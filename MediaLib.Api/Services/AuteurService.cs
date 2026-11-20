using MediaLib.Api.Common;
using MediaLib.Api.Data;
using MediaLib.Api.Mapping;
using MediaLib.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MediaLib.Api.Services;

public interface IAuteurService
{
    Task<IReadOnlyList<AuteurDto>> GetAllAsync(CancellationToken ct = default);
    Task<AuteurDto> GetAsync(int id, CancellationToken ct = default);
    Task<AuteurDto> CreateAsync(CreateAuteurRequest request, CancellationToken ct = default);
    Task<AuteurDto> UpdateAsync(int id, UpdateAuteurRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class AuteurService : IAuteurService
{
    private readonly MediaLibDbContext _db;

    public AuteurService(MediaLibDbContext db) => _db = db;

    public async Task<IReadOnlyList<AuteurDto>> GetAllAsync(CancellationToken ct = default)
    {
        var auteurs = await _db.Auteurs.AsNoTracking()
            .Include(a => a.Livres)
            .OrderBy(a => a.Nom).ThenBy(a => a.Prenom)
            .ToListAsync(ct);

        return auteurs.Select(a => a.ToDto()).ToList();
    }

    public async Task<AuteurDto> GetAsync(int id, CancellationToken ct = default)
    {
        var auteur = await _db.Auteurs.AsNoTracking()
            .Include(a => a.Livres)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw AppException.NotFound("Cet auteur n'existe pas.");

        return auteur.ToDto();
    }

    public async Task<AuteurDto> CreateAsync(CreateAuteurRequest request, CancellationToken ct = default)
    {
        var auteur = new Domain.Auteur
        {
            Nom = request.Nom.Trim(),
            Prenom = request.Prenom.Trim(),
            Biographie = (request.Biographie ?? string.Empty).Trim(),
        };

        _db.Auteurs.Add(auteur);
        await _db.SaveChangesAsync(ct);
        return auteur.ToDto();
    }

    public async Task<AuteurDto> UpdateAsync(int id, UpdateAuteurRequest request, CancellationToken ct = default)
    {
        var auteur = await _db.Auteurs.FirstOrDefaultAsync(a => a.Id == id, ct)
                     ?? throw AppException.NotFound("Cet auteur n'existe pas.");

        auteur.Nom = request.Nom.Trim();
        auteur.Prenom = request.Prenom.Trim();
        auteur.Biographie = (request.Biographie ?? string.Empty).Trim();
        await _db.SaveChangesAsync(ct);

        return (await GetAsync(id, ct));
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var auteur = await _db.Auteurs
            .Include(a => a.Livres)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw AppException.NotFound("Cet auteur n'existe pas.");

        if (auteur.Livres.Count > 0)
        {
            throw AppException.Conflict(
                $"Impossible de supprimer cet auteur : {auteur.Livres.Count} livre(s) lui sont associés.",
                "AUTEUR_AVEC_LIVRES");
        }

        _db.Auteurs.Remove(auteur);
        await _db.SaveChangesAsync(ct);
    }
}
