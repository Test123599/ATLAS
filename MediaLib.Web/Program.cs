using MediaLib.Shared.Constants;
using MediaLib.Web.Components;
using MediaLib.Web.Services;
using MediaLib.Web.Services.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Interactive Server keeps tokens and API calls on the server side of the SignalR
// circuit; only an opaque browser-storage copy is used to survive page reloads.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication(AtlasRouteAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, AtlasRouteAuthenticationHandler>(
        AtlasRouteAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddAuthorizationCore(options =>
{
    // Mirror the API policies so AuthorizeView and [Authorize(Policy=...)] work client-side too.
    options.AddPolicy(Policies.PeutGererCatalogue, policy => policy.RequireRole(Roles.Bibliothecaire, Roles.Admin));
    options.AddPolicy(Policies.PeutEmprunter, policy => policy.RequireRole(Roles.Membre, Roles.Bibliothecaire, Roles.Admin));
    options.AddPolicy(Policies.EstAdmin, policy => policy.RequireRole(Roles.Admin));
});
builder.Services.AddCascadingAuthenticationState();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5196";
builder.Services.AddHttpClient("AtlasApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddScoped<AtlasTokenStore>();
builder.Services.AddScoped<AtlasApiClient>();
builder.Services.AddScoped<AtlasAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AtlasAuthenticationStateProvider>());

builder.Services.AddScoped<IAuthApi, AuthApi>();
builder.Services.AddScoped<IBooksApi, BooksApi>();
builder.Services.AddScoped<IAuthorsApi, AuthorsApi>();
builder.Services.AddScoped<ICategoriesApi, CategoriesApi>();
builder.Services.AddScoped<ILoansApi, LoansApi>();
builder.Services.AddScoped<IAdminApi, AdminApi>();
builder.Services.AddScoped<IDashboardApi, DashboardApi>();
builder.Services.AddScoped<ToastService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
