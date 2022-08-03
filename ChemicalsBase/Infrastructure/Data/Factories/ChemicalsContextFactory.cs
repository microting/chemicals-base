namespace ChemicalsBase.Infrastructure.Data.Factories;

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
public class ChemicalsContextFactory : IDesignTimeDbContextFactory<ChemicalsDbContext>
{
    public ChemicalsDbContext CreateDbContext(string?[] args)
    {
        var defaultCs = "Server = localhost; port = 3306; Database = chemical-base; user = root; password = secretpassword;Convert Zero Datetime = true;";
        var optionsBuilder = new DbContextOptionsBuilder<ChemicalsDbContext>();

        optionsBuilder.UseMySql((args.Any() ? args[0] : defaultCs)!, new MariaDbServerVersion(
            ServerVersion.AutoDetect(args.Any() ? args[0] : defaultCs)), mySqlOptionsAction: builder =>
        {
            builder.EnableRetryOnFailure();
        });
        //optionsBuilder.UseLazyLoadingProxies(true);

        return new ChemicalsDbContext(optionsBuilder.Options);
        // dotnet ef migrations add InitialCreate --project ChemicalsBase --startup-project DBMigrator
    }
}