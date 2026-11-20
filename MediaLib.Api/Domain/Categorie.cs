namespace MediaLib.Api.Domain;

/// <summary>A thematic category used to organise the catalogue.</summary>
public class Categorie
{
    public int Id { get; set; }

    public string Libelle { get; set; } = string.Empty;

    public ICollection<Livre> Livres { get; set; } = new List<Livre>();
}
