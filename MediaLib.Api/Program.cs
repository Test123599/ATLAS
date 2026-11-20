using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using MediaLib.Api.Common;
using MediaLib.Api.Configuration;
using MediaLib.Api.Data;
using MediaLib.Api.Domain;
using MediaLib.Api.Services;
using MediaLib.Shared.Constants;
using MediaLib.Shared.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration -------------------------------------------------------
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
jwt.Validate();
builder.Services.AddSingleton(jwt);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=atlas-medialib.db";

// ---- Persistence & Identity ------------------------------------------------
builder.Services.AddDbContext<MediaLibDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddIdentity<Utilisateur, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 1;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<MediaLibDbContext>()
    .AddDefaultTokenProviders();

// AddIdentity registers the Identity.Application cookie as the default scheme.
// This API is token-only, so force JWT as the default at post-configure time
// (after Identity's own AuthenticationOptions configuration has run).
builder.Services.PostConfigure<AuthenticationOptions>(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
});

// ---- Authentication (JWT) --------------------------------------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.PeutGererCatalogue, policy =>
        policy.RequireRole(Roles.Bibliothecaire, Roles.Admin));
    options.AddPolicy(Policies.PeutEmprunter, policy =>
        policy.RequireRole(Roles.Membre, Roles.Bibliothecaire, Roles.Admin));
    options.AddPolicy(Policies.EstAdmin, policy => policy.RequireRole(Roles.Admin));
});

// ---- Business services -----------------------------------------------------
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICatalogueService, CatalogueService>();
builder.Services.AddScoped<IAuteurService, AuteurService>();
builder.Services.AddScoped<ICategorieService, CategorieService>();
builder.Services.AddScoped<IEmpruntService, EmpruntService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// ---- API behaviour, errors, docs, rate limiting ----------------------------
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = ApiValidationResponse.Create;
});

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        var error = new ApiError(429, "TAUX_LIMITE",
            "Trop de tentatives. Veuillez patienter avant de réessayer.", context.HttpContext.TraceIdentifier);
        await context.HttpContext.Response.WriteAsJsonAsync(error, token);
    };

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "inconnu",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 15,
                Window = TimeSpan.FromMinutes(5),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options => options.AddPolicy("AtlasDev", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
}

var app = builder.Build();

// Reliable initialization: migrate + seed before serving requests.
await DbInitializer.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("ATLAS MediaLib — API Reference");
        options.WithTheme(ScalarTheme.DeepSpace);
        options.AddPreferredSecuritySchemes("Bearer");
    });
}

app.UseExceptionHandler();
// Local Web apps call the documented http://localhost:5196 base URL. Redirecting
// that request to the https profile port can strip the bearer header and surface
// as a 401 on every authenticated operation.
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors("AtlasDev");
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>Exposed for future WebApplicationFactory-based integration tests.</summary>
public partial class Program;
