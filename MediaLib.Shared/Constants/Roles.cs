namespace MediaLib.Shared.Constants;

/// <summary>Canonical role and policy names shared by the API and the Blazor UI.</summary>
public static class Roles
{
    public const string Membre = "Membre";
    public const string Bibliothecaire = "Bibliothecaire";
    public const string Admin = "Admin";

    public static readonly string[] All = [Membre, Bibliothecaire, Admin];

    public static bool EstValide(string role) => All.Contains(role, StringComparer.Ordinal);
}

public static class Policies
{
    public const string PeutGererCatalogue = "PeutGererCatalogue";
    public const string PeutEmprunter = "PeutEmprunter";
    public const string EstAdmin = "EstAdmin";
}
