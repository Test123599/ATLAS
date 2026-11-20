namespace MediaLib.Api.Domain;

/// <summary>An author of one or more books in the collection.</summary>
public class Auteur
{
    public int Id { get; set; }

    public string Nom { get; set; } = string.Empty;

    public string Prenom { get; set; } = string.Empty;

    public string Biographie { get; set; } = string.Empty;

    public ICollection<Livre> Livres { get; set; } = new List<Livre>();

    public string NomComplet => string.IsNullOrWhiteSpace(Prenom) ? Nom : $"{Prenom} {Nom}";
}
