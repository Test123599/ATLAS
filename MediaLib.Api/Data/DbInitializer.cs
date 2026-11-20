using MediaLib.Api.Domain;
using MediaLib.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MediaLib.Api.Data;

/// <summary>
/// Applies migrations and seeds roles, three demo accounts and the curated collection
/// once. Re-running the app never duplicates data.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<MediaLibDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in SeedData.Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var created = await roleManager.CreateAsync(new IdentityRole(role));
                if (!created.Succeeded)
                {
                    throw new InvalidOperationException($"Cannot create role {role}: {string.Join(", ", created.Errors.Select(e => e.Description))}");
                }
            }
        }

        var userManager = provider.GetRequiredService<UserManager<Utilisateur>>();
        foreach (var seed in SeedData.Users)
        {
            var existing = await userManager.FindByEmailAsync(seed.Email);
            if (existing is null)
            {
                existing = new Utilisateur
                {
                    UserName = seed.Email,
                    Email = seed.Email,
                    EmailConfirmed = true,
                    Nom = seed.Nom,
                    Prenom = seed.Prenom,
                    DateInscription = DateTime.UtcNow.AddDays(-90),
                    EstActif = true,
                };

                var created = await userManager.CreateAsync(existing, SeedData.DevelopmentPassword);
                if (!created.Succeeded)
                {
                    throw new InvalidOperationException($"Cannot create user {seed.Email}: {string.Join(", ", created.Errors.Select(e => e.Description))}");
                }
            }

            if (!await userManager.IsInRoleAsync(existing, seed.Role))
            {
                await userManager.AddToRoleAsync(existing, seed.Role);
            }
        }

        if (await db.Auteurs.AnyAsync() || await db.Livres.AnyAsync())
        {
            return;
        }

        var auteurs = SeedData.Auteurs.Select(a => new Auteur
        {
            Nom = a.Nom,
            Prenom = a.Prenom,
            Biographie = a.Biographie,
        }).ToList();
        db.Auteurs.AddRange(auteurs);

        var categories = SeedData.Categories.Select(c => new Categorie { Libelle = c }).ToList();
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        var livres = SeedData.Livres.Select((l, i) => new Livre
        {
            Titre = l.Titre,
            ISBN = l.Isbn,
            Resume = l.Resume,
            AnneePublication = l.Annee,
            NombreExemplaires = l.Stock,
            NombreDisponibles = l.Dispo,
            CouvertureUrl = null,
            DateAjout = DateTime.UtcNow.AddDays(-l.Ajout),
            AuteurId = auteurs[l.Auteur].Id,
            CategorieId = categories[l.Categorie].Id,
        }).ToList();
        db.Livres.AddRange(livres);
        await db.SaveChangesAsync();

        // A small, realistic loan history for the demo member: two active loans,
        // one overdue loan and one completed loan. Stock is adjusted accordingly.
        var membre = await userManager.FindByEmailAsync(SeedData.Users[2].Email);
        if (membre is not null)
        {
            var maintenant = DateTime.UtcNow;
            var emprunts = new List<Emprunt>
            {
                NewLoan(livres[2], membre.Id, maintenant.AddDays(-16), maintenant.AddDays(5)),
                NewLoan(livres[10], membre.Id, maintenant.AddDays(-19), maintenant.AddDays(2)),
                NewLoan(livres[24], membre.Id, maintenant.AddDays(-24), maintenant.AddDays(-3)),
                NewLoan(livres[0], membre.Id, maintenant.AddDays(-45), maintenant.AddDays(-24), maintenant.AddDays(-22)),
            };

            foreach (var e in emprunts.Where(e => e.DateRetourEffective is null))
            {
                e.Livre.NombreDisponibles = Math.Max(0, e.Livre.NombreDisponibles - 1);
            }

            db.Emprunts.AddRange(emprunts);
            await db.SaveChangesAsync();
        }
    }

    private static Emprunt NewLoan(Livre livre, string utilisateurId, DateTime emprunt, DateTime echeance, DateTime? retour = null)
        => new()
        {
            Livre = livre,
            LivreId = livre.Id,
            UtilisateurId = utilisateurId,
            DateEmprunt = emprunt,
            DateRetourPrevue = echeance,
            DateRetourEffective = retour,
        };
}
