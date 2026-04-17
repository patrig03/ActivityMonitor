using Database.Configuration;
using Database.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Database;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ActivityMonitorDbContext>
{
    public ActivityMonitorDbContext CreateDbContext(string[] args)
    {
        var options = ActivityMonitorDatabaseOptions.LoadFromEnvironment();
        
        var optionsBuilder = new DbContextOptionsBuilder<ActivityMonitorDbContext>();
        optionsBuilder.UseMySql(
            $"Server={options.Host};Port={options.Port};Database={options.Database};User={options.Username};Password={options.Password};SslMode={options.SslMode};",
            ServerVersion.AutoDetect($"Server={options.Host};Port={options.Port};Database={options.Database};User={options.Username};Password={options.Password};")
        );

        return new ActivityMonitorDbContext(optionsBuilder.Options);
    }
}