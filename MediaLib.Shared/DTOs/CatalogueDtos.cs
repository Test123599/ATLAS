using System.ComponentModel.DataAnnotations;
using MediaLib.Shared.Validation;

namespace MediaLib.Shared.DTOs;

/// <summary>List/card representation of a book. No EF entities cross the wire.</summary>
public sealed record LivreDto(
    int Id,
    string Titre,
    string Isbn,
    string Resume,
    int AnneePublication,
    int NombreExemplaires,
    int NombreDisponibles,
    string? CouvertureUrl,
    DateTime DateAjout,
    int AuteurId,
    string AuteurNom,
    int CategorieId,
    string CategorieLibelle)
{
    public bool EstDisponible => NombreDisponibles > 0;
}

/// <summary>Detailed book representation used by the book details page.</summary>
public sealed record LivreDetailDto(
    int Id,
    string Titre,
    string Isbn,
    string Resume,
    int AnneePublication,
    int NombreExemplaires,
    int NombreDisponibles,
    string? CouvertureUrl,
    DateTime DateAjout,
    int AuteurId,
    string AuteurNom,
    string AuteurBiographie,
    int CategorieId,
    string CategorieLibelle,
    int NombreEmpruntsActifs,
    bool EmprunteParUtilisateurCourant)
{
    public bool EstDisponible => NombreDisponibles > 0;
}

public sealed class CreateLivreRequest
{
    [Required(ErrorMessage = "Le titre est obligatoire.")]
    [StringLength(250, ErrorMessage = "Le titre ne peut pas dépasser 250 caractères.")]
    public string Titre { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'ISBN est obligatoire.")]
    [Isbn(ErrorMessage = "L'ISBN doit être un ISBN-10 ou ISBN-13 valide (clé de contrôle incluse).")]
    public string Isbn { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le résumé est obligatoire.")]
    [StringLength(3000, ErrorMessage = "Le résumé ne peut pas dépasser 3000 caractères.")]
    public string Resume { get; set; } = string.Empty;

    [Range(1450, 2100, ErrorMessage = "L'année de publication doit être comprise entre 1450 et 2100.")]
    public int AnneePublication { get; set; }

    [Range(1, 1000, ErrorMessage = "Le nombre d'exemplaires doit être compris entre 1 et 1000.")]
    public int NombreExemplaires { get; set; } = 1;

    [Url(ErrorMessage = "L'URL de couverture doit être une URL valide.")]
    [StringLength(500)]
    public string? CouvertureUrl { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Sélectionnez un auteur.")]
    public int AuteurId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Sélectionnez une catégorie.")]
    public int CategorieId { get; set; }
}

public sealed class UpdateLivreRequest
{
    [Required(ErrorMessage = "Le titre est obligatoire.")]
    [StringLength(250)]
    public string Titre { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'ISBN est obligatoire.")]
    [Isbn(ErrorMessage = "L'ISBN doit être un ISBN-10 ou ISBN-13 valide (clé de contrôle incluse).")]
    public string Isbn { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le résumé est obligatoire.")]
    [StringLength(3000)]
    public string Resume { get; set; } = string.Empty;

    [Range(1450, 2100)]
    public int AnneePublication { get; set; }

    /// <summary>Total copies. Cannot be lower than copies currently on loan.</summary>
    [Range(0, 1000)]
    public int NombreExemplaires { get; set; }

    [Url]
    [StringLength(500)]
    public string? CouvertureUrl { get; set; }

    [Range(1, int.MaxValue)]
    public int AuteurId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategorieId { get; set; }
}

/// <summary>Query parameters accepted by GET /api/livres.</summary>
public sealed class LivreQueryDto
{
    public string? Recherche { get; set; }
    public int? CategorieId { get; set; }
    public int? AuteurId { get; set; }
    public bool? DisponibleUniquement { get; set; }

    /// <summary>titre | auteur | annee | ajout | disponibilite (prefix with - for descending).</summary>
    public string? Tri { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int TaillePage { get; set; } = 12;
}

public sealed record AuteurDto(int Id, string Nom, string Prenom, string NomComplet, string Biographie, int NombreLivres);

public sealed class CreateAuteurRequest
{
    [Required, StringLength(120)]
    public string Nom { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Prenom { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Biographie { get; set; } = string.Empty;
}

public sealed class UpdateAuteurRequest
{
    [Required, StringLength(120)]
    public string Nom { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Prenom { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Biographie { get; set; } = string.Empty;
}

public sealed record CategorieDto(int Id, string Libelle, int NombreLivres);

public sealed class CreateCategorieRequest
{
    [Required, StringLength(100)]
    public string Libelle { get; set; } = string.Empty;
}

public sealed class UpdateCategorieRequest
{
    [Required, StringLength(100)]
    public string Libelle { get; set; } = string.Empty;
}
