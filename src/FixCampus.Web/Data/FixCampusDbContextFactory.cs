using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FixCampus.Web.Data;

public class FixCampusDbContextFactory : IDesignTimeDbContextFactory<FixCampusDbContext>
{
    public FixCampusDbContext CreateDbContext(string[] args)
    {
        // dotnet ef may be invoked from the repository root or project directory.
        var current = Directory.GetCurrentDirectory();
        var project = File.Exists(Path.Combine(current, "appsettings.json"))
            ? current : Path.Combine(current, "src", "FixCampus.Web");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(project)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<FixCampusDbContext>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connection = configuration.GetConnectionString("Supabase");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException(
                "ConnectionStrings:Supabase eksik. User Secrets veya ConnectionStrings__Supabase ortam değişkenini ayarlayın.");
        return new FixCampusDbContext(
            new DbContextOptionsBuilder<FixCampusDbContext>().UseNpgsql(connection).Options);
    }
}
