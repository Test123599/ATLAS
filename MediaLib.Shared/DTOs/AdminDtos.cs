using System.ComponentModel.DataAnnotations;

namespace MediaLib.Shared.DTOs;

public sealed record UtilisateurDto(
    string Id,
    string Email,
    string Prenom,
    string Nom,
    DateTime DateInscription,
    bool EstActif,
    bool EstVerrouille,
    IReadOnlyList<string> Roles,
    int NombreEmpruntsActifs);

public sealed record UtilisateurDetailDto(
    string Id,
    string Email,
    string Prenom,
    string Nom,
    DateTime DateInscription,
    bool EstActif,
    bool EstVerrouille,
    DateTime? FinVerrouillage,
    IReadOnlyList<string> Roles,
    IReadOnlyList<EmpruntDto> Emprunts);

public sealed class UpdateUtilisateurRequest
{
    [Required, StringLength(120)]
    public string Prenom { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Nom { get; set; } = string.Empty;
}

public sealed class UpdateRolesRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "Au moins un rôle doit être attribué.")]
    public List<string> Roles { get; set; } = [];
}

public sealed class SetActifRequest
{
    public bool EstActif { get; set; }
}

public sealed class UtilisateurQueryDto
{
    public string? Recherche { get; set; }
    public string? Role { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int TaillePage { get; set; } = 20;
}
