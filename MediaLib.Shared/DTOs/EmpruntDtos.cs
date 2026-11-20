using System.ComponentModel.DataAnnotations;

namespace MediaLib.Shared.DTOs;

/// <summary>Loan representation returned to members and librarians.</summary>
public sealed record EmpruntDto(
    int Id,
    int LivreId,
    string LivreTitre,
    string AuteurNom,
    string Isbn,
    string? CouvertureUrl,
    DateTime DateEmprunt,
    DateTime DateRetourPrevue,
    DateTime? DateRetourEffective,
    string UtilisateurId,
    string UtilisateurNom)
{
    public bool EstEnRetard => DateRetourEffective is null && DateTime.UtcNow > DateRetourPrevue;

    public int JoursRestants => DateRetourEffective is not null
        ? 0
        : (int)Math.Ceiling((DateRetourPrevue - DateTime.UtcNow).TotalDays);
}

/// <summary>Active loans and history for the authenticated member.</summary>
public sealed record MesEmpruntsDto(
    IReadOnlyList<EmpruntDto> Actifs,
    IReadOnlyList<EmpruntDto> Historique,
    int LimiteEmprunts);

public sealed class CreateEmpruntRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Sélectionnez un livre.")]
    public int LivreId { get; set; }
}

public sealed class EmpruntQueryDto
{
    /// <summary>When true, only overdue active loans are returned.</summary>
    public bool? EnRetard { get; set; }

    /// <summary>When true, only active loans; when false, only returned loans.</summary>
    public bool? Actif { get; set; }

    public string? Recherche { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int TaillePage { get; set; } = 20;
}
