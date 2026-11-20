using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MediaLib.Api.Data;

/// <summary>
/// Used only by `dotnet ef` design-time tooling so migrations can be generated
/// without starting the web host.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MediaLibDbContext>
{
    public MediaLibDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MediaLibDbContext>()
            .UseSqlite("Data Source=atlas-medialib.db")
            .Options;

        return new MediaLibDbContext(options);
    }
}
