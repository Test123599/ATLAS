namespace MediaLib.Api.Domain;

/// <summary>
/// A rotated refresh token. Only a SHA-256 hash of the opaque token is persisted,
/// so a database leak cannot be used to mint access tokens.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }

    public string UtilisateurId { get; set; } = string.Empty;

    public Utilisateur Utilisateur { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreeLe { get; set; } = DateTime.UtcNow;

    public DateTime ExpireLe { get; set; }

    public DateTime? RevoqueLe { get; set; }

    /// <summary>Hash of the token that replaced this one (rotation chain auditing).</summary>
    public string? RemplaceParHash { get; set; }
}
