using MediaLib.Api.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MediaLib.Api.Data;

/// <summary>EF Core Code First context. Inherits Identity to share one SQLite database.</summary>
public sealed class MediaLibDbContext : IdentityDbContext<Utilisateur>
{
    public MediaLibDbContext(DbContextOptions<MediaLibDbContext> options) : base(options)
    {
    }

    public DbSet<Auteur> Auteurs => Set<Auteur>();

    public DbSet<Categorie> Categories => Set<Categorie>();

    public DbSet<Livre> Livres => Set<Livre>();

    public DbSet<Emprunt> Emprunts => Set<Emprunt>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Auteur>(entity =>
        {
            entity.ToTable("Auteurs");
            entity.Property(e => e.Nom).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Prenom).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Biographie).HasMaxLength(2000).IsRequired();
        });

        builder.Entity<Categorie>(entity =>
        {
            entity.ToTable("Categories");
            entity.Property(e => e.Libelle).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Libelle).IsUnique();
        });

        builder.Entity<Livre>(entity =>
        {
            entity.ToTable("Livres", table =>
            {
                table.HasCheckConstraint("CK_Livres_Exemplaires", "NombreExemplaires >= 0");
                table.HasCheckConstraint("CK_Livres_Disponibles", "NombreDisponibles >= 0");
                table.HasCheckConstraint("CK_Livres_Stock", "NombreDisponibles <= NombreExemplaires");
                table.HasCheckConstraint("CK_Livres_Annee", "AnneePublication > 0 AND AnneePublication <= 2100");
            });
            entity.Property(e => e.Titre).HasMaxLength(250).IsRequired();
            entity.Property(e => e.ISBN).HasMaxLength(17).IsRequired();
            entity.Property(e => e.Resume).HasMaxLength(3000).IsRequired();
            entity.Property(e => e.CouvertureUrl).HasMaxLength(500);
            entity.HasIndex(e => e.ISBN).IsUnique();
            entity.HasIndex(e => e.Titre);
            entity.HasIndex(e => e.AuteurId);
            entity.HasIndex(e => e.CategorieId);
            entity.HasIndex(e => e.DateAjout);

            entity.HasOne(e => e.Auteur)
                .WithMany(a => a.Livres)
                .HasForeignKey(e => e.AuteurId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Categorie)
                .WithMany(c => c.Livres)
                .HasForeignKey(e => e.CategorieId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Emprunt>(entity =>
        {
            entity.ToTable("Emprunts");
            entity.HasIndex(e => e.UtilisateurId);
            entity.HasIndex(e => e.LivreId);
            entity.HasIndex(e => e.DateRetourPrevue);
            entity.HasIndex(e => new { e.UtilisateurId, e.DateRetourEffective });

            entity.HasOne(e => e.Livre)
                .WithMany(l => l.Emprunts)
                .HasForeignKey(e => e.LivreId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Utilisateur)
                .WithMany(u => u.Emprunts)
                .HasForeignKey(e => e.UtilisateurId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.UtilisateurId);
            entity.HasOne(e => e.Utilisateur)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UtilisateurId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Utilisateur>(entity =>
        {
            entity.Property(e => e.Nom).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Prenom).HasMaxLength(120).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}
