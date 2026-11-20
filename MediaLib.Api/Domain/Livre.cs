namespace MediaLib.Api.Domain;

/// <summary>A book in the library collection, including its inventory state.</summary>
public class Livre
{
    public int Id { get; set; }

    public string Titre { get; set; } = string.Empty;

    /// <summary>Valid ISBN-10 or ISBN-13, stored without separators.</summary>
    public string ISBN { get; set; } = string.Empty;

    public string Resume { get; set; } = string.Empty;

    public int AnneePublication { get; set; }

    public int NombreExemplaires { get; set; }

    public int NombreDisponibles { get; set; }

    public string? CouvertureUrl { get; set; }

    public DateTime DateAjout { get; set; } = DateTime.UtcNow;

    public int AuteurId { get; set; }

    public Auteur Auteur { get; set; } = null!;

    public int CategorieId { get; set; }

    public Categorie Categorie { get; set; } = null!;

    public ICollection<Emprunt> Emprunts { get; set; } = new List<Emprunt>();
}
