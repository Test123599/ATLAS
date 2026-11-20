using Microsoft.AspNetCore.Identity;

namespace MediaLib.Api.Domain;

/// <summary>Application user; extends Identity with the library-specific profile fields.</summary>
public class Utilisateur : IdentityUser
{
    public string Nom { get; set; } = string.Empty;

    public string Prenom { get; set; } = string.Empty;

    public DateTime DateInscription { get; set; } = DateTime.UtcNow;

    public bool EstActif { get; set; } = true;

    public ICollection<Emprunt> Emprunts { get; set; } = new List<Emprunt>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public string NomComplet => string.IsNullOrWhiteSpace(Prenom) ? Nom : $"{Prenom} {Nom}";
}
