namespace MediaLib.Shared.DTOs;

/// <summary>Standard paginated envelope used by every collection endpoint.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int TaillePage, int TotalCount)
{
    public int TotalPages => TaillePage == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)TaillePage);
    public bool APrecedent => Page > 1;
    public bool ASuivant => Page < TotalPages;
}

/// <summary>Consistent error payload produced by the API exception handler.</summary>
public sealed record ApiError(int Status, string Code, string Message, string? TraceId, Dictionary<string, string[]>? Errors = null);

public sealed record CatalogueStatsDto(
    int TotalLivres,
    int ExemplairesTotal,
    int ExemplairesDisponibles,
    int NombreAuteurs,
    int NombreCategories);

public sealed record ActiviteDto(
    int EmpruntId,
    int LivreId,
    string LivreTitre,
    string UtilisateurNom,
    DateTime Date,
    string Type,
    bool EstEnRetard);

public sealed record MemberDashboardDto(
    string Prenom,
    int NombreEmpruntsActifs,
    int LimiteEmprunts,
    int NombreEnRetard,
    int NombreHistorique,
    DateTime? ProchaineEcheance,
    IReadOnlyList<EmpruntDto> EmpruntsActifs,
    IReadOnlyList<EmpruntDto> HistoriqueRecent,
    IReadOnlyList<LivreDto> LivresRecents);

public sealed record LibrarianDashboardDto(
    CatalogueStatsDto Catalogue,
    int EmpruntsActifs,
    int EmpruntsEnRetard,
    int EmpruntsAujourdhui,
    int RetoursCetteSemaine,
    IReadOnlyList<ActiviteDto> ActiviteRecente,
    IReadOnlyList<EmpruntDto> RetoursRecents);
