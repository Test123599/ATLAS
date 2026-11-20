using MediaLib.Shared.DTOs;

namespace MediaLib.Web.Services;

public interface IAuthApi
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserProfileDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<UserProfileDto> GetMeAsync(CancellationToken ct = default);
    Task<UserProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
}

public sealed class AuthApi : IAuthApi
{
    private readonly AtlasApiClient _api;
    public AuthApi(AtlasApiClient api) => _api = api;

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        => _api.PostAsync<AuthResponse>("api/auth/connexion", request, authenticated: false, ct);

    public Task<UserProfileDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        => _api.PostAsync<UserProfileDto>("api/auth/inscription", request, authenticated: false, ct);

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
        => await _api.PostAsync<object?>("api/auth/deconnexion", new RefreshRequest { RefreshToken = refreshToken }, true, ct);

    public Task<UserProfileDto> GetMeAsync(CancellationToken ct = default)
        => _api.GetAsync<UserProfileDto>("api/auth/moi", ct);

    public Task<UserProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
        => _api.PutAsync<UserProfileDto>("api/auth/profil", request, ct);

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
        => await _api.PutAsync<object?>("api/auth/mot-de-passe", request, ct);
}

public interface IBooksApi
{
    Task<PagedResult<LivreDto>> GetAsync(LivreQueryDto query, CancellationToken ct = default);
    Task<LivreDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<LivreDto>> GetRecentsAsync(int count, CancellationToken ct = default);
    Task<LivreDto> CreateAsync(CreateLivreRequest request, CancellationToken ct = default);
    Task<LivreDto> UpdateAsync(int id, UpdateLivreRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class BooksApi : IBooksApi
{
    private readonly AtlasApiClient _api;
    public BooksApi(AtlasApiClient api) => _api = api;

    public Task<PagedResult<LivreDto>> GetAsync(LivreQueryDto query, CancellationToken ct = default)
        => _api.GetOptionalAsync<PagedResult<LivreDto>>("api/livres" + QueryStringHelper.Build(
            ("recherche", query.Recherche),
            ("categorieId", query.CategorieId),
            ("auteurId", query.AuteurId),
            ("disponibleUniquement", query.DisponibleUniquement),
            ("tri", query.Tri),
            ("page", query.Page),
            ("taillePage", query.TaillePage)), ct);

    public Task<LivreDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
        => _api.GetOptionalAsync<LivreDetailDto>($"api/livres/{id}", ct);

    public async Task<IReadOnlyList<LivreDto>> GetRecentsAsync(int count, CancellationToken ct = default)
    {
        var result = await GetAsync(new LivreQueryDto { Tri = "-ajout", Page = 1, TaillePage = count }, ct);
        return result.Items;
    }

    public Task<LivreDto> CreateAsync(CreateLivreRequest request, CancellationToken ct = default)
        => _api.PostAsync<LivreDto>("api/livres", request, authenticated: true, ct);

    public Task<LivreDto> UpdateAsync(int id, UpdateLivreRequest request, CancellationToken ct = default)
        => _api.PutAsync<LivreDto>($"api/livres/{id}", request, ct);

    public async Task DeleteAsync(int id, CancellationToken ct = default)
        => await _api.DeleteAsync($"api/livres/{id}", ct);
}

public interface IAuthorsApi
{
    Task<IReadOnlyList<AuteurDto>> GetAllAsync(CancellationToken ct = default);
    Task<AuteurDto> GetAsync(int id, CancellationToken ct = default);
    Task<AuteurDto> CreateAsync(CreateAuteurRequest request, CancellationToken ct = default);
    Task<AuteurDto> UpdateAsync(int id, UpdateAuteurRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class AuthorsApi : IAuthorsApi
{
    private readonly AtlasApiClient _api;
    public AuthorsApi(AtlasApiClient api) => _api = api;

    public Task<IReadOnlyList<AuteurDto>> GetAllAsync(CancellationToken ct = default)
        => _api.GetOptionalAsync<IReadOnlyList<AuteurDto>>("api/auteurs", ct);
    public Task<AuteurDto> GetAsync(int id, CancellationToken ct = default)
        => _api.GetOptionalAsync<AuteurDto>($"api/auteurs/{id}", ct);
    public Task<AuteurDto> CreateAsync(CreateAuteurRequest request, CancellationToken ct = default)
        => _api.PostAsync<AuteurDto>("api/auteurs", request, authenticated: true, ct);
    public Task<AuteurDto> UpdateAsync(int id, UpdateAuteurRequest request, CancellationToken ct = default)
        => _api.PutAsync<AuteurDto>($"api/auteurs/{id}", request, ct);
    public async Task DeleteAsync(int id, CancellationToken ct = default)
        => await _api.DeleteAsync($"api/auteurs/{id}", ct);
}

public interface ICategoriesApi
{
    Task<IReadOnlyList<CategorieDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategorieDto> GetAsync(int id, CancellationToken ct = default);
    Task<CategorieDto> CreateAsync(CreateCategorieRequest request, CancellationToken ct = default);
    Task<CategorieDto> UpdateAsync(int id, UpdateCategorieRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class CategoriesApi : ICategoriesApi
{
    private readonly AtlasApiClient _api;
    public CategoriesApi(AtlasApiClient api) => _api = api;

    public Task<IReadOnlyList<CategorieDto>> GetAllAsync(CancellationToken ct = default)
        => _api.GetOptionalAsync<IReadOnlyList<CategorieDto>>("api/categories", ct);
    public Task<CategorieDto> GetAsync(int id, CancellationToken ct = default)
        => _api.GetOptionalAsync<CategorieDto>($"api/categories/{id}", ct);
    public Task<CategorieDto> CreateAsync(CreateCategorieRequest request, CancellationToken ct = default)
        => _api.PostAsync<CategorieDto>("api/categories", request, authenticated: true, ct);
    public Task<CategorieDto> UpdateAsync(int id, UpdateCategorieRequest request, CancellationToken ct = default)
        => _api.PutAsync<CategorieDto>($"api/categories/{id}", request, ct);
    public async Task DeleteAsync(int id, CancellationToken ct = default)
        => await _api.DeleteAsync($"api/categories/{id}", ct);
}

public interface ILoansApi
{
    Task<EmpruntDto> BorrowAsync(int livreId, CancellationToken ct = default);
    Task<MesEmpruntsDto> GetMineAsync(CancellationToken ct = default);
    Task<PagedResult<EmpruntDto>> GetAllAsync(EmpruntQueryDto query, CancellationToken ct = default);
    Task<EmpruntDto> ReturnAsync(int id, CancellationToken ct = default);
}

public sealed class LoansApi : ILoansApi
{
    private readonly AtlasApiClient _api;
    public LoansApi(AtlasApiClient api) => _api = api;

    public Task<EmpruntDto> BorrowAsync(int livreId, CancellationToken ct = default)
        => _api.PostAsync<EmpruntDto>("api/emprunts", new CreateEmpruntRequest { LivreId = livreId }, authenticated: true, ct);

    public Task<MesEmpruntsDto> GetMineAsync(CancellationToken ct = default)
        => _api.GetAsync<MesEmpruntsDto>("api/emprunts/moi", ct);

    public Task<PagedResult<EmpruntDto>> GetAllAsync(EmpruntQueryDto query, CancellationToken ct = default)
        => _api.GetAsync<PagedResult<EmpruntDto>>("api/emprunts" + QueryStringHelper.Build(
            ("enRetard", query.EnRetard),
            ("actif", query.Actif),
            ("recherche", query.Recherche),
            ("page", query.Page),
            ("taillePage", query.TaillePage)), ct);

    public Task<EmpruntDto> ReturnAsync(int id, CancellationToken ct = default)
        => _api.PostAsync<EmpruntDto>($"api/emprunts/{id}/retour", new { }, authenticated: true, ct);
}

public interface IAdminApi
{
    Task<PagedResult<UtilisateurDto>> GetUsersAsync(UtilisateurQueryDto query, CancellationToken ct = default);
    Task<UtilisateurDetailDto> GetUserAsync(string id, CancellationToken ct = default);
    Task<UtilisateurDto> UpdateUserAsync(string id, UpdateUtilisateurRequest request, CancellationToken ct = default);
    Task<UtilisateurDto> UpdateRolesAsync(string id, UpdateRolesRequest request, CancellationToken ct = default);
    Task<UtilisateurDto> SetActiveAsync(string id, bool estActif, CancellationToken ct = default);
}

public sealed class AdminApi : IAdminApi
{
    private readonly AtlasApiClient _api;
    public AdminApi(AtlasApiClient api) => _api = api;

    public Task<PagedResult<UtilisateurDto>> GetUsersAsync(UtilisateurQueryDto query, CancellationToken ct = default)
        => _api.GetAsync<PagedResult<UtilisateurDto>>("api/admin/utilisateurs" + QueryStringHelper.Build(
            ("recherche", query.Recherche), ("role", query.Role),
            ("page", query.Page), ("taillePage", query.TaillePage)), ct);

    public Task<UtilisateurDetailDto> GetUserAsync(string id, CancellationToken ct = default)
        => _api.GetAsync<UtilisateurDetailDto>($"api/admin/utilisateurs/{Uri.EscapeDataString(id)}", ct);

    public Task<UtilisateurDto> UpdateUserAsync(string id, UpdateUtilisateurRequest request, CancellationToken ct = default)
        => _api.PutAsync<UtilisateurDto>($"api/admin/utilisateurs/{Uri.EscapeDataString(id)}", request, ct);

    public Task<UtilisateurDto> UpdateRolesAsync(string id, UpdateRolesRequest request, CancellationToken ct = default)
        => _api.PutAsync<UtilisateurDto>($"api/admin/utilisateurs/{Uri.EscapeDataString(id)}/roles", request, ct);

    public Task<UtilisateurDto> SetActiveAsync(string id, bool estActif, CancellationToken ct = default)
        => _api.PutAsync<UtilisateurDto>($"api/admin/utilisateurs/{Uri.EscapeDataString(id)}/actif",
            new SetActifRequest { EstActif = estActif }, ct);
}

public interface IDashboardApi
{
    Task<MemberDashboardDto> GetMemberAsync(CancellationToken ct = default);
    Task<LibrarianDashboardDto> GetLibrarianAsync(CancellationToken ct = default);
}

public sealed class DashboardApi : IDashboardApi
{
    private readonly AtlasApiClient _api;
    public DashboardApi(AtlasApiClient api) => _api = api;

    public Task<MemberDashboardDto> GetMemberAsync(CancellationToken ct = default)
        => _api.GetAsync<MemberDashboardDto>("api/dashboard/membre", ct);

    public Task<LibrarianDashboardDto> GetLibrarianAsync(CancellationToken ct = default)
        => _api.GetAsync<LibrarianDashboardDto>("api/dashboard/bibliothecaire", ct);
}
