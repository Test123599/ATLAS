namespace MediaLib.Api.Domain;

/// <summary>A loan of one book copy to one member. 21 days borrowing period.</summary>
public class Emprunt
{
    public int Id { get; set; }

    public int LivreId { get; set; }

    public Livre Livre { get; set; } = null!;

    public string UtilisateurId { get; set; } = string.Empty;

    public Utilisateur Utilisateur { get; set; } = null!;

    public DateTime DateEmprunt { get; set; } = DateTime.UtcNow;

    public DateTime DateRetourPrevue { get; set; }

    public DateTime? DateRetourEffective { get; set; }

    public bool EstEnRetard(DateTime? now = null)
        => DateRetourEffective is null && (now ?? DateTime.UtcNow) > DateRetourPrevue;
}
