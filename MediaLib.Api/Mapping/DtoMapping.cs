using MediaLib.Api.Domain;
using MediaLib.Shared.DTOs;

namespace MediaLib.Api.Mapping;

/// <summary>Entity-to-DTO projections. EF entities never leave the API project.</summary>
public static class DtoMapping
{
    public static LivreDto ToDto(this Livre livre) => new(
        livre.Id,
        livre.Titre,
        livre.ISBN,
        livre.Resume,
        livre.AnneePublication,
        livre.NombreExemplaires,
        livre.NombreDisponibles,
        livre.CouvertureUrl,
        livre.DateAjout,
        livre.AuteurId,
        livre.Auteur?.NomComplet ?? string.Empty,
        livre.CategorieId,
        livre.Categorie?.Libelle ?? string.Empty);

    public static LivreDetailDto ToDetailDto(this Livre livre, int nombreEmpruntsActifs, bool emprunteParMoi) => new(
        livre.Id,
        livre.Titre,
        livre.ISBN,
        livre.Resume,
        livre.AnneePublication,
        livre.NombreExemplaires,
        livre.NombreDisponibles,
        livre.CouvertureUrl,
        livre.DateAjout,
        livre.AuteurId,
        livre.Auteur?.NomComplet ?? string.Empty,
        livre.Auteur?.Biographie ?? string.Empty,
        livre.CategorieId,
        livre.Categorie?.Libelle ?? string.Empty,
        nombreEmpruntsActifs,
        emprunteParMoi);

    public static LivreDto ToDto(this LivreDetailDto detail) => new(
        detail.Id,
        detail.Titre,
        detail.Isbn,
        detail.Resume,
        detail.AnneePublication,
        detail.NombreExemplaires,
        detail.NombreDisponibles,
        detail.CouvertureUrl,
        detail.DateAjout,
        detail.AuteurId,
        detail.AuteurNom,
        detail.CategorieId,
        detail.CategorieLibelle);

    public static EmpruntDto ToDto(this Emprunt emprunt) => new(
        emprunt.Id,
        emprunt.LivreId,
        emprunt.Livre?.Titre ?? string.Empty,
        emprunt.Livre?.Auteur?.NomComplet ?? string.Empty,
        emprunt.Livre?.ISBN ?? string.Empty,
        emprunt.Livre?.CouvertureUrl,
        emprunt.DateEmprunt,
        emprunt.DateRetourPrevue,
        emprunt.DateRetourEffective,
        emprunt.UtilisateurId,
        emprunt.Utilisateur?.NomComplet ?? string.Empty);

    public static AuteurDto ToDto(this Auteur auteur) => new(
        auteur.Id,
        auteur.Nom,
        auteur.Prenom,
        auteur.NomComplet,
        auteur.Biographie,
        auteur.Livres.Count);

    public static CategorieDto ToDto(this Categorie categorie) => new(
        categorie.Id,
        categorie.Libelle,
        categorie.Livres.Count);

    public static UserProfileDto ToProfileDto(this Utilisateur utilisateur, IReadOnlyList<string> roles) => new(
        utilisateur.Id,
        utilisateur.Email ?? string.Empty,
        utilisateur.Prenom,
        utilisateur.Nom,
        utilisateur.DateInscription,
        roles);

    public static UtilisateurDto ToDto(this Utilisateur utilisateur, IReadOnlyList<string> roles, int empruntsActifs) => new(
        utilisateur.Id,
        utilisateur.Email ?? string.Empty,
        utilisateur.Prenom,
        utilisateur.Nom,
        utilisateur.DateInscription,
        utilisateur.EstActif,
        utilisateur.LockoutEnd is not null && utilisateur.LockoutEnd > DateTimeOffset.UtcNow,
        roles,
        empruntsActifs);

    public static UtilisateurDetailDto ToDetailDto(this Utilisateur utilisateur, IReadOnlyList<string> roles) => new(
        utilisateur.Id,
        utilisateur.Email ?? string.Empty,
        utilisateur.Prenom,
        utilisateur.Nom,
        utilisateur.DateInscription,
        utilisateur.EstActif,
        utilisateur.LockoutEnd is not null && utilisateur.LockoutEnd > DateTimeOffset.UtcNow,
        utilisateur.LockoutEnd?.UtcDateTime,
        roles,
        utilisateur.Emprunts.OrderByDescending(e => e.DateEmprunt).Select(e => e.ToDto()).ToList());
}
